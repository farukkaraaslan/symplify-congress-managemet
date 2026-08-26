using AutoMapper;
using Core.Application.Rules;
using Core.Persistence.Repositories;
using Core.Test.Application.FakeData;
using Core.Test.Application.Helpers;
using Moq;

namespace Core.Test.Application.Repositories;

public abstract class BaseMockRepository<TRepository, TEntity, TId, TMappingProfile, TBusinessRules, TFakeData>
    where TEntity : Entity<TId>, new()
    where TRepository : class, IAsyncRepository<TEntity, TId>, IRepository<TEntity, TId>
    where TMappingProfile : Profile, new()
    where TBusinessRules : BaseBusinessRules
    where TFakeData : BaseFakeData<TEntity, TId>, new()
    where TId : notnull
{
    public IMapper Mapper;
    public Mock<TRepository> MockRepository;
    public TBusinessRules BusinessRules;

    public BaseMockRepository(TFakeData fakeData)
    {
        MapperConfiguration mapperConfig =
            new(c =>
            {
                c.AddProfile<TMappingProfile>();
            });
        Mapper = mapperConfig.CreateMapper();

        MockRepository = MockRepositoryHelper.GetRepository<TRepository, TEntity, TId>(fakeData.Data);
        BusinessRules = (TBusinessRules)Activator.CreateInstance(type: typeof(TBusinessRules), MockRepository.Object);
    }
}
