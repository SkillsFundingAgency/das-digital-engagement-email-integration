using Microsoft.Extensions.Configuration;
using SFA.DAS.Configuration.AzureTableStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailIntegration.Extensions
{
    public static class ConfigurationExtensions
    {

        public static IConfiguration BuildDasConfiguration(this IConfigurationBuilder configBuilder)
        {
            // Set base path and add environment variables
            configBuilder.SetBasePath(AppContext.BaseDirectory)
                         .AddEnvironmentVariables();

#if DEBUG
            // Add local settings for development
            configBuilder.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
#endif

            // Build initial configuration to read keys for Azure Table Storage
            var configuration = configBuilder.Build();

            // Add Azure Table Storage configuration provider
            configBuilder.AddAzureTableStorage(options =>
            {
                var configKeys = configuration["configName"]?.Split(",") ?? Array.Empty<string>();
                options.ConfigurationKeys = configKeys;
                options.StorageConnectionString = configuration["ConfigurationStorageConnectionString"];
                options.EnvironmentName = configuration["EnvironmentName"];
                options.PreFixConfigurationKeys = false;
            });

            // Return final configuration
            return configBuilder.Build();
        }
    }
}

