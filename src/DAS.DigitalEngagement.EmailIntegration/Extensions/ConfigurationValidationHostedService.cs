using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DAS.DigitalEngagement.EmailIntegration.Extensions
{
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    internal sealed class ConfigurationValidationHostedService : IHostedService
    {
        private readonly IValidateOptions<ApplicationConfiguration> _validator;
        private readonly IOptions<ApplicationConfiguration> _options;
        private readonly ILogger<ConfigurationValidationHostedService> _logger;
        private readonly IHostApplicationLifetime _lifetime;

        public ConfigurationValidationHostedService(
            IValidateOptions<ApplicationConfiguration> validator,
            IOptions<ApplicationConfiguration> options,
            ILogger<ConfigurationValidationHostedService> logger,
            IHostApplicationLifetime lifetime)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            ApplicationConfiguration optionsValue;
            try
            {
                optionsValue = _options.Value;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to resolve {OptionsType}.", nameof(ApplicationConfiguration));
                Console.Error.WriteLine($"Failed to resolve {nameof(ApplicationConfiguration)}: {ex.Message}");
                _lifetime.StopApplication();
                return Task.CompletedTask;
            }

            var result = _validator.Validate(Options.DefaultName, optionsValue);

            if (!result.Succeeded)
            {
                _logger.LogCritical("Configuration validation failed. Stopping application.");
                Console.Error.WriteLine("Configuration validation failed. Stopping application.");

                if (result.Failures != null && result.Failures.Any())
                {
                    foreach (var failure in result.Failures)
                    {
                        _logger.LogCritical("Configuration error: {Error}", failure);
                        Console.Error.WriteLine($"Configuration error: {failure}");
                    }
                }
                else if (!string.IsNullOrWhiteSpace(result.FailureMessage))
                {
                    _logger.LogCritical("Configuration error: {Error}", result.FailureMessage);
                    Console.Error.WriteLine("Configuration error: {Error}", result.FailureMessage);
                }

                _lifetime.StopApplication();
                return Task.CompletedTask;
            }

            _logger.LogInformation("Configuration validated successfully.");
            Console.WriteLine("Configuration validated successfully.");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}