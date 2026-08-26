using Core.Persistence.Repositories;

namespace Core.Test.Application.FakeData;

public abstract class BaseFakeData<TEntity, TId>
    where TEntity : Entity<TId>, new()
    where TId : notnull
{
    public List<TEntity> Data => CreateFakeData();
    public abstract List<TEntity> CreateFakeData();
}
