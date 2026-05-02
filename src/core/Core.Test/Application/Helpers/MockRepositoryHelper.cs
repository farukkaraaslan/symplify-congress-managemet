using System.Linq.Expressions;
using Core.Persistence.Paging;
using Core.Persistence.Repositories;
using Microsoft.EntityFrameworkCore.Query;
using Moq;

namespace Core.Test.Application.Helpers;

public static class MockRepositoryHelper
{
    public static Mock<TRepository> GetRepository<TRepository, TEntity, TId>(List<TEntity> list)
        where TEntity : Entity<TId>, new()
        where TRepository : class, IAsyncRepository<TEntity, TId>, IRepository<TEntity, TId>
        where TId : notnull
    {
        var mockRepo = new Mock<TRepository>();

        Build<TRepository, TEntity, TId>(mockRepo, list);
        return mockRepo;
    }

    private static void Build<TRepository, TEntity, TId>(Mock<TRepository> mockRepo, List<TEntity> entityList)
        where TEntity : Entity<TId>, new()
        where TRepository : class, IAsyncRepository<TEntity, TId>, IRepository<TEntity, TId>
        where TId : notnull
    {
        SetupGetListAsync<TRepository, TEntity, TId>(mockRepo, entityList);
        SetupGetAsync<TRepository, TEntity, TId>(mockRepo, entityList);
        SetupAddAsync<TRepository, TEntity, TId>(mockRepo, entityList);
        SetupUpdateAsync<TRepository, TEntity, TId>(mockRepo, entityList);
        SetupDeleteAsync<TRepository, TEntity, TId>(mockRepo, entityList);
    }

    private static void SetupGetListAsync<TRepository, TEntity, TId>(Mock<TRepository> mockRepo, List<TEntity> entityList)
        where TEntity : Entity<TId>, new()
        where TRepository : class, IAsyncRepository<TEntity, TId>, IRepository<TEntity, TId>
        where TId : notnull
    {
        mockRepo
            .Setup(
                s =>
                    s.GetListAsync(
                        It.IsAny<Expression<Func<TEntity, bool>>>(),
                        It.IsAny<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>>(),
                        It.IsAny<Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<bool>(),
                        It.IsAny<CancellationToken>()
                    )
            )
            .ReturnsAsync(
                (
                    Expression<Func<TEntity, bool>> expression,
                    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy,
                    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include,
                    int index,
                    int size,
                    bool enableTracking,
                    CancellationToken cancellationToken
                ) =>
                {
                    IList<TEntity> list = new List<TEntity>();

                    if (expression == null)
                        list = entityList.Where(x => x.DeletedDate == null).ToList();
                    else
                        list = entityList.Where(x => x.DeletedDate == null).Where(expression.Compile()).ToList();

                    Paginate<TEntity> paginateList = new() { Items = list };
                    return paginateList;
                }
            );
    }

    private static void SetupGetAsync<TRepository, TEntity, TId>(Mock<TRepository> mockRepo, List<TEntity> entityList)
        where TEntity : Entity<TId>, new()
        where TRepository : class, IAsyncRepository<TEntity, TId>, IRepository<TEntity, TId>
        where TId : notnull
    {
        mockRepo
            .Setup(
                s =>
                    s.GetAsync(
                        It.IsAny<Expression<Func<TEntity, bool>>>(),
                        It.IsAny<Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>>(),
                        It.IsAny<bool>(),
                        It.IsAny<CancellationToken>()
                    )
            )
            .ReturnsAsync(
                (
                    Expression<Func<TEntity, bool>> expression,
                    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include,
                    bool enableTracking,
                    CancellationToken cancellationToken
                ) =>
                {
                    TEntity? result = entityList.Where(x => x.DeletedDate == null).FirstOrDefault(predicate: expression.Compile());
                    return result;
                }
            );
    }

    private static void SetupAddAsync<TRepository, TEntity, TId>(Mock<TRepository> mockRepo, List<TEntity> entityList)
        where TEntity : Entity<TId>, new()
        where TRepository : class, IAsyncRepository<TEntity, TId>, IRepository<TEntity, TId>
        where TId : notnull
    {
        mockRepo
            .Setup(r => r.AddAsync(It.IsAny<TEntity>()))
            .ReturnsAsync(
                (TEntity entity) =>
                {
                    entityList.Add(entity);
                    return entity;
                }
            );
    }

    private static void SetupUpdateAsync<TRepository, TEntity, TId>(Mock<TRepository> mockRepo, List<TEntity> entityList)
        where TEntity : Entity<TId>, new()
        where TRepository : class, IAsyncRepository<TEntity, TId>, IRepository<TEntity, TId>
        where TId : notnull
    {
        mockRepo
            .Setup(r => r.UpdateAsync(It.IsAny<TEntity>()))
            .ReturnsAsync(
                (TEntity entity) =>
                {
                    TEntity? result = entityList.FirstOrDefault(x => EqualityComparer<TId>.Default.Equals(x.Id, entity.Id));
                    if (result != null)
                        result = entity;
                    return result;
                }
            );
    }

    private static void SetupDeleteAsync<TRepository, TEntity, TId>(Mock<TRepository> mockRepo, List<TEntity> entityList)
        where TEntity : Entity<TId>, new()
        where TRepository : class, IAsyncRepository<TEntity, TId>, IRepository<TEntity, TId>
        where TId : notnull
    {
        mockRepo
            .Setup(r => r.DeleteAsync(It.IsAny<TEntity>()))
            .ReturnsAsync(
                (TEntity entity) =>
                {
                    DateTime now = DateTime.UtcNow;
                    entity.DeletedDate = now;
                    entity.UpdatedDate = now;
                    return entity;
                }
            );
    }
}
