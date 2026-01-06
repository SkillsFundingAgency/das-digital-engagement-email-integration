using DAS.DigitalEngagement.Application.Handlers.Import.Interfaces;
using DAS.DigitalEngagement.Application.Import.Handlers;
using DAS.DigitalEngagement.Application.Repositories;
using DAS.DigitalEngagement.Application.Repositories.Interfaces;
using DAS.DigitalEngagement.Application.Services;
using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Infrastructure;
using EmailIntegration.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Configuration;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();
builder.Configuration.BuildDasConfiguration();

//todo: revisit and see the latest logging implementation
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);
builder.Logging.AddFilter("DAS.EmailIntegration", LogLevel.Information);

builder.Services.AddOptions();
builder.Services.Configure<ConnectionStrings>(builder.Configuration.GetSection("ConnectionStrings"));
builder.Services.Configure<List<DataMartSettings>>(builder.Configuration.GetSection("DataMart"));



builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddTransient<IImportDataMartHandler, ImportDataMartHandler>();
builder.Services.AddTransient<IImportService, ImportService>();

var environment = builder.Configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT");

if (environment == "Development")
{
    builder.Services.AddTransient<IDataMartRepository, DataMartRepository>();
}
else
{
    builder.Services.AddTransient<IDataMartRepository, DataMartRepository>(s =>
        new DataMartRepository(s.GetService<IOptions<ConnectionStrings>>()));
}


builder.Build().Run();
