using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Domain.Communication;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Identity;
using Symplify.BackOffice.Domain.Localization;
using Symplify.BackOffice.Domain.Lookups;
using Symplify.BackOffice.Domain.Organization;
using Symplify.BackOffice.Domain.Reference;
using Symplify.BackOffice.Domain.Reference.Translations;
using Symplify.BackOffice.Domain.ShortLinks;
using Symplify.BackOffice.Domain.Submission;
using Symplify.BackOffice.Domain.Workflow;
using PaymentDocumentEntity = Symplify.BackOffice.Domain.Workflow.PaymentDocument;
using PaymentStatusEntity = Symplify.BackOffice.Domain.Workflow.PaymentStatus;
using PaymentStatusTranslationEntity = Symplify.BackOffice.Domain.Workflow.PaymentStatusTranslation;
using TransactionStatusEntity = Symplify.BackOffice.Domain.Workflow.TransactionStatus;
using TransactionStatusTransitionEntity = Symplify.BackOffice.Domain.Workflow.TransactionStatusTransition;
using TransactionStatusTransitionTranslationEntity = Symplify.BackOffice.Domain.Workflow.TransactionStatusTransitionTranslation;
using TransactionStatusTranslationEntity = Symplify.BackOffice.Domain.Workflow.TransactionStatusTranslation;

namespace Symplify.BackOffice.Persistence.Contexts;

