using Core.CrossCuttingConcerns.Exceptions.Types;
using Core.Security.Constants;
using Core.Security.Extensions;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Core.Application.Pipelines.Authorization;

public class AuthorizationBehavior<TRequest, TResponse>(IHttpContextAccessor httpContextAccessor)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ISecuredRequest
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        List<string>? userRoleClaims = httpContextAccessor.HttpContext?.User.ClaimRoles();

        if (userRoleClaims == null)
            throw new AuthorizationException("You are not authenticated.");

        bool isNotMatchedAUserRoleClaimWithRequestRoles = userRoleClaims
            .FirstOrDefault(
                userRoleClaim => userRoleClaim == GeneralOperationClaims.Admin || request.Roles.Any(role => role == userRoleClaim)
            ) is null;
        if (isNotMatchedAUserRoleClaimWithRequestRoles)
            throw new AuthorizationException("You are not authorized.");

        TResponse response = await next();
        return response;
    }
}
