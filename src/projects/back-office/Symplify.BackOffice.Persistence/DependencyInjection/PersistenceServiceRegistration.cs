using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Application.Services.Maintenance;
using Symplify.BackOffice.Persistence.Contexts;
using Symplify.BackOffice.Persistence.Repositories;
using Symplify.BackOffice.Application.Features.Congresses.Cloning;
using Symplify.BackOffice.Application.Features.ParticipationCertificates.Services;
using Symplify.BackOffice.Persistence.Services.Congresses;
using Symplify.BackOffice.Persistence.Services.ParticipationCertificates;
using Symplify.BackOffice.Persistence.Services.Maintenance;
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
        RegisterOrganizationRepositories(services);
        services.AddOrganizationMailPersistenceServices();
        RegisterCongressRepositories(services);
        services.AddCongressContactEmailPersistenceServices();
        RegisterSubmissionRepositories(services);
        RegisterPaymentRepositories(services);
        RegisterWorkflowRepositories(services);
        services.AddScoped<IParticipationCertificateService, ParticipationCertificateService>();
        services.AddScoped<ICongressCleanupService, CongressCleanupService>();

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
        services.AddScoped<IEvaluationScoreOptionRepository, EvaluationScoreOptionRepository>();

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

    private static void RegisterOrganizationRepositories(IServiceCollection services)
    {
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IOrganizationApiKeyRepository, OrganizationApiKeyRepository>();
        services.AddScoped<IOrganizationUserRepository, OrganizationUserRepository>();
    }

    private static void RegisterCongressRepositories(IServiceCollection services)
    {
        services.AddScoped<ICongressRepository, CongressRepository>();
        services.AddScoped<ICongressTranslationRepository, CongressTranslationRepository>();
        services.AddScoped<ICongressAnnouncementRepository, CongressAnnouncementRepository>();
        services.AddScoped<ICongressAnnouncementTranslationRepository, CongressAnnouncementTranslationRepository>();

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
        services.AddScoped<ICongressDocumentTranslationRepository, CongressDocumentTranslationRepository>();

        services.AddScoped<ICongressEvaluationCriterionRepository, CongressEvaluationCriterionRepository>();
        services.AddScoped<ICongressSubmissionTypeRepository, CongressSubmissionTypeRepository>();
        services.AddScoped<ICongressTopicRepository, CongressTopicRepository>();
        services.AddScoped<ICongressTopicCategoryRepository, CongressTopicCategoryRepository>();
        services.AddScoped<ICongressTopicCategoryTranslationRepository, CongressTopicCategoryTranslationRepository>();
        services.AddScoped<ICongressReviewerRepository, CongressReviewerRepository>();
        services.AddScoped<IProgramManagementRepository, ProgramManagementRepository>();
        services.AddScoped<IAbstractBookRepository, AbstractBookRepository>();
        services.AddScoped<ICongressCloneService, CongressCloneService>();
        services.AddScoped<IFullTextBookRepository, FullTextBookRepository>();
    }

    private static void RegisterSubmissionRepositories(IServiceCollection services)
    {
        services.AddScoped<IAuthorRepository, AuthorRepository>();
        services.AddScoped<IReviewerRepository, ReviewerRepository>();

        services.AddScoped<ISubmissionRepository, SubmissionRepository>();
        services.AddScoped<ISubmissionEvaluationRepository, SubmissionEvaluationRepository>();
        services.AddScoped<IEvaluationScoreRepository, EvaluationScoreRepository>();
        services.AddScoped<ISubmissionHistoryRepository, SubmissionHistoryRepository>();
        services.AddScoped<ISubmissionFileRepository, SubmissionFileRepository>();
        services.AddScoped<ISubmissionExhibitionDetailRepository, SubmissionExhibitionDetailRepository>();
        services.AddScoped<ISubmissionAcceptanceLetterRepository, SubmissionAcceptanceLetterRepository>();
        services.AddScoped<IParticipationCertificateRepository, ParticipationCertificateRepository>();
        services.AddScoped<IMailOutboxMessageRepository, MailOutboxMessageRepository>();
        services.AddScoped<IMailDeliveryEventRepository, MailDeliveryEventRepository>();
        services.AddScoped<IShortLinkRepository, ShortLinkRepository>();
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
        services.AddScoped<IWorkflowTransitionConditionRepository, WorkflowTransitionConditionRepository>();
        services.AddScoped<IWorkflowTransitionEffectRepository, WorkflowTransitionEffectRepository>();

        services.AddScoped<ICongressWorkflowSettingRepository, CongressWorkflowSettingRepository>();
        services.AddScoped<ICongressTransactionStatusTransitionRepository, CongressTransactionStatusTransitionRepository>();
    }
}
