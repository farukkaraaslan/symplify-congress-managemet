using Core.Persistence.Dynamic;
using Core.Persistence.Paging;
using Core.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace Core.Persistence.Repositories;

public class EfRepositoryBase<TEntity, TEntityId, TContext>
    : IAsyncRepository<TEntity, TEntityId>,
        IRepository<TEntity, TEntityId>
    where TEntity : Entity<TEntityId>, IEntityTimestamps
    where TContext : DbContext
{
    protected readonly TContext Context;

    public EfRepositoryBase(TContext context)
    {
        Context = context;
    }

    public IQueryable<TEntity> Query()
    {
        return Context.Set<TEntity>();
    }

    private static IQueryable<TEntity> ApplySoftDeleteScope(IQueryable<TEntity> queryable, bool withDeleted)
    {
        return withDeleted ? queryable : queryable.Where(x => x.DeletedDate == null);
    }

    protected virtual void EditEntityPropertiesToAdd(TEntity entity)
    {
        entity.CreatedDate = DateTime.UtcNow;
        var httpContextAccessor = ServiceTool.GetService<IHttpContextAccessor>();
        entity.CreatedBy = httpContextAccessor?.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
             ?? httpContextAccessor?.HttpContext?.User?.FindFirst("sub")?.Value
             ?? "System";
    }

    protected virtual void EditEntityPropertiesToUpdate(TEntity entity)
    {
        entity.UpdatedDate = DateTime.UtcNow;
        var httpContextAccessor = ServiceTool.GetService<IHttpContextAccessor>();
        entity.UpdatedBy = httpContextAccessor?.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
             ?? httpContextAccessor?.HttpContext?.User?.FindFirst("sub")?.Value
             ?? "System";
    }

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        EditEntityPropertiesToAdd(entity);
        await Context.AddAsync(entity, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<ICollection<TEntity>> AddRangeAsync(
        ICollection<TEntity> entities,
        CancellationToken cancellationToken = default
    )
    {
        foreach (TEntity entity in entities)
            EditEntityPropertiesToAdd(entity);
        await Context.AddRangeAsync(entities, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return entities;
    }

    public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        EditEntityPropertiesToUpdate(entity);
        if (Context.Entry(entity).State == EntityState.Detached)
            Context.Update(entity);
        await Context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<ICollection<TEntity>> UpdateRangeAsync(
        ICollection<TEntity> entities,
        CancellationToken cancellationToken = default
    )
    {
        foreach (TEntity entity in entities)
            EditEntityPropertiesToUpdate(entity);
        if (entities.Any(e => Context.Entry(e).State == EntityState.Detached))
            Context.UpdateRange(entities);
        await Context.SaveChangesAsync(cancellationToken);
        return entities;
    }

    public async Task<TEntity> DeleteAsync(TEntity entity, bool permanent = false, CancellationToken cancellationToken = default)
    {
        await SetEntityAsDeleted(entity, permanent, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<ICollection<TEntity>> DeleteRangeAsync(
        ICollection<TEntity> entities,
        bool permanent = false,
        CancellationToken cancellationToken = default
    )
    {
        await SetEntityAsDeleted(entities, permanent, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return entities;
    }

    public TEntity Delete(TEntity entity, bool permanent = false)
    {
        SetEntityAsDeletedSync(entity, permanent);
        Context.SaveChanges();
        return entity;
    }

    public ICollection<TEntity> DeleteRange(ICollection<TEntity> entities, bool permanent = false)
    {
        SetEntityAsDeletedRangeSync(entities, permanent);
        Context.SaveChanges();
        return entities;
    }

    protected async Task SetEntityAsDeleted(
        TEntity entity,
        bool permanent,
        CancellationToken cancellationToken = default
    )
    {
        if (!permanent)
        {
            CheckHasEntityHaveOneToOneRelation(entity);
            await setEntityAsSoftDeleted(entity, cancellationToken);
        }
        else
            Context.Remove(entity);
    }

    protected void SetEntityAsDeletedSync(
        TEntity entity,
        bool permanent
    )
    {
        if (!permanent)
        {
            CheckHasEntityHaveOneToOneRelation(entity);
            setEntityAsSoftDeletedSync(entity);
        }
        else
            Context.Remove(entity);
    }

    protected void SetEntityAsDeletedRangeSync(
        IEnumerable<TEntity> entities,
        bool permanent
    )
    {
        foreach (TEntity entity in entities)
            SetEntityAsDeletedSync(entity, permanent);
    }

    protected async Task SetEntityAsDeleted(
        IEnumerable<TEntity> entities,
        bool permanent,
        CancellationToken cancellationToken = default
    )
    {
        foreach (TEntity entity in entities)
            await SetEntityAsDeleted(entity, permanent, cancellationToken);
    }

    private async Task setEntityAsSoftDeleted(
        IEntityTimestamps entity,
        CancellationToken cancellationToken = default,
        bool isRoot = true
    )
    {
        if (IsSoftDeleted(entity))
            return;

        UpdateEntityProperties(entity, isRoot);

        await ProcessNavigationsAsync(entity, cancellationToken);

        Context.Update(entity);
    }

    private void setEntityAsSoftDeletedSync(
        IEntityTimestamps entity,
        bool isRoot = true
    )
    {
        if (IsSoftDeleted(entity))
            return;

        UpdateEntityProperties(entity, isRoot);

        Context.Update(entity);
    }

    private void UpdateEntityProperties(IEntityTimestamps entity, bool isRoot)
    {
        if (isRoot)
            EditEntityPropertiesToDelete((TEntity)entity);
        else
            EditRelationEntityPropertiesToCascadeSoftDelete(entity);
    }

    private async Task ProcessNavigationsAsync(
        IEntityTimestamps entity,
        CancellationToken cancellationToken
    )
    {
        var navigations = Context
            .Entry(entity)
            .Metadata.GetNavigations()
            .Where(x =>
                x is { IsOnDependent: false, ForeignKey.DeleteBehavior: DeleteBehavior.ClientCascade or DeleteBehavior.Cascade }
            )
            .ToList();

        foreach (INavigation? navigation in navigations)
        {
            if (navigation.TargetEntityType.IsOwned() || navigation.PropertyInfo == null)
                continue;

            if (navigation.IsCollection)
                await HandleCollectionNavigationAsync(entity, navigation, cancellationToken);
            else
                await HandleReferenceNavigationAsync(entity, navigation, cancellationToken);
        }
    }

    private async Task HandleCollectionNavigationAsync(
      IEntityTimestamps entity,
      INavigation navigation,
      CancellationToken cancellationToken
    )
    {
        object? navValue = navigation.PropertyInfo!.GetValue(entity);

        var collectionEntry = Context.Entry(entity).Collection(navigation.PropertyInfo!.Name);

        if (!collectionEntry.IsLoaded)
        {
            IQueryable query = collectionEntry.Query();
            var elementType = navigation.TargetEntityType.ClrType;
            navValue = await LoadRelationDataAsync(query, elementType, cancellationToken, isCollection: true);
        }

        if (navValue == null)
            return;

        foreach (object navValueItem in (IEnumerable)navValue)
            await setEntityAsSoftDeleted((IEntityTimestamps)navValueItem, cancellationToken, isRoot: false);
    }

    private async Task HandleReferenceNavigationAsync(
        IEntityTimestamps entity,
        INavigation navigation,
        CancellationToken cancellationToken
    )
    {
        object? navValue = navigation.PropertyInfo!.GetValue(entity);

        var referenceEntry = Context.Entry(entity).Reference(navigation.PropertyInfo!.Name);

        if (!referenceEntry.IsLoaded)
        {
            IQueryable query = referenceEntry.Query();
            var elementType = navigation.TargetEntityType.ClrType;
            navValue = await LoadRelationDataAsync(query, elementType, cancellationToken, isCollection: false);
        }

        if (navValue == null)
            return;

        await setEntityAsSoftDeleted((IEntityTimestamps)navValue, cancellationToken, isRoot: false);
    }

    private async Task<object?> LoadRelationDataAsync(
        IQueryable query,
        Type navigationPropertyType,
        CancellationToken cancellationToken,
        bool isCollection
    )
    {
        IQueryable<object>? relationLoaderQuery = GetRelationLoaderQuery(query, navigationPropertyType);

        if (relationLoaderQuery is null)
            return null;

        return isCollection
            ? await relationLoaderQuery.ToListAsync(cancellationToken)
            : await relationLoaderQuery.FirstOrDefaultAsync(cancellationToken);
    }


    public async Task<IPaginate<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        int index = 0,
        int size = 10,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default
    )
    {
        IQueryable<TEntity> queryable = Query();
        queryable = ApplySoftDeleteScope(queryable, withDeleted);
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        if (predicate != null)
            queryable = queryable.Where(predicate);
        if (orderBy != null)
            return await orderBy(queryable).ToPaginateAsync(index, size, from: 0, cancellationToken);
        return await queryable.OrderBy(x => x.Id).ToPaginateAsync(index, size, from: 0, cancellationToken);
    }

    public async Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default
    )
    {
        IQueryable<TEntity> queryable = Query();
        queryable = ApplySoftDeleteScope(queryable, withDeleted);
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        return await queryable.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<IPaginate<TEntity>> GetListByDynamicAsync(
        DynamicQuery dynamic,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        int index = 0,
        int size = 10,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default
    )
    {
        IQueryable<TEntity> queryable = Query().ToDynamic(dynamic);
        queryable = ApplySoftDeleteScope(queryable, withDeleted);
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        if (predicate != null)
            queryable = queryable.Where(predicate);
        if (queryable is not IOrderedQueryable<TEntity>)
            queryable = queryable.OrderBy(x => x.Id);
        return await queryable.ToPaginateAsync(index, size, from: 0, cancellationToken);
    }

    public async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        bool withDeleted = false,
        CancellationToken cancellationToken = default
    )
    {
        IQueryable<TEntity> queryable = Query();
        queryable = ApplySoftDeleteScope(queryable, withDeleted);
        if (predicate != null)
            queryable = queryable.Where(predicate);
        return await queryable.AnyAsync(cancellationToken);
    }

    public TEntity Add(TEntity entity)
    {
        EditEntityPropertiesToAdd(entity);
        Context.Add(entity);
        Context.SaveChanges();
        return entity;
    }

    public ICollection<TEntity> AddRange(ICollection<TEntity> entities)
    {
        foreach (TEntity entity in entities)
            EditEntityPropertiesToAdd(entity);
        Context.AddRange(entities);
        Context.SaveChanges();
        return entities;
    }

    public TEntity Update(TEntity entity)
    {
        EditEntityPropertiesToUpdate(entity);
        if (Context.Entry(entity).State == EntityState.Detached)
            Context.Update(entity);
        Context.SaveChanges();
        return entity;
    }

    public ICollection<TEntity> UpdateRange(ICollection<TEntity> entities)
    {
        foreach (TEntity entity in entities)
            EditEntityPropertiesToUpdate(entity);
        if (entities.Any(e => Context.Entry(e).State == EntityState.Detached))
            Context.UpdateRange(entities);
        Context.SaveChanges();
        return entities;
    }

    public TEntity? Get(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true
    )
    {
        IQueryable<TEntity> queryable = Query();
        queryable = ApplySoftDeleteScope(queryable, withDeleted);
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        return queryable.FirstOrDefault(predicate);
    }

    public IPaginate<TEntity> GetList(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        int index = 0,
        int size = 10,
        bool withDeleted = false,
        bool enableTracking = true
    )
    {
        IQueryable<TEntity> queryable = Query();
        queryable = ApplySoftDeleteScope(queryable, withDeleted);
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        if (predicate != null)
            queryable = queryable.Where(predicate);
        if (orderBy != null)
            return orderBy(queryable).ToPaginate(index, size);
        return queryable.ToPaginate(index, size);
    }

    public IPaginate<TEntity> GetListByDynamic(
        DynamicQuery dynamic,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        int index = 0,
        int size = 10,
        bool withDeleted = false,
        bool enableTracking = true
    )
    {
        IQueryable<TEntity> queryable = Query().ToDynamic(dynamic);
        queryable = ApplySoftDeleteScope(queryable, withDeleted);
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        if (predicate != null)
            queryable = queryable.Where(predicate);
        return queryable.ToPaginate(index, size);
    }

    public bool Any(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        bool withDeleted = false
    )
    {
        IQueryable<TEntity> queryable = Query();
        queryable = ApplySoftDeleteScope(queryable, withDeleted);
        if (predicate != null)
            queryable = queryable.Where(predicate);
        return queryable.Any();
    }


    protected IQueryable<object>? GetRelationLoaderQuery(IQueryable query, Type navigationPropertyType)
    {
        Type queryProviderType = query.Provider.GetType();
        MethodInfo createQueryMethod =
            queryProviderType
                .GetMethods()
                .First(m => m is { Name: nameof(query.Provider.CreateQuery), IsGenericMethod: true })
                ?.MakeGenericMethod(navigationPropertyType)
                ?? throw new InvalidOperationException("CreateQuery<TElement> method is not found in IQueryProvider.");
        var queryProviderQuery = (IQueryable<object>)createQueryMethod.Invoke(query.Provider, parameters: [query.Expression])!;
        return queryProviderQuery.Where(x => !((IEntityTimestamps)x).DeletedDate.HasValue);
    }

    protected void CheckHasEntityHaveOneToOneRelation(TEntity entity)
    {
        IEnumerable<IForeignKey> foreignKeys = Context.Entry(entity).Metadata.GetForeignKeys();
        IForeignKey? oneToOneForeignKey = foreignKeys.FirstOrDefault(fk =>
            fk.IsUnique && fk.PrincipalKey.Properties.All(pk => Context.Entry(entity).Property(pk.Name).Metadata.IsPrimaryKey())
        );

        if (oneToOneForeignKey != null)
        {
            string relatedEntity = oneToOneForeignKey.PrincipalEntityType.ClrType.Name;
            IReadOnlyList<IProperty> primaryKeyProperties = Context.Entry(entity).Metadata.FindPrimaryKey()!.Properties;
            string primaryKeyNames = string.Join(", ", primaryKeyProperties.Select(prop => prop.Name));
            throw new InvalidOperationException(
                $"Entity {entity.GetType().Name} has a one-to-one relationship with {relatedEntity} via the primary key ({primaryKeyNames}). Soft Delete causes problems if you try to create an entry again with the same foreign key."
            );
        }
    }

    protected virtual void EditEntityPropertiesToDelete(TEntity entity)
    {
        entity.DeletedDate = DateTime.UtcNow;
        var httpContextAccessor = ServiceTool.GetService<IHttpContextAccessor>();
        entity.DeletedBy = httpContextAccessor?.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
             ?? httpContextAccessor?.HttpContext?.User?.FindFirst("sub")?.Value
             ?? "System";
    }

    protected virtual void EditRelationEntityPropertiesToCascadeSoftDelete(IEntityTimestamps entity)
    {
        entity.DeletedDate = DateTime.UtcNow;

        if (entity is IAuditable auditable)
        {
            var httpContextAccessor = ServiceTool.GetService<IHttpContextAccessor>();
            auditable.DeletedBy = httpContextAccessor?.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                 ?? httpContextAccessor?.HttpContext?.User?.FindFirst("sub")?.Value
                 ?? "System";
        }
    }

    protected virtual bool IsSoftDeleted(IEntityTimestamps entity)
    {
        return entity.DeletedDate.HasValue;
    }
}
