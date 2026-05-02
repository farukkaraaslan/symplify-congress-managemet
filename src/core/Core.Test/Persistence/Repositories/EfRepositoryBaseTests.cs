using Core.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Core.Test.Persistence.Repositories;

public class EfRepositoryBaseTests
{
    [Xunit.Fact]
    public async Task AddAsync_ShouldPersistEntity()
    {
        await using TestDbContext context = CreateContext();
        EfRepositoryBase<TestEntity, TestDbContext, int> repository = new(context);

        TestEntity entity = new() { Id = 1, Name = "alpha", CreatedDate = DateTime.UtcNow };
        await repository.AddAsync(entity);

        TestEntity? persisted = await context.Entities.AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1);

        Assert.NotNull(persisted);
        Assert.Equal("alpha", persisted.Name);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteEntity()
    {
        await using TestDbContext context = CreateContext();
        EfRepositoryBase<TestEntity, TestDbContext, int> repository = new(context);

        TestEntity entity = new() { Id = 2, Name = "beta", CreatedDate = DateTime.UtcNow };
        context.Entities.Add(entity);
        await context.SaveChangesAsync();

        await repository.DeleteAsync(entity);

        TestEntity? fromFilteredQuery = await repository.Query().FirstOrDefaultAsync(x => x.Id == 2);
        TestEntity? fromDatabase = await context.Entities.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == 2);

        Assert.Null(fromFilteredQuery);
        Assert.NotNull(fromDatabase);
        Assert.NotNull(fromDatabase.DeletedDate);
        Assert.NotNull(fromDatabase.UpdatedDate);
    }

    [Fact]
    public async Task GetAsync_ShouldNotReturnSoftDeletedEntity()
    {
        await using TestDbContext context = CreateContext();
        EfRepositoryBase<TestEntity, TestDbContext, int> repository = new(context);

        TestEntity deleted = new() { Id = 3, Name = "deleted", CreatedDate = DateTime.UtcNow, DeletedDate = DateTime.UtcNow };
        TestEntity active = new() { Id = 4, Name = "active", CreatedDate = DateTime.UtcNow };

        context.Entities.AddRange(deleted, active);
        await context.SaveChangesAsync();

        TestEntity? deletedResult = await repository.GetAsync(x => x.Id == 3);
        TestEntity? activeResult = await repository.GetAsync(x => x.Id == 4);

        Assert.Null(deletedResult);
        Assert.NotNull(activeResult);
    }

    [Fact]
    public async Task AnyAsync_ShouldIgnoreSoftDeletedRows()
    {
        await using TestDbContext context = CreateContext();
        EfRepositoryBase<TestEntity, TestDbContext, int> repository = new(context);

        context.Entities.AddRange(
            new TestEntity { Id = 5, Name = "gone", CreatedDate = DateTime.UtcNow, DeletedDate = DateTime.UtcNow },
            new TestEntity { Id = 6, Name = "live", CreatedDate = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        bool hasDeleted = await repository.AnyAsync(x => x.Name == "gone");
        bool hasActive = await repository.AnyAsync(x => x.Name == "live");

        Assert.False(hasDeleted);
        Assert.True(hasActive);
    }

    private static TestDbContext CreateContext()
    {
        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"efrepo-tests-{Guid.NewGuid()}")
            .Options;

        return new TestDbContext(options);
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<TestEntity> Entities => Set<TestEntity>();
    }

    private sealed class TestEntity : Entity<int>
    {
        public required string Name { get; set; }
    }
}
