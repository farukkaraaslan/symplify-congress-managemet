using System.Reflection;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Pipelines.Logging;
using Core.Application.Pipelines.Transaction;
using Core.Application.Pipelines.Validation;
using Core.Application.Rules;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Symplify.BackOffice.Application.Features.BulkEmails.Services;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Email;
using Symplify.BackOffice.Application.Services.Mailing;
using Symplify.BackOffice.Application.Services.Workflow;
using Symplify.BackOffice.Application.Features.ProgramManagement.Services;
using Symplify.BackOffice.Application.Features.AbstractBook.Services;
using Symplify.BackOffice.Application.Features.FullTextBook.Services;
using Symplify.BackOffice.Application.Features.ParticipationCertificates.Services;

namespace Symplify.BackOffice.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddBackOfficeApplicationServices(this IServiceCollection services)
    {
        Assembly assembly = typeof(ApplicationServiceRegistration).Assembly;

        services.AddAutoMapper(config =>
        {
            config.AddMaps(assembly);
        });

        services.AddValidatorsFromAssembly(assembly);

        services.AddSubClassesOfType(assembly, typeof(BaseBusinessRules));

        RegisterApplicationServices(services);

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);

            configuration.AddOpenBehavior(typeof(AuthorizationBehavior<,>));
            configuration.AddOpenBehavior(typeof(CachingBehavior<,>));
            configuration.AddOpenBehavior(typeof(CacheRemovingBehavior<,>));
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(RequestValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(TransactionScopeBehavior<,>));
        });

        return services;
    }

    private static void RegisterApplicationServices(IServiceCollection services)
    {
        services.AddScoped<IApplicationLanguageProvider, ApplicationLanguageProvider>();
        services.AddScoped<ICurrentLanguageProvider, CurrentLanguageProvider>();
        services.AddScoped<ITranslationFallbackResolver, TranslationFallbackResolver>();

        // Organization-resolved mail template and sender services.
        services.AddScoped<ISystemMailTemplateRenderer, SystemMailTemplateRenderer>();
        services.AddScoped<IMailBrandingResolver, MailBrandingResolver>();
        services.AddScoped<IOrganizationMailConfigurationResolver, OrganizationMailConfigurationResolver>();
        services.AddScoped<IApplicationMailQueueService, ApplicationMailQueueService>();
        services.AddScoped<IBulkEmailRecipientResolver, BulkEmailRecipientResolver>();
        services.AddScoped<IBulkEmailBodyRenderer, BulkEmailBodyRenderer>();
        services.AddScoped<IBulkEmailComposer, BulkEmailComposer>();

        services.AddScoped<IWorkflowTemplateCopyService, WorkflowTemplateCopyService>();
        services.AddScoped<IWorkflowEngine, WorkflowEngine>();
        services.AddScoped<IWorkflowConditionEvaluator, WorkflowConditionEvaluator>();
        services.AddScoped<IWorkflowEffectProcessor, WorkflowEffectProcessor>();
        services.AddScoped<IAcceptanceLetterPdfRenderer, PdfAcroFormAcceptanceLetterPdfRenderer>();
        services.AddScoped<IAcceptanceLetterService, AcceptanceLetterService>();
        services.AddScoped<IMailOutboxService, MailOutboxService>();
        services.AddScoped<ICongressProgramScheduler, CongressProgramScheduler>();
        services.AddScoped<IProgramDraftPdfRenderer, ProgramDraftPdfRenderer>();
        services.AddScoped<IProgramDraftWordRenderer, ProgramDraftWordRenderer>();
        services.AddScoped<IAbstractBookDocumentBuilder, AbstractBookDocumentBuilder>();
        services.AddScoped<IFullTextBookDocumentBuilder, FullTextBookDocumentBuilder>();
        services.AddScoped<IAbstractBookPdfRenderer, AbstractBookPdfRenderer>();
        services.AddScoped<AbstractBookWordRenderer>();
        services.AddScoped<IAbstractBookWordRenderer>(provider => provider.GetRequiredService<AbstractBookWordRenderer>());
        services.AddScoped<IFullTextBookWordRenderer>(provider => provider.GetRequiredService<AbstractBookWordRenderer>());
        services.AddScoped<IParticipationCertificatePdfRenderer, ParticipationCertificatePdfRenderer>();
    }

    public static IServiceCollection AddSubClassesOfType(
        this IServiceCollection services,
        Assembly assembly,
        Type type,
        Func<IServiceCollection, Type, IServiceCollection>? addWithLifeCycle = null)
    {
        List<Type> types = assembly
            .GetTypes()
            .Where(t =>
                t.IsClass &&
                !t.IsAbstract &&
                t.IsSubclassOf(type) &&
                t != type)
            .ToList();

        foreach (Type item in types)
        {
            if (addWithLifeCycle is null)
            {
                services.AddScoped(item);
                continue;
            }

            addWithLifeCycle(services, item);
        }

        return services;
    }
}
