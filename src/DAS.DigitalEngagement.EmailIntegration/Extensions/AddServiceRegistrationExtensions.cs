using DAS.DigitalEngagement.Application.Handlers.Import.Interfaces;
using DAS.DigitalEngagement.Application.Import.Handlers;
using DAS.DigitalEngagement.Application.Repositories;
using DAS.DigitalEngagement.Application.Repositories.Interfaces;
using DAS.DigitalEngagement.Application.Services;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using SFA.DAS.Encoding;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // services.AddTransient<IEncodingService, EncodingService>();
            services.AddTransient<IImportDataMartHandler, ImportDataMartHandler>();
            services.AddTransient<IImportService, ImportService>();

            services.AddTransient<IDataMartRepository, DataMartRepository>();

            return services;
        }
    }
}
