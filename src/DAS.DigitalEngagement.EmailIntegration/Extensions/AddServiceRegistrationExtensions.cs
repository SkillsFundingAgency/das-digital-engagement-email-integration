using Azure.Core;
using Azure.Identity;
using DAS.DigitalEngagement.Application.Handlers.Import.Interfaces;
using DAS.DigitalEngagement.Application.Import.Handlers;
using DAS.DigitalEngagement.Application.Repositories;
using DAS.DigitalEngagement.Application.Repositories.Interfaces;
using DAS.DigitalEngagement.Application.Services;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace DAS.DigitalEngagement.EmailIntegration.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class AddServiceRegistrationExtensions
    {
        public static IServiceCollection AddOuterApi(this IServiceCollection services)
        {
            //services.AddScoped<DefaultHeadersHandler>();
            //services.AddScoped<LoggingMessageHandler>();
            //services.AddScoped<ApimHeadersHandler>();

            //var configuration = services
            //    .BuildServiceProvider()
            //    .GetRequiredService<EmployerFeedbackOuterApiConfiguration>();

            //services
            //    .AddRestEaseClient<IEmployerFeedbackOuterApi>(configuration.ApiBaseUrl)
            //    .AddHttpMessageHandler<DefaultHeadersHandler>()
            //    .AddHttpMessageHandler<ApimHeadersHandler>()
            //    .AddHttpMessageHandler<LoggingMessageHandler>();

            //services.AddTransient<IApimClientConfiguration>((_) => configuration);

            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services, string? tenantId)
        {
            services.AddTransient<IImportDataMartHandler, ImportDataMartHandler>();
            services.AddTransient<IImportService, ImportService>();

            services.AddTransient<IDataMartRepository, DataMartRepository>();
            services.AddSingleton<TokenCredential>(sp =>
                                                    new ChainedTokenCredential(
                                                        new ManagedIdentityCredential(),
                                                        new AzureCliCredential(new AzureCliCredentialOptions { TenantId = tenantId }),
                                                        new VisualStudioCodeCredential(new VisualStudioCodeCredentialOptions { TenantId = tenantId }),
                                                        new VisualStudioCredential(new VisualStudioCredentialOptions { TenantId = tenantId })

                                                    ));
            return services;
        }
    }
}
