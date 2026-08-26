using System.Security.Claims;
using Core.Application.Responses;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Symplify.BackOffice.Application.Common.Authorization;
using Symplify.BackOffice.Application.Common.Text;
using Symplify.BackOffice.Application.Features.Users.Dtos;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Application.Services.UserAdministration;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Identity;
using Symplify.BackOffice.Domain.Organization;
using Symplify.BackOffice.Domain.Lookups;
using Symplify.BackOffice.Domain.Reference;
using Symplify.BackOffice.Infrastructure.Identity.Constants;

namespace Symplify.BackOffice.Infrastructure.Identity;

public sealed class BackOfficeUserAdministrationService : IUserAdministrationService
{
    private const string PermissionClaimType = "Permission";
    private const int MaxPasswordResetAttemptsPerWindow = 4;
    private static readonly TimeSpan PasswordResetWindow = TimeSpan.FromHours(1);

    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly IPasswordGenerator _passwordGenerator;
    private readonly IMemoryCache _memoryCache;
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly ICongressRepository _congressRepository;
    private readonly IStateRepository _stateRepository;

    public BackOfficeUserAdministrationService(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        IPasswordGenerator passwordGenerator,
        IMemoryCache memoryCache,
        IOrganizationUserRepository organizationUserRepository,
        ICongressRepository congressRepository,
        IStateRepository stateRepository)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _passwordGenerator = passwordGenerator;
        _memoryCache = memoryCache;
        _organizationUserRepository = organizationUserRepository;
        _congressRepository = congressRepository;
        _stateRepository = stateRepository;
    }

    public async Task<GetListResponse<UserListItemDto>> GetListAsync(
        int page,
        int pageSize,
        string? searchText,
        bool? isBlacklisted,
        Guid? organizationId,
        bool? emailConfirmed,
        Guid? countryId,
        Guid? stateId,
        Guid? congressId,
        string? roleName,
        string? accountStatus,
        string? culture,
        string? sortColumn = null,
        string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        page = page < 0 ? 0 : page;
        pageSize = pageSize <= 0 ? 25 : Math.Min(pageSize, 200);

        Guid? normalizedOrganizationId = NormalizeOptionalGuid(organizationId);
        Guid? normalizedCountryId = NormalizeOptionalGuid(countryId);
        Guid? normalizedStateId = NormalizeOptionalGuid(stateId);
        Guid? normalizedCongressId = NormalizeOptionalGuid(congressId);
        string? normalizedRoleName = NormalizeOptional(roleName);
        string? normalizedAccountStatus = NormalizeOptional(accountStatus)?.ToLowerInvariant();

        IQueryable<AppUser> query = _userManager.Users
            .AsNoTracking()
            .Where(user => user.DeletedDate == null)
            .Include(user => user.Title)
                .ThenInclude(title => title!.Translations)
                    .ThenInclude(translation => translation.Language)
            .Include(user => user.Country)
                .ThenInclude(country => country!.Translations)
                    .ThenInclude(translation => translation.Language)
            .Include(user => user.State)
                .ThenInclude(state => state!.Translations)
                    .ThenInclude(translation => translation.Language)
            .Include(user => user.State)
                .ThenInclude(state => state!.Country)
                    .ThenInclude(country => country.Translations)
                        .ThenInclude(translation => translation.Language)
            .AsSplitQuery();

        if (normalizedOrganizationId.HasValue)
        {
            List<Guid> organizationUserIds = await _organizationUserRepository.Query()
                .AsNoTracking()
                .Where(access =>
                    access.DeletedDate == null &&
                    access.OrganizationId == normalizedOrganizationId.Value)
                .Select(access => access.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (organizationUserIds.Count == 0)
                return EmptyUserListResponse(page, pageSize);

            query = query.Where(user => organizationUserIds.Contains(user.Id));
        }

        if (normalizedCongressId.HasValue)
        {
            List<Guid> defaultCongressUserIds = await _organizationUserRepository.Query()
                .AsNoTracking()
                .Where(access =>
                    access.DeletedDate == null &&
                    access.DefaultCongressId == normalizedCongressId.Value)
                .Select(access => access.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = query.Where(user =>
                defaultCongressUserIds.Contains(user.Id) ||
                user.Submissions.Any(submission =>
                    submission.DeletedDate == null &&
                    submission.CongressId == normalizedCongressId.Value));
        }

        if (emailConfirmed.HasValue)
            query = query.Where(user => user.EmailConfirmed == emailConfirmed.Value);

        if (normalizedCountryId.HasValue)
        {
            query = query.Where(user =>
                (user.StateId.HasValue && user.State != null && user.State.CountryId == normalizedCountryId.Value) ||
                (!user.StateId.HasValue && user.CountryId == normalizedCountryId.Value));
        }

        if (normalizedStateId.HasValue)
            query = query.Where(user => user.StateId == normalizedStateId.Value);

        DateTimeOffset utcNow = DateTimeOffset.UtcNow;

        switch (normalizedAccountStatus)
        {
            case "active":
                query = query.Where(user =>
                    !user.IsBlacklisted &&
                    (!user.LockoutEnabled || !user.LockoutEnd.HasValue || user.LockoutEnd <= utcNow));
                break;

            case "locked":
                query = query.Where(user =>
                    !user.IsBlacklisted &&
                    user.LockoutEnabled &&
                    user.LockoutEnd.HasValue &&
                    user.LockoutEnd > utcNow);
                break;

            case "blacklisted":
                query = query.Where(user => user.IsBlacklisted);
                break;

            default:
                if (isBlacklisted.HasValue)
                    query = query.Where(user => user.IsBlacklisted == isBlacklisted.Value);
                break;
        }

        if (!string.IsNullOrWhiteSpace(normalizedRoleName))
        {
            IList<AppUser> usersInRole = await _userManager.GetUsersInRoleAsync(normalizedRoleName);
            List<Guid> roleUserIds = usersInRole
                .Where(user => user.DeletedDate == null)
                .Select(user => user.Id)
                .Distinct()
                .ToList();

            if (roleUserIds.Count == 0)
                return EmptyUserListResponse(page, pageSize);

            query = query.Where(user => roleUserIds.Contains(user.Id));
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            string normalizedSearch = searchText.Trim().ToLowerInvariant();

            List<Guid> organizationSearchUserIds = await _organizationUserRepository.Query()
                .AsNoTracking()
                .Where(access =>
                    access.DeletedDate == null &&
                    (
                        access.Organization.Name.ToLower().Contains(normalizedSearch) ||
                        (access.Organization.ShortName != null && access.Organization.ShortName.ToLower().Contains(normalizedSearch)) ||
                        (access.Organization.Code != null && access.Organization.Code.ToLower().Contains(normalizedSearch)) ||
                        (access.DefaultCongress != null &&
                         (
                             access.DefaultCongress.Name.ToLower().Contains(normalizedSearch) ||
                             access.DefaultCongress.Code.ToLower().Contains(normalizedSearch) ||
                             access.DefaultCongress.Translations.Any(translation =>
                                 translation.DeletedDate == null &&
                                 translation.Title.ToLower().Contains(normalizedSearch))
                         ))
                    ))
                .Select(access => access.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = query.Where(user =>
                (user.Email != null && user.Email.ToLower().Contains(normalizedSearch)) ||
                (user.PhoneNumber != null && user.PhoneNumber.ToLower().Contains(normalizedSearch)) ||
                user.Name.ToLower().Contains(normalizedSearch) ||
                user.Surname.ToLower().Contains(normalizedSearch) ||
                (user.Institution != null && user.Institution.ToLower().Contains(normalizedSearch)) ||
                (user.Orcid != null && user.Orcid.ToLower().Contains(normalizedSearch)) ||
                (user.Country != null && user.Country.Translations.Any(translation =>
                    translation.DeletedDate == null &&
                    translation.Name.ToLower().Contains(normalizedSearch))) ||
                (user.State != null && user.State.Country.Translations.Any(translation =>
                    translation.DeletedDate == null &&
                    translation.Name.ToLower().Contains(normalizedSearch))) ||
                (user.State != null && user.State.Translations.Any(translation =>
                    translation.DeletedDate == null &&
                    translation.Name.ToLower().Contains(normalizedSearch))) ||
                user.Submissions.Any(submission =>
                    submission.DeletedDate == null &&
                    (
                        submission.Congress.Name.ToLower().Contains(normalizedSearch) ||
                        submission.Congress.Code.ToLower().Contains(normalizedSearch) ||
                        submission.Congress.Translations.Any(translation =>
                            translation.DeletedDate == null &&
                            translation.Title.ToLower().Contains(normalizedSearch))
                    )) ||
                organizationSearchUserIds.Contains(user.Id));
        }

        int count = await query.CountAsync(cancellationToken);

        List<AppUser> users = await ApplyUserListOrdering(query, sortColumn, sortDirection)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        List<Guid> pageUserIds = users.Select(user => user.Id).ToList();
        Dictionary<Guid, OrganizationUser> organizationAccessByUserId = await LoadOrganizationAccessByUserIdAsync(
            pageUserIds,
            normalizedOrganizationId,
            normalizedCongressId,
            cancellationToken);

        List<UserListItemDto> items = new(users.Count);

        foreach (AppUser user in users)
        {
            IList<string> roles = await _userManager.GetRolesAsync(user);
            bool isLockedOut = user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd > utcNow;
            organizationAccessByUserId.TryGetValue(user.Id, out OrganizationUser? organizationAccess);

            items.Add(new UserListItemDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                FullName = ResolveFullName(user),
                TitleShortName = ResolveTitleShortName(user.Title, culture),
                Institution = user.Institution,
                Orcid = user.Orcid,
                CountryName = ResolveCountryName(user.State?.Country ?? user.Country, culture),
                StateName = ResolveStateName(user.State, culture),
                OrganizationName = ResolveOrganizationName(organizationAccess),
                OrganizationShortName = ResolveOrganizationShortName(organizationAccess),
                DefaultCongressName = ResolveCongressName(organizationAccess?.DefaultCongress, culture),
                RolesText = roles.Count == 0 ? "-" : string.Join(", ", roles.OrderBy(role => role)),
                EmailConfirmed = user.EmailConfirmed,
                IsBlacklisted = user.IsBlacklisted,
                IsLockedOut = isLockedOut,
                OrganizationAccessIsActive = organizationAccess?.IsActive ?? true,
                CreatedDate = user.CreatedDate
            });
        }

        int pages = count == 0 ? 0 : (int)Math.Ceiling(count / (double)pageSize);

        return new GetListResponse<UserListItemDto>
        {
            Index = page,
            Size = pageSize,
            Count = count,
            Pages = pages,
            HasPrevious = page > 0,
            HasNext = page + 1 < pages,
            Items = items
        };
    }

    public async Task<UserDetailDto> GetByIdAsync(
        Guid id,
        string? culture = null,
        CancellationToken cancellationToken = default)
    {
        AppUser? user = await _userManager.Users
            .AsNoTracking()
            .Where(item => item.Id == id && item.DeletedDate == null)
            .Include(item => item.Title)
                .ThenInclude(title => title!.Translations)
                    .ThenInclude(translation => translation.Language)
            .Include(item => item.Country)
                .ThenInclude(country => country!.Translations)
                    .ThenInclude(translation => translation.Language)
            .Include(item => item.State)
                .ThenInclude(state => state!.Translations)
                    .ThenInclude(translation => translation.Language)
            .Include(item => item.State)
                .ThenInclude(state => state!.Country)
                    .ThenInclude(country => country.Translations)
                        .ThenInclude(translation => translation.Language)
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            throw new BusinessException(UserMessages.UserNotFound);

        return await MapDetailAsync(user, culture, cancellationToken);
    }

    public async Task<CreatedUserDto> CreateAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default)
    {
        string email = NormalizeEmail(request.Email);

        AppUser? existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
            throw new BusinessException(UserMessages.EmailAlreadyRegistered);

        string password = request.GeneratePassword || string.IsNullOrWhiteSpace(request.Password)
            ? _passwordGenerator.Generate()
            : request.Password!;

        (Guid? countryId, Guid? stateId) = await NormalizeLocationAsync(
            request.CountryId,
            request.StateId,
            cancellationToken);

        AppUser user = new()
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            Name = BackOfficeTextNormalizer.NormalizeRequiredPersonFirstName(request.Name),
            Surname = BackOfficeTextNormalizer.NormalizeRequiredPersonSurname(request.Surname),
            Institution = BackOfficeTextNormalizer.NormalizeInstitution(request.Institution),
            TitleId = NormalizeOptionalGuid(request.TitleId),
            CountryId = countryId,
            StateId = stateId,
            Orcid = NormalizeOptional(request.Orcid),
            PhoneNumber = NormalizeOptional(request.PhoneNumber),
            EmailConfirmed = request.EmailConfirmed,
            LockoutEnabled = true,
            IsBlacklisted = false,
            CreatedDate = DateTime.UtcNow
        };

        IdentityResult createResult = await _userManager.CreateAsync(user, password);
        ThrowIfFailed(createResult, UserMessages.UserCreateFailed);

        await UpdateRolesAsync(user.Id, request.RoleNames, cancellationToken);

        return new CreatedUserDto
        {
            Id = user.Id,
            Email = email,
            GeneratedPassword = password
        };
    }

    public async Task<UserDetailDto> UpdateAsync(UpdateUserRequestDto request, CancellationToken cancellationToken = default)
    {
        AppUser user = await GetUserOrThrowAsync(request.Id);
        string email = NormalizeEmail(request.Email);

        AppUser? existingWithEmail = await _userManager.FindByEmailAsync(email);
        if (existingWithEmail is not null && existingWithEmail.Id != user.Id)
            throw new BusinessException(UserMessages.EmailTakenByAnotherUser);

        (Guid? countryId, Guid? stateId) = await NormalizeLocationAsync(
            request.CountryId,
            request.StateId,
            cancellationToken);

        user.Email = email;
        user.UserName = email;
        user.Name = BackOfficeTextNormalizer.NormalizeRequiredPersonFirstName(request.Name);
        user.Surname = BackOfficeTextNormalizer.NormalizeRequiredPersonSurname(request.Surname);
        user.Institution = BackOfficeTextNormalizer.NormalizeInstitution(request.Institution);
        user.TitleId = NormalizeOptionalGuid(request.TitleId);
        user.CountryId = countryId;
        user.StateId = stateId;
        user.Orcid = NormalizeOptional(request.Orcid);
        user.PhoneNumber = NormalizeOptional(request.PhoneNumber);
        user.EmailConfirmed = request.EmailConfirmed;
        user.LockoutEnabled = request.LockoutEnabled;
        user.UpdatedDate = DateTime.UtcNow;

        await UpsertOrganizationAccessAsync(user.Id, request, cancellationToken);

        IdentityResult result = await _userManager.UpdateAsync(user);
        ThrowIfFailed(result, UserMessages.UserUpdateFailed);

        return await GetByIdAsync(user.Id, null, cancellationToken);
    }

    public async Task<ResetUserPasswordDto> ResetPasswordAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        AppUser user = await GetUserOrThrowAsync(userId);
        int remaining = RegisterPasswordResetAttempt(user.Id);

        string password = _passwordGenerator.Generate();
        string token = await _userManager.GeneratePasswordResetTokenAsync(user);
        IdentityResult result = await _userManager.ResetPasswordAsync(user, token, password);
        ThrowIfFailed(result, UserMessages.PasswordResetFailed);

        await _userManager.UpdateSecurityStampAsync(user);
        RemoveClaimsCache(user.Id);

        return new ResetUserPasswordDto
        {
            UserId = user.Id,
            GeneratedPassword = password,
            RemainingAttemptsInWindow = remaining
        };
    }

    public async Task UpdateRolesAsync(Guid userId, IReadOnlyCollection<string> roleNames, CancellationToken cancellationToken = default)
    {
        AppUser user = await GetUserOrThrowAsync(userId);
        HashSet<string> requestedRoles = roleNames
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (string roleName in requestedRoles)
        {
            bool exists = await _roleManager.RoleExistsAsync(roleName);
            if (!exists)
                throw new BusinessException(UserMessages.RoleNotFound);
        }

        IList<string> currentRoles = await _userManager.GetRolesAsync(user);
        IEnumerable<string> rolesToRemove = currentRoles.Where(role => !requestedRoles.Contains(role)).ToList();
        IEnumerable<string> rolesToAdd = requestedRoles.Where(role => !currentRoles.Contains(role, StringComparer.OrdinalIgnoreCase)).ToList();

        if (rolesToRemove.Any())
            ThrowIfFailed(await _userManager.RemoveFromRolesAsync(user, rolesToRemove), UserMessages.UserRolesUpdateFailed);

        if (rolesToAdd.Any())
            ThrowIfFailed(await _userManager.AddToRolesAsync(user, rolesToAdd), UserMessages.UserRolesUpdateFailed);

        RemoveClaimsCache(user.Id);
    }

    public async Task UpdateClaimsAsync(Guid userId, IReadOnlyCollection<string> claimNames, CancellationToken cancellationToken = default)
    {
        AppUser user = await GetUserOrThrowAsync(userId);
        HashSet<string> catalog = OperationClaimCatalog.GetAll().ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> requestedClaims = claimNames
            .Where(claim => !string.IsNullOrWhiteSpace(claim))
            .Select(claim => claim.Trim())
            .Where(claim => catalog.Contains(claim))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        IList<Claim> currentClaims = await _userManager.GetClaimsAsync(user);
        IEnumerable<Claim> currentPermissionClaims = currentClaims
            .Where(claim => claim.Type == PermissionClaimType)
            .ToList();

        if (currentPermissionClaims.Any())
            ThrowIfFailed(await _userManager.RemoveClaimsAsync(user, currentPermissionClaims), UserMessages.UserPermissionsUpdateFailed);

        IEnumerable<Claim> newClaims = requestedClaims
            .OrderBy(claim => claim)
            .Select(claim => new Claim(PermissionClaimType, claim));

        if (newClaims.Any())
            ThrowIfFailed(await _userManager.AddClaimsAsync(user, newClaims), UserMessages.UserPermissionsUpdateFailed);

        RemoveClaimsCache(user.Id);
    }

    public async Task SetBlacklistAsync(Guid userId, bool isBlacklisted, CancellationToken cancellationToken = default)
    {
        AppUser user = await GetUserOrThrowAsync(userId);
        user.IsBlacklisted = isBlacklisted;
        user.UpdatedDate = DateTime.UtcNow;

        if (isBlacklisted)
            user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
        else
            user.LockoutEnd = null;

        IdentityResult result = await _userManager.UpdateAsync(user);
        ThrowIfFailed(result, UserMessages.BlacklistStatusUpdateFailed);

        RemoveClaimsCache(user.Id);
    }


    public async Task SoftDeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        AppUser user = await GetUserOrThrowAsync(userId);
        user.DeletedDate = DateTime.UtcNow;
        user.IsBlacklisted = true;
        user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
        user.UpdatedDate = DateTime.UtcNow;

        IdentityResult result = await _userManager.UpdateAsync(user);
        ThrowIfFailed(result, UserMessages.UserDeleteFailed);

        RemoveClaimsCache(user.Id);
    }

    private async Task<UserDetailDto> MapDetailAsync(
        AppUser user,
        string? culture,
        CancellationToken cancellationToken)
    {
        IList<string> roles = await _userManager.GetRolesAsync(user);
        IList<Claim> userClaims = await _userManager.GetClaimsAsync(user);
        bool isLockedOut = user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;

        IReadOnlyList<string> assignedClaims = userClaims
            .Where(claim => claim.Type == PermissionClaimType)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(claim => claim)
            .ToList();

        List<RoleOptionDto> availableRoles = _roleManager.Roles
            .AsEnumerable()
            .Where(role => role.DeletedDate == null && !string.IsNullOrWhiteSpace(role.Name))
            .OrderBy(role => role.Name)
            .Select(role => new RoleOptionDto
            {
                Name = role.Name!,
                IsAssigned = roles.Contains(role.Name!, StringComparer.OrdinalIgnoreCase)
            })
            .ToList();

        HashSet<string> assignedClaimSet = assignedClaims.ToHashSet(StringComparer.OrdinalIgnoreCase);
        List<ClaimOptionDto> availableClaims = OperationClaimCatalog.GetAll()
            .Select(claim => new ClaimOptionDto
            {
                Name = claim,
                IsAssigned = assignedClaimSet.Contains(claim)
            })
            .ToList();

        OrganizationUser? organizationAccess = await GetPrimaryOrganizationAccessAsync(user.Id, cancellationToken);

        return new UserDetailDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
            Name = user.Name,
            Surname = user.Surname,
            FullName = ResolveFullName(user),
            Institution = user.Institution,
            TitleId = user.TitleId,
            TitleName = ResolveTitleName(user.Title, culture),
            TitleShortName = ResolveTitleShortName(user.Title, culture),
            CountryId = user.State?.CountryId ?? user.CountryId,
            CountryName = ResolveCountryName(user.State?.Country ?? user.Country, culture),
            StateId = user.StateId,
            StateName = ResolveStateName(user.State, culture),
            Orcid = user.Orcid,
            PhoneNumber = user.PhoneNumber,
            EmailConfirmed = user.EmailConfirmed,
            IsBlacklisted = user.IsBlacklisted,
            LockoutEnabled = user.LockoutEnabled,
            IsLockedOut = isLockedOut,
            LockoutEnd = user.LockoutEnd,
            AccessFailedCount = user.AccessFailedCount,
            OrganizationAccessId = organizationAccess?.Id,
            OrganizationId = organizationAccess?.OrganizationId,
            OrganizationName = ResolveOrganizationName(organizationAccess),
            OrganizationShortName = ResolveOrganizationShortName(organizationAccess),
            DefaultCongressId = organizationAccess?.DefaultCongressId,
            DefaultCongressName = ResolveCongressName(organizationAccess?.DefaultCongress, culture),
            OrganizationAccessIsActive = organizationAccess?.IsActive ?? true,
            CreatedDate = user.CreatedDate,
            UpdatedDate = user.UpdatedDate,
            AssignedRoles = roles.OrderBy(role => role).ToList(),
            AssignedClaims = assignedClaims,
            AvailableRoles = availableRoles,
            AvailableClaims = availableClaims
        };
    }

    private async Task<(Guid? CountryId, Guid? StateId)> NormalizeLocationAsync(
        Guid? requestedCountryId,
        Guid? requestedStateId,
        CancellationToken cancellationToken)
    {
        Guid? countryId = NormalizeOptionalGuid(requestedCountryId);
        Guid? stateId = NormalizeOptionalGuid(requestedStateId);

        if (!stateId.HasValue)
            return (countryId, null);

        State? state = await _stateRepository.GetAsync(
            predicate: item =>
                item.Id == stateId.Value &&
                item.DeletedDate == null &&
                item.IsActive,
            cancellationToken: cancellationToken);

        if (state is null)
            throw new BusinessException("Seçilen şehir / il kaydı bulunamadı veya aktif değil.");

        // Şehir/il daha spesifik kaynaktır. CountryId ile StateId uyuşmuyorsa
        // ülkeyi State.CountryId üzerinden düzeltiriz.
        return (state.CountryId, state.Id);
    }

    private async Task UpsertOrganizationAccessAsync(
        Guid userId,
        UpdateUserRequestDto request,
        CancellationToken cancellationToken)
    {
        Guid? organizationId = NormalizeOptionalGuid(request.OrganizationId);
        Guid? defaultCongressId = NormalizeOptionalGuid(request.DefaultCongressId);

        if (!organizationId.HasValue)
            return;

        if (defaultCongressId.HasValue)
        {
            Congress? congress = await _congressRepository.GetAsync(
                predicate: item => item.Id == defaultCongressId.Value,
                cancellationToken: cancellationToken);

            if (congress is null || congress.DeletedDate is not null)
                throw new BusinessException(UserMessages.DefaultCongressNotFound);

            if (congress.OrganizationId != organizationId.Value)
                throw new BusinessException(UserMessages.DefaultCongressMustBelongToOrganization);
        }

        OrganizationUser? organizationUser = await _organizationUserRepository.GetAsync(
            predicate: item => item.UserId == userId && item.DeletedDate == null,
            cancellationToken: cancellationToken);

        if (organizationUser is null)
        {
            organizationUser = new OrganizationUser
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId.Value,
                UserId = userId,
                DefaultCongressId = defaultCongressId,
                IsActive = request.OrganizationAccessIsActive,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "UserAdministration"
            };

            await _organizationUserRepository.AddAsync(organizationUser);
            return;
        }

        organizationUser.OrganizationId = organizationId.Value;
        organizationUser.DefaultCongressId = defaultCongressId;
        organizationUser.IsActive = request.OrganizationAccessIsActive;
        organizationUser.UpdatedDate = DateTime.UtcNow;
        organizationUser.UpdatedBy = "UserAdministration";

        await _organizationUserRepository.UpdateAsync(organizationUser);
    }

    private async Task<Dictionary<Guid, OrganizationUser>> LoadOrganizationAccessByUserIdAsync(
        IReadOnlyCollection<Guid> userIds,
        Guid? organizationId,
        Guid? congressId,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
            return new Dictionary<Guid, OrganizationUser>();

        IQueryable<OrganizationUser> query = _organizationUserRepository.Query()
            .AsNoTracking()
            .Where(item => item.DeletedDate == null && userIds.Contains(item.UserId))
            .Include(item => item.Organization)
            .Include(item => item.DefaultCongress)
                .ThenInclude(congress => congress!.Translations)
                    .ThenInclude(translation => translation.Language);

        if (organizationId.HasValue)
            query = query.Where(item => item.OrganizationId == organizationId.Value);

        if (congressId.HasValue)
            query = query.Where(item => item.DefaultCongressId == congressId.Value);

        List<OrganizationUser> accesses = await query.ToListAsync(cancellationToken);

        return accesses
            .GroupBy(item => item.UserId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(item => item.IsActive)
                    .ThenByDescending(item => item.DefaultCongressId.HasValue)
                    .ThenBy(item => item.CreatedDate)
                    .First());
    }

    private static string ResolveOrganizationShortName(OrganizationUser? organizationAccess)
    {
        Organization? organization = organizationAccess?.Organization;

        if (organization is null)
            return "-";

        if (!string.IsNullOrWhiteSpace(organization.ShortName))
            return organization.ShortName;

        if (!string.IsNullOrWhiteSpace(organization.Code))
            return organization.Code;

        return organization.Name;
    }

    private static string ResolveOrganizationName(OrganizationUser? organizationAccess)
    {
        Organization? organization = organizationAccess?.Organization;
        return organization is null || string.IsNullOrWhiteSpace(organization.Name)
            ? "-"
            : organization.Name;
    }

    private static IOrderedQueryable<AppUser> ApplyUserListOrdering(
        IQueryable<AppUser> query,
        string? sortColumn,
        string? sortDirection)
    {
        bool descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        string column = string.IsNullOrWhiteSpace(sortColumn) ? "createdDate" : sortColumn.Trim();

        return column.ToLowerInvariant() switch
        {
            "fullname" => descending
                ? query.OrderByDescending(user => user.Name).ThenByDescending(user => user.Surname)
                : query.OrderBy(user => user.Name).ThenBy(user => user.Surname),

            "email" => descending
                ? query.OrderByDescending(user => user.Email).ThenByDescending(user => user.CreatedDate)
                : query.OrderBy(user => user.Email).ThenBy(user => user.CreatedDate),

            "phonenumber" => descending
                ? query.OrderByDescending(user => user.PhoneNumber).ThenByDescending(user => user.CreatedDate)
                : query.OrderBy(user => user.PhoneNumber).ThenBy(user => user.CreatedDate),

            "institution" => descending
                ? query.OrderByDescending(user => user.Institution).ThenByDescending(user => user.CreatedDate)
                : query.OrderBy(user => user.Institution).ThenBy(user => user.CreatedDate),

            "createddate" => descending
                ? query.OrderByDescending(user => user.CreatedDate).ThenByDescending(user => user.Email)
                : query.OrderBy(user => user.CreatedDate).ThenBy(user => user.Email),

            _ => query.OrderByDescending(user => user.CreatedDate).ThenByDescending(user => user.Email)
        };
    }

    private async Task<OrganizationUser?> GetPrimaryOrganizationAccessAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _organizationUserRepository.Query()
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.DeletedDate == null)
            .Include(item => item.Organization)
            .Include(item => item.DefaultCongress)
                .ThenInclude(congress => congress!.Translations)
                    .ThenInclude(translation => translation.Language)
            .OrderByDescending(item => item.IsActive)
            .ThenByDescending(item => item.DefaultCongressId.HasValue)
            .ThenBy(item => item.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<AppUser> GetUserOrThrowAsync(Guid userId)
    {
        AppUser? user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.DeletedDate is not null)
            throw new BusinessException(UserMessages.UserNotFound);

        return user;
    }

    private int RegisterPasswordResetAttempt(Guid userId)
    {
        string cacheKey = $"BackOffice:PasswordResetAttempts:{userId}";
        DateTime utcNow = DateTime.UtcNow;

        List<DateTime> attempts = _memoryCache.TryGetValue(cacheKey, out List<DateTime>? cachedAttempts) && cachedAttempts is not null
            ? cachedAttempts.Where(date => utcNow - date < PasswordResetWindow).ToList()
            : new List<DateTime>();

        if (attempts.Count >= MaxPasswordResetAttemptsPerWindow)
            throw new BusinessException(UserMessages.PasswordResetRateLimitExceeded);

        attempts.Add(utcNow);

        _memoryCache.Set(
            cacheKey,
            attempts,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = PasswordResetWindow
            });

        return MaxPasswordResetAttemptsPerWindow - attempts.Count;
    }

    private void RemoveClaimsCache(Guid userId)
    {
        _memoryCache.Remove($"BackOffice:UserOperationClaims:{userId}");
    }

    private static GetListResponse<UserListItemDto> EmptyUserListResponse(int page, int pageSize)
    {
        return new GetListResponse<UserListItemDto>
        {
            Index = page,
            Size = pageSize,
            Count = 0,
            Pages = 0,
            HasPrevious = false,
            HasNext = false,
            Items = new List<UserListItemDto>()
        };
    }

    private static string ResolveTitleName(Title? title, string? culture)
    {
        var translation = title?.Translations
            .Where(item => item.DeletedDate == null)
            .OrderByDescending(item => CultureMatches(item.Language?.Culture, culture))
            .ThenByDescending(item => item.Language?.IsDefault == true)
            .ThenBy(item => item.CreatedDate)
            .FirstOrDefault();

        return translation?.Name?.Trim() ?? string.Empty;
    }

    private static string ResolveTitleShortName(Title? title, string? culture)
    {
        var translation = title?.Translations
            .Where(item => item.DeletedDate == null)
            .OrderByDescending(item => CultureMatches(item.Language?.Culture, culture))
            .ThenByDescending(item => item.Language?.IsDefault == true)
            .ThenBy(item => item.CreatedDate)
            .FirstOrDefault();

        return FirstNonEmpty(translation?.Description, translation?.Name);
    }

    private static string ResolveCountryName(Country? country, string? culture)
    {
        var translation = country?.Translations
            .Where(item => item.DeletedDate == null)
            .OrderByDescending(item => CultureMatches(item.Language?.Culture, culture))
            .ThenByDescending(item => item.Language?.IsDefault == true)
            .ThenBy(item => item.CreatedDate)
            .FirstOrDefault();

        return translation?.Name?.Trim() ?? string.Empty;
    }

    private static string ResolveStateName(State? state, string? culture)
    {
        var translation = state?.Translations
            .Where(item => item.DeletedDate == null)
            .OrderByDescending(item => CultureMatches(item.Language?.Culture, culture))
            .ThenByDescending(item => item.Language?.IsDefault == true)
            .ThenBy(item => item.CreatedDate)
            .FirstOrDefault();

        return translation?.Name?.Trim() ?? string.Empty;
    }

    private static string ResolveCongressName(Congress? congress, string? culture)
    {
        var translation = congress?.Translations
            .Where(item => item.DeletedDate == null)
            .OrderByDescending(item => CultureMatches(item.Language?.Culture, culture))
            .ThenByDescending(item => item.Language?.IsDefault == true)
            .ThenBy(item => item.CreatedDate)
            .FirstOrDefault();

        return FirstNonEmpty(translation?.Title, congress?.Name, congress?.Code);
    }

    private static bool CultureMatches(string? candidate, string? requested)
    {
        return !string.IsNullOrWhiteSpace(candidate) &&
               !string.IsNullOrWhiteSpace(requested) &&
               string.Equals(candidate, requested, StringComparison.OrdinalIgnoreCase);
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?.Trim() ?? string.Empty;
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new BusinessException("E-posta adresi zorunludur.");

        return email.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static Guid? NormalizeOptionalGuid(Guid? value)
    {
        return value.HasValue && value.Value != Guid.Empty ? value.Value : null;
    }

    private static bool Contains(string? value, string searchText)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveFullName(AppUser user)
    {
        string fullName = $"{user.Name} {user.Surname}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? user.Email ?? user.UserName ?? string.Empty : fullName;
    }

    private static void ThrowIfFailed(IdentityResult result, string message)
    {
        if (result.Succeeded)
            return;

        string details = string.Join(" ", result.Errors.Select(error => error.Description));
        throw new BusinessException(string.IsNullOrWhiteSpace(details) ? message : $"{message} {details}");
    }
}
