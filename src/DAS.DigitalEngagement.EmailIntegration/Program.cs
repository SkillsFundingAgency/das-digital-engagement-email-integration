
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using DAS.DigitalEngagement.EmailIntegration.Extensions;

static class Program
{
    static async Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureFunctionsWebApplication()
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddConfiguration(hostingContext.HostingEnvironment.ContentRootPath);
            })

            .ConfigureServices((context, services) =>
            {
                services.AddApplicationOptions();
                services.AddApplicationServices(context.Configuration);
                services.AddOpenTelemetryRegistration(context.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]!);

            })
            .ConfigureLogging((hostingContext, logging) =>
            {
                logging.SetMinimumLevel(LogLevel.Information);

                logging.AddFilter("Microsoft", LogLevel.Warning);
                logging.AddFilter("System", LogLevel.Warning);
                logging.AddFilter("SFA.DAS.EmployerFeedback.Jobs", LogLevel.Information);
            })
            .Build();

        await host.RunAsync();
    }

}
