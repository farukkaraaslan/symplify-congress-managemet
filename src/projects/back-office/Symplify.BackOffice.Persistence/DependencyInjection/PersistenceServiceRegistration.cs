using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Persistence.Contexts;
using Symplify.BackOffice.Persistence.Repositories;
using Symplify.BackOffice.Persistence.Seeding.Abstractions;
using Symplify.BackOffice.Persistence.Seeding.Extensions;
using Symplify.BackOffice.Persistence.Seeding.Seeders;

namespace Symplify.BackOffice.Persistence.DependencyInjection;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddBackOfficePersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<BackOfficeDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddBackOfficePersistenceSeedingServices(configuration);

        services.AddScoped<ILanguageRepository, LanguageRepository>();
        services.AddScoped<IResourceKeyRepository, ResourceKeyRepository>();
        services.AddScoped<IResourceValueRepository, ResourceValueRepository>();

        services.AddScoped<ITopicRepository, TopicRepository>();
        services.AddScoped<ITopicTranslationRepository, TopicTranslationRepository>();

        services.AddScoped<IDocumentTypeRepository, DocumentTypeRepository>();
        services.AddScoped<IDocumentTypeTranslationRepository, DocumentTypeTranslationRepository>();

        services.AddScoped<IEvaluationCriterionRepository, EvaluationCriterionRepository>();
        services.AddScoped<IEvaluationCriterionTranslationRepository, EvaluationCriterionTranslationRepository>();

        services.AddScoped<IEventRoomRepository, EventRoomRepository>();
        services.AddScoped<IEventRoomTranslationRepository, EventRoomTranslationRepository>();

        services.AddScoped<ISubmissionTypeRepository, SubmissionTypeRepository>();
        services.AddScoped<ISubmissionTypeTranslationRepository, SubmissionTypeTranslationRepository>();

        services.AddScoped<ITitleRepository, TitleRepository>();
        services.AddScoped<ITitleTranslationRepository, TitleTranslationRepository>();

        services.AddScoped<ICountryRepository, CountryRepository>();
        services.AddScoped<IStateRepository, StateRepository>();
        services.AddScoped<ICityRepository, CityRepository>();
        services.AddScoped<IRegionRepository, RegionRepository>();

        services.AddScoped<ICountryTranslationRepository, CountryTranslationRepository>();
        services.AddScoped<IStateTranslationRepository, StateTranslationRepository>();
        services.AddScoped<ICityTranslationRepository, CityTranslationRepository>();
        services.AddScoped<IRegionTranslationRepository, RegionTranslationRepository>();

        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ITenantApiKeyRepository, TenantApiKeyRepository>();
        services.AddScoped<ITenantUserRepository, TenantUserRepository>();

        services.AddScoped<ICongressRepository, CongressRepository>();
        services.AddScoped<ICongressTranslationRepository, CongressTranslationRepository>();

        services.AddScoped<ICongressSectionRepository, CongressSectionRepository>();
        services.AddScoped<ICongressSectionTranslationRepository, CongressSectionTranslationRepository>();

        services.AddScoped<ICongressSliderRepository, CongressSliderRepository>();
        services.AddScoped<ICongressSliderTranslationRepository, CongressSliderTranslationRepository>();

        services.AddScoped<ICongressBoardRepository, CongressBoardRepository>();
        services.AddScoped<ICongressBoardTranslationRepository, CongressBoardTranslationRepository>();

        services.AddScoped<ICongressBoardMemberRepository, CongressBoardMemberRepository>();
        services.AddScoped<ICongressBoardMemberTranslationRepository, CongressBoardMemberTranslationRepository>();

        services.AddScoped<ICongressImportantDateRepository, CongressImportantDateRepository>();
        services.AddScoped<ICongressImportantDateTranslationRepository, CongressImportantDateTranslationRepository>();

        services.AddScoped<ICongressPaymentPlanRepository, CongressPaymentPlanRepository>();
        services.AddScoped<ICongressPaymentPlanTranslationRepository, CongressPaymentPlanTranslationRepository>();

        services.AddScoped<ICongressDocumentRepository, CongressDocumentRepository>();

        services.AddScoped<ICongressEvaluationCriterionRepository, CongressEvaluationCriterionRepository>();
        services.AddScoped<ICongressSubmissionTypeRepository, CongressSubmissionTypeRepository>();
        services.AddScoped<ICongressTopicRepository, CongressTopicRepository>();

        services.AddScoped<IAuthorRepository, AuthorRepository>();
        services.AddScoped<IReviewerRepository, ReviewerRepository>();

        services.AddScoped<ISubmissionRepository, SubmissionRepository>();
        services.AddScoped<ISubmissionEvaluationRepository, SubmissionEvaluationRepository>();
        services.AddScoped<IEvaluationScoreRepository, EvaluationScoreRepository>();
        services.AddScoped<ISubmissionHistoryRepository, SubmissionHistoryRepository>();

        services.AddScoped<IPaymentDocumentRepository, PaymentDocumentRepository>();

        services.AddScoped<IPaymentStatusRepository, PaymentStatusRepository>();
        services.AddScoped<IPaymentStatusTranslationRepository, PaymentStatusTranslationRepository>();

        services.AddScoped<ITransactionStatusRepository, TransactionStatusRepository>();
        services.AddScoped<ITransactionStatusTranslationRepository, TransactionStatusTranslationRepository>();

        services.AddScoped<ITransactionStatusTransitionRepository, TransactionStatusTransitionRepository>();
        services.AddScoped<ITransactionStatusTransitionTranslationRepository, TransactionStatusTransitionTranslationRepository>();

        return services;
    }
}