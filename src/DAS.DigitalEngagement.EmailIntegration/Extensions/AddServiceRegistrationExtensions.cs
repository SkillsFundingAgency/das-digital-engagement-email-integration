using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using DAS.DigitalEngagement.Application.Handlers.Campaigns;
using DAS.DigitalEngagement.Application.Handlers.Import.Interfaces;
using DAS.DigitalEngagement.Application.Import.Handlers;
using DAS.DigitalEngagement.Application.Repositories;
using DAS.DigitalEngagement.Application.Repositories.Interfaces;
using DAS.DigitalEngagement.Application.Services;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.CampaignInterest.Data.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DAS.DigitalEngagement.Models.Infrastructure;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;


namespace DAS.DigitalEngagement.EmailIntegration.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class AddServiceRegistrationExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            string? azureBlobStorage = configuration.GetSection("AzureWebJobsStorage").Value ?? throw new ConfigurationErrorsException("AzureWebJobsStorage is not configured");
            string? tenantId = configuration.GetSection("TenantId").Value ?? throw new ConfigurationErrorsException("TenantId is not configured");

            services.AddTransient<IImportDataMartHandler, ImportDataMartHandler>();
            services.AddTransient<IImportService, ImportService>();
            services.AddTransient<IPayLoadMapper, PayLoadMapper>();

            services.AddTransient<IImportCampaignPerformanceHandler, ImportCampaignPerformanceHandler>();
            services.AddTransient<IUnitOfWork, UnitOfWork>();
            services.AddTransient<ICampaignService, CampaignService>();

            services.AddTransient<IDataMartRepository, DataMartRepository>();
           
            services.AddSingleton<TokenCredential>(sp =>
                                                    new ChainedTokenCredential(
                                                        new ManagedIdentityCredential(),
                                                        new AzureCliCredential(new AzureCliCredentialOptions { TenantId = tenantId }),
                                                        new VisualStudioCodeCredential(new VisualStudioCodeCredentialOptions { TenantId = tenantId }),
                                                        new VisualStudioCredential(new VisualStudioCredentialOptions { TenantId = tenantId })
                                                    ));
            services.AddHttpClient<IExternalApiService,ExternalApiService>();
            services.AddTransient<IChunkingService, ChunkingService>();
            services.AddTransient<ICsvService, CsvService>();
            services.AddTransient<IReportService, ReportService>();
            services.AddSingleton(provider => new BlobServiceClient(azureBlobStorage));

            // Add GovNotify configuration
            services.AddSingleton<GovNotifyConfiguration>(sp => 
                sp.GetRequiredService<IOptions<GovNotifyConfiguration>>().Value);
            
            // Register email notification service
            services.AddTransient<IEmailNotificationService, EmailNotificationService>();
            
            return services;
        }
    }
}
