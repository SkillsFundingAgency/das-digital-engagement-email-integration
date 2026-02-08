using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;

namespace DAS.DigitalEngagement.EmailIntegration.Validators
{
    [ExcludeFromCodeCoverage]
    public class ApplicationConfigurationValidator : IValidateOptions<ApplicationConfiguration>
    {
        public ValidateOptionsResult Validate(string? name, ApplicationConfiguration? options)
        {
            var failures = new List<string>();

            if (options == null)
            {
                failures.Add("ApplicationConfiguration: section is missing or could not be bound.");
                return ValidateOptionsResult.Fail(failures);
            }

            ValidateDataMart(options.DataMart, failures);
            ValidateConnectionString(options.ConnectionString, failures);
            ValidateEShotAPIM(options.EShotAPIM, failures);

            return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
        }

        private void ValidateDataMart(IList<DataMartSettings>? dataMart, List<string> failures)
        {
            if (dataMart == null || !dataMart.Any())
            {
                failures.Add("DataMart: section is missing or contains no entries.");
                return;
            }

            for (var i = 0; i < dataMart.Count; i++)
            {
                var dm = dataMart[i];
                if (dm == null)
                {
                    failures.Add($"DataMart[{i}]: entry is null.");
                    continue;
                }

                AddIfNullOrWhiteSpace(dm.ViewName, $"DataMart[{i}].ViewName: required and cannot be empty.", failures);
                AddIfNullOrWhiteSpace(dm.ObjectName, $"DataMart[{i}].ObjectName: required and cannot be empty.", failures);
                AddIfNullOrWhiteSpace(dm.FieldMapping, $"DataMart[{i}].FieldMapping: required and cannot be empty.", failures);
                if (dm.TemplatedUploadId == 0)
                    failures.Add($"DataMart[{i}].TemplatedUploadId: required and cannot be empty.");

            }
        }

        private void ValidateConnectionString(ConnectionString? connectionString, List<string> failures)
        {
            if (connectionString == null)
            {
                failures.Add("ConnectionString: section is missing.");
                return;
            }

            AddIfNullOrWhiteSpace(connectionString.DataMart, "ConnectionString.Database: required and cannot be empty.", failures);
        }

        private void ValidateEShotAPIM(EShotAPIM? eShotAPIM, List<string> failures)
        {
            if (eShotAPIM == null)
            {
                failures.Add("EShotAPIM: section is missing.");
                return;
            }

            AddIfNullOrWhiteSpace(eShotAPIM.ApiClientId, "EShotAPIM.ApiKey: required and cannot be empty.", failures);
            AddIfNullOrWhiteSpace(eShotAPIM.ApiBaseUrl, "EShotAPIM.ApiBaseUrl: required and cannot be empty.", failures);
            if (eShotAPIM.ApiRetryCount == 0)
                failures.Add("EShotAPIM.ApiRetryCount: required and cannot be Zero.");
            if (eShotAPIM.ChunkSizeKB == 0)
                failures.Add("EShotAPIM.ChunkSizeKB: required and cannot be Zero.");
        }

        private void AddIfNullOrWhiteSpace(string? value, string message, List<string> failures)
        {
            if (string.IsNullOrWhiteSpace(value))
                failures.Add(message);
        }
    }
}
