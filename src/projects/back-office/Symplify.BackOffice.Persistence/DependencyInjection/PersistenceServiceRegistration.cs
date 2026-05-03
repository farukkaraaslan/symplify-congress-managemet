using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Persistence.Contexts;
using Symplify.BackOffice.Persistence.Repositories;
using Symplify.BackOffice.Persistence.Seeding.Extensions;

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

        RegisterLocalizationRepositories(services);
        RegisterLookupRepositories(services);
        RegisterGeoRepositories(services);
        RegisterTenantRepositories(services);
        RegisterCongressRepositories(services);
        RegisterSubmissionRepositories(services);
        RegisterPaymentRepositories(services);
        RegisterWorkflowRepositories(services);

        return services;
    }

    private static void RegisterLocalizationRepositories(IServiceCollection services)
    {
        services.AddScoped<ILanguageRepository, LanguageRepository>();
        services.AddScoped<IResourceKeyRepository, ResourceKeyRepository>();
        services.AddScoped<IResourceValueRepository, ResourceValueRepository>();
    }

    private static void RegisterLookupRepositories(IServiceCollection services)
    {
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
    }

    private static void RegisterGeoRepositories(IServiceCollection services)
    {
        services.AddScoped<ICountryRepository, CountryRepository>();
        services.AddScoped<IStateRepository, StateRepository>();
        services.AddScoped<ICityRepository, CityRepository>();
        services.AddScoped<IRegionRepository, RegionRepository>();

        services.AddScoped<ICountryTranslationRepository, CountryTranslationRepository>();
        services.AddScoped<IStateTranslationRepository, StateTranslationRepository>();
        services.AddScoped<ICityTranslationRepository, CityTranslationRepository>();
        services.AddScoped<IRegionTranslationRepository, RegionTranslationRepository>();
    }

    private static void RegisterTenantRepositories(IServiceCollection services)
    {
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ITenantApiKeyRepository, TenantApiKeyRepository>();
        services.AddScoped<ITenantUserRepository, TenantUserRepository>();
    }

    private static void RegisterCongressRepositories(IServiceCollection services)
    {
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
    }

    private static void RegisterSubmissionRepositories(IServiceCollection services)
    {
        services.AddScoped<IAuthorRepository, AuthorRepository>();
        services.AddScoped<IReviewerRepository, ReviewerRepository>();

        services.AddScoped<ISubmissionRepository, SubmissionRepository>();
        services.AddScoped<ISubmissionEvaluationRepository, SubmissionEvaluationRepository>();
        services.AddScoped<IEvaluationScoreRepository, EvaluationScoreRepository>();
        services.AddScoped<ISubmissionHistoryRepository, SubmissionHistoryRepository>();
    }

    private static void RegisterPaymentRepositories(IServiceCollection services)
    {
        services.AddScoped<IPaymentDocumentRepository, PaymentDocumentRepository>();

        services.AddScoped<IPaymentStatusRepository, PaymentStatusRepository>();
        services.AddScoped<IPaymentStatusTranslationRepository, PaymentStatusTranslationRepository>();
    }

    private static void RegisterWorkflowRepositories(IServiceCollection services)
    {
        services.AddScoped<ITransactionStatusPhaseRepository, TransactionStatusPhaseRepository>();
        services.AddScoped<ITransactionStatusPhaseTranslationRepository, TransactionStatusPhaseTranslationRepository>();

        services.AddScoped<ITransactionStatusRepository, TransactionStatusRepository>();
        services.AddScoped<ITransactionStatusTranslationRepository, TransactionStatusTranslationRepository>();

        services.AddScoped<ITransactionStatusTransitionRepository, TransactionStatusTransitionRepository>();
        services.AddScoped<ITransactionStatusTransitionTranslationRepository, TransactionStatusTransitionTranslationRepository>();

        services.AddScoped<IWorkflowTemplateRepository, WorkflowTemplateRepository>();
        services.AddScoped<IWorkflowTemplateTranslationRepository, WorkflowTemplateTranslationRepository>();
        services.AddScoped<IWorkflowTemplateTransitionRepository, WorkflowTemplateTransitionRepository>();

        services.AddScoped<ICongressWorkflowSettingRepository, CongressWorkflowSettingRepository>();
        services.AddScoped<ICongressTransactionStatusTransitionRepository, CongressTransactionStatusTransitionRepository>();
    }
}