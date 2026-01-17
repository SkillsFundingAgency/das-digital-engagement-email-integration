using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SFA.DAS.Configuration.AzureTableStorage;
using SFA.DAS.Encoding;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAS.DigitalEngagement.EmailIntegration.Extensions
{
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

        public static IServiceCollection AddApplicationOptions(this IServiceCollection services)
        {
            services
                .AddOptions<ApplicationConfiguration>()
                .Configure<IConfiguration>((settings, configuration) =>
                    configuration.Bind(settings));

            services.AddSingleton(s => s.GetRequiredService<IOptions<ApplicationConfiguration>>().Value);

            services.AddSingleton(s =>
            {
                var configuration = s.GetRequiredService<IConfiguration>();
                var dasEncodingConfig = new EncodingConfig { Encodings = [] };
                configuration.GetSection(nameof(dasEncodingConfig.Encodings)).Bind(dasEncodingConfig.Encodings);
                return dasEncodingConfig;
            });

            // Provide IOptions<ConnectionString> so repository can receive it via DI
            services.AddSingleton<IOptions<ConnectionString>>(s =>
            {
                var appConfig = s.GetRequiredService<IOptions<ApplicationConfiguration>>().Value;
                var conn = appConfig?.ConnectionString ?? new ConnectionString();
                return Options.Create(conn);
            });

            return services;
        }

    }
}
