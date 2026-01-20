using Azure.Core;
using Azure.Identity;
using DAS.DigitalEngagement.Application.Handlers.Import.Interfaces;
using DAS.DigitalEngagement.Application.Import.Handlers;
using DAS.DigitalEngagement.Application.Repositories;
using DAS.DigitalEngagement.Application.Repositories.Interfaces;
using DAS.DigitalEngagement.Application.Services;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace DAS.DigitalEngagement.EmailIntegration.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class AddServiceRegistrationExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<IImportDataMartHandler, ImportDataMartHandler>();
            services.AddTransient<IImportService, ImportService>();

            services.AddTransient<IDataMartRepository, DataMartRepository>();
            string? tenantId = configuration.GetSection("TenantId").Value ?? throw new ConfigurationErrorsException("TenantId is not configured");
            services.AddSingleton<TokenCredential>(sp =>
                                                    new ChainedTokenCredential(
                                                        new ManagedIdentityCredential(),
                                                        new AzureCliCredential(new AzureCliCredentialOptions { TenantId = tenantId }),
                                                        new VisualStudioCodeCredential(new VisualStudioCodeCredentialOptions { TenantId = tenantId }),
                                                        new VisualStudioCredential(new VisualStudioCredentialOptions { TenantId = tenantId })

                                                    ));
            services.AddHttpClient<IExternalApiService,ExternalApiService>();
            return services;
        }
    }
}
