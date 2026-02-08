using DAS.DigitalEngagement.EmailIntegration.Validators;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SFA.DAS.Configuration.AzureTableStorage;
using System.Diagnostics.CodeAnalysis;

namespace DAS.DigitalEngagement.EmailIntegration.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class AddConfigurationExtensions
    {
        public static void AddConfiguration(this IConfigurationBuilder builder, string? contentRootPath = null)
        {
            var basePath = contentRootPath ?? Directory.GetCurrentDirectory();

            builder
                .SetBasePath(basePath)
                .AddJsonFile(Path.Combine(basePath, "local.settings.json"), optional: true, reloadOnChange: true);

            var config = builder.Build();

            builder.AddAzureTableStorage(options =>
            {
                options.ConfigurationKeys = config["ConfigNames"]?.Split(",") ?? Array.Empty<string>();
                options.StorageConnectionString = config["ConfigurationStorageConnectionString"];
                options.EnvironmentName = config["EnvironmentName"];
                options.PreFixConfigurationKeys = false;
            });
        }

        public static IServiceCollection AddApplicationOptions(this IServiceCollection services, IConfiguration configuration)
        {

           services.AddOptions<ApplicationConfiguration>()
                .Configure<IConfiguration>((settings, configuration) =>
                    configuration.Bind(settings));

            // Register validator that returns detailed, per-property errors
            services.AddSingleton<IValidateOptions<ApplicationConfiguration>, ApplicationConfigurationValidator>();
            services.AddHostedService<ConfigurationValidationHostedService>();

            services.AddSingleton(s => s.GetRequiredService<IOptions<ApplicationConfiguration>>().Value);

            services.AddSingleton(s =>
            {
                var appConfig = s.GetRequiredService<IOptions<ApplicationConfiguration>>().Value;
                var conn = appConfig.ConnectionString;
                return Options.Create(conn);
            });

            services.AddSingleton(s =>
            {
                var appConfig = s.GetRequiredService<IOptions<ApplicationConfiguration>>().Value;
                var eShotAPIM = appConfig.EShotAPIM;
                return Options.Create(eShotAPIM);
            });

            services.AddSingleton(sp =>
            {
                var appConfig = sp.GetRequiredService<IOptions<ApplicationConfiguration>>().Value;
                return appConfig.DataMart ?? new List<DataMartSettings>();
            });

            return services;

        }

    }
}
