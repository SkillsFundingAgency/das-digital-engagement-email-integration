using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using DAS.DigitalEngagement.Models.Infrastructure;
using SFA.DAS.Notifications.Messages.Commands;
using SFA.DAS.NServiceBus.Configuration;
using SFA.DAS.NServiceBus.Configuration.AzureServiceBus;
using SFA.DAS.NServiceBus.Configuration.NewtonsoftJsonSerializer;
using SFA.DAS.NServiceBus.Hosting;

namespace DAS.DigitalEngagement.EmailIntegration.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class AddNServiceBusExtension
    {
        public const string NotificationsMessageHandlerEndpoint = "SFA.DAS.Notifications.MessageHandlers";
        public const string EndpointName = "DAS.DigitalEngagement.EmailIntegration";

        public static IServiceCollection AddNServiceBus(this IServiceCollection services, IConfiguration configuration)
        {
            return services
                .AddSingleton(p =>
                {
                    var nServiceBusConfiguration = new NServiceBusConfiguration();
                    configuration.GetSection(nameof(NServiceBusConfiguration)).Bind(nServiceBusConfiguration);

                    var endpointConfiguration = new EndpointConfiguration(EndpointName)
                        .UseErrorQueue($"{EndpointName}-errors")
                        .UseLicense(nServiceBusConfiguration.NServiceBusLicense)
                        .UseMessageConventions()
                        .UseNewtonsoftJsonSerializer();

                    endpointConfiguration.SendOnly();

                    endpointConfiguration.UseAzureServiceBusTransport(
                        nServiceBusConfiguration.NServiceBusConnectionString,
                        routing => routing.RouteToEndpoint(typeof(SendEmailCommand), NotificationsMessageHandlerEndpoint));

                    return Endpoint.Start(endpointConfiguration).GetAwaiter().GetResult();
                })
                .AddSingleton<IMessageSession>(p => p.GetRequiredService<IEndpointInstance>())
                .AddHostedService<NServiceBusHostedService>();
        }
    }
}