public class BackOfficeDbContext : IdentityDbContext<
        AppUser,
        AppRole,
        Guid,
        IdentityUserClaim<Guid>,
        IdentityUserRole<Guid>,
        IdentityUserLogin<Guid>,
        AppRoleClaim,
        IdentityUserToken<Guid>>
{
    public BackOfficeDbContext(DbContextOptions<BackOfficeDbContext> options) : base(options)
    {
    }

    public DbSet<Language> Languages => Set<Language>();
    public DbSet<ResourceKey> ResourceKeys => Set<ResourceKey>();
    public DbSet<ResourceValue> ResourceValues => Set<ResourceValue>();

    public DbSet<Title> Titles => Set<Title>();
    public DbSet<TitleTranslation> TitleTranslations => Set<TitleTranslation>();
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
    public DbSet<DocumentTypeTranslation> DocumentTypeTranslations => Set<DocumentTypeTranslation>();
    public DbSet<SubmissionType> SubmissionTypes => Set<SubmissionType>();
    public DbSet<SubmissionTypeTranslation> SubmissionTypeTranslations => Set<SubmissionTypeTranslation>();
    public DbSet<Topic> Categories => Set<Topic>();
    public DbSet<TopicTranslation> CategoryTranslations => Set<TopicTranslation>();
    public DbSet<EvaluationCriterion> EvaluationCriteria => Set<EvaluationCriterion>();
    public DbSet<EvaluationCriterionTranslation> EvaluationCriterionTranslations => Set<EvaluationCriterionTranslation>();
    public DbSet<EvaluationScoreOption> EvaluationScoreOptions => Set<EvaluationScoreOption>();
    public DbSet<EventRoom> EventRooms => Set<EventRoom>();
    public DbSet<EventRoomTranslation> EventRoomTranslations => Set<EventRoomTranslation>();

    public DbSet<Country> Countries => Set<Country>();
    public DbSet<State> States => Set<State>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<CountryTranslation> CountryTranslations => Set<CountryTranslation>();
    public DbSet<StateTranslation> StateTranslations => Set<StateTranslation>();
    public DbSet<CityTranslation> CityTranslations => Set<CityTranslation>();
    public DbSet<RegionTranslation> RegionTranslations => Set<RegionTranslation>();

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationUser> OrganizationUsers => Set<OrganizationUser>();
    public DbSet<OrganizationApiKey> OrganizationApiKeys => Set<OrganizationApiKey>();

    public DbSet<Congress> Congresses => Set<Congress>();
    public DbSet<CongressTranslation> CongressTranslations => Set<CongressTranslation>();
    public DbSet<CongressContactEmail> CongressContactEmails => Set<CongressContactEmail>();
    public DbSet<CongressAnnouncement> CongressAnnouncements => Set<CongressAnnouncement>();
    public DbSet<CongressAnnouncementTranslation> CongressAnnouncementTranslations => Set<CongressAnnouncementTranslation>();
    public DbSet<CongressSection> CongressSections => Set<CongressSection>();
    public DbSet<CongressSectionTranslation> CongressSectionTranslations => Set<CongressSectionTranslation>();
    public DbSet<CongressSlider> CongressSliders => Set<CongressSlider>();
    public DbSet<CongressSliderTranslation> CongressSliderTranslations => Set<CongressSliderTranslation>();
    public DbSet<CongressBoard> CongressBoards => Set<CongressBoard>();
    public DbSet<CongressBoardTranslation> CongressBoardTranslations => Set<CongressBoardTranslation>();
    public DbSet<CongressBoardMember> CongressBoardMembers => Set<CongressBoardMember>();
    public DbSet<CongressBoardMemberTranslation> CongressBoardMemberTranslations => Set<CongressBoardMemberTranslation>();
    public DbSet<CongressImportantDate> CongressImportantDates => Set<CongressImportantDate>();
    public DbSet<CongressImportantDateTranslation> CongressImportantDateTranslations => Set<CongressImportantDateTranslation>();
    public DbSet<CongressPaymentPlan> CongressPaymentPlans => Set<CongressPaymentPlan>();
    public DbSet<CongressPaymentPlanTranslation> CongressPaymentPlanTranslations => Set<CongressPaymentPlanTranslation>();
    public DbSet<CongressDocument> CongressDocuments => Set<CongressDocument>();
    public DbSet<CongressTopic> CongressTopics => Set<CongressTopic>();
    public DbSet<CongressSubmissionType> CongressSubmissionTypes => Set<CongressSubmissionType>();
    public DbSet<CongressEvaluationCriterion> CongressEvaluationCriteria => Set<CongressEvaluationCriterion>();
    public DbSet<CongressWorkflowSetting> CongressWorkflowSettings => Set<CongressWorkflowSetting>();
    public DbSet<OrganizationMailConfiguration> OrganizationMailConfigurations => Set<OrganizationMailConfiguration>();
    public DbSet<CongressTransactionStatusTransition> CongressTransactionStatusTransitions => Set<CongressTransactionStatusTransition>();
    public DbSet<CongressReviewer> CongressReviewers => Set<CongressReviewer>();
    public DbSet<CongressProgramPlan> CongressProgramPlans => Set<CongressProgramPlan>();
    public DbSet<CongressProgramDay> CongressProgramDays => Set<CongressProgramDay>();
    public DbSet<CongressProgramSession> CongressProgramSessions => Set<CongressProgramSession>();
    public DbSet<CongressProgramFixedBlock> CongressProgramFixedBlocks => Set<CongressProgramFixedBlock>();
    public DbSet<CongressProgramItem> CongressProgramItems => Set<CongressProgramItem>();

    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Reviewer> Reviewers => Set<Reviewer>();
    public DbSet<Domain.Submission.Submission> Submissions => Set<Domain.Submission.Submission>();
    public DbSet<SubmissionEvaluation> SubmissionEvaluations => Set<SubmissionEvaluation>();
    public DbSet<EvaluationScore> EvaluationScores => Set<EvaluationScore>();
    public DbSet<SubmissionHistory> SubmissionHistories => Set<SubmissionHistory>();
    public DbSet<SubmissionFile> SubmissionFiles => Set<SubmissionFile>();
    public DbSet<SubmissionExhibitionDetail> SubmissionExhibitionDetails => Set<SubmissionExhibitionDetail>();
    public DbSet<SubmissionAcceptanceLetter> SubmissionAcceptanceLetters => Set<SubmissionAcceptanceLetter>();
    public DbSet<ParticipationCertificateTemplate> ParticipationCertificateTemplates => Set<ParticipationCertificateTemplate>();
    public DbSet<ParticipationCertificate> ParticipationCertificates => Set<ParticipationCertificate>();
    public DbSet<ParticipationCertificateGenerationJob> ParticipationCertificateGenerationJobs => Set<ParticipationCertificateGenerationJob>();
    public DbSet<ParticipationCertificateGenerationJobItem> ParticipationCertificateGenerationJobItems => Set<ParticipationCertificateGenerationJobItem>();

    public DbSet<PaymentStatusEntity> PaymentStatuses => Set<PaymentStatusEntity>();
    public DbSet<PaymentStatusTranslationEntity> PaymentStatusTranslations => Set<PaymentStatusTranslationEntity>();
    public DbSet<PaymentDocumentEntity> PaymentDocuments => Set<PaymentDocumentEntity>();
    public DbSet<TransactionStatusEntity> TransactionStatuses => Set<TransactionStatusEntity>();
    public DbSet<TransactionStatusTranslationEntity> TransactionStatusTranslations => Set<TransactionStatusTranslationEntity>();
    public DbSet<TransactionStatusTransitionEntity> TransactionStatusTransitions => Set<TransactionStatusTransitionEntity>();
    public DbSet<TransactionStatusTransitionTranslationEntity> TransactionStatusTransitionTranslations => Set<TransactionStatusTransitionTranslationEntity>();
    public DbSet<TransactionStatusPhase> TransactionStatusPhases => Set<TransactionStatusPhase>();
    public DbSet<TransactionStatusPhaseTranslation> TransactionStatusPhaseTranslations => Set<TransactionStatusPhaseTranslation>();
    public DbSet<WorkflowTemplate> WorkflowTemplates => Set<WorkflowTemplate>();
    public DbSet<WorkflowTemplateTranslation> WorkflowTemplateTranslations => Set<WorkflowTemplateTranslation>();
    public DbSet<WorkflowTemplateTransition> WorkflowTemplateTransitions => Set<WorkflowTemplateTransition>();
    public DbSet<WorkflowTransitionCondition> WorkflowTransitionConditions => Set<WorkflowTransitionCondition>();
    public DbSet<WorkflowTransitionEffect> WorkflowTransitionEffects => Set<WorkflowTransitionEffect>();

    public DbSet<MailOutboxMessage> MailOutboxMessages => Set<MailOutboxMessage>();
    public DbSet<MailDeliveryEvent> MailDeliveryEvents => Set<MailDeliveryEvent>();

    public DbSet<ShortLink> ShortLinks => Set<ShortLink>();
    public DbSet<CongressDocumentTranslation>   CongressDocumentTranslations => Set<CongressDocumentTranslation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BackOfficeDbContext).Assembly);
    }
}
