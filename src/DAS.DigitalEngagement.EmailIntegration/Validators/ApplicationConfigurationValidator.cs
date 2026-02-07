using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAS.DigitalEngagement.EmailIntegration.Validators
{
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

            if (options.DataMart == null || !options.DataMart.Any())
            {
                failures.Add("DataMart: section is missing or contains no entries.");
            }
            else
            {
                for (var i = 0; i < options.DataMart.Count; i++)
                {
                    var dm = options.DataMart[i];
                    if (dm == null)
                    {
                        failures.Add($"DataMart[{i}]: entry is null.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(dm.ViewName))
                        failures.Add($"DataMart[{i}].ViewName: required and cannot be empty.");

                    if (string.IsNullOrWhiteSpace(dm.ObjectName))
                        failures.Add($"DataMart[{i}].ObjectName: required and cannot be empty.");

                    if (string.IsNullOrWhiteSpace(dm.FieldMapping))
                        failures.Add($"DataMart[{i}].FieldMapping: required and cannot be empty.");

                    if (dm.TemplatedUploadId == 0)
                        failures.Add($"DataMart[{i}].TemplatedUploadId: required and cannot be empty.");

                }
            }

            if (options.ConnectionString == null)
                failures.Add("ConnectionString: section is missing.");
            else
            {
                if (string.IsNullOrWhiteSpace(options.ConnectionString.DataMart))
                    failures.Add("ConnectionString.Database: required and cannot be empty.");
            }

            if (options.EShotAPIM == null)
                failures.Add("EShotAPIM: section is missing.");
            else
            {
                if (string.IsNullOrWhiteSpace(options.EShotAPIM.ApiClientId))
                    failures.Add("EShotAPIM.ApiKey: required and cannot be empty.");
                if (string.IsNullOrWhiteSpace(options.EShotAPIM.ApiBaseUrl))
                    failures.Add("EShotAPIM.ApiBaseUrl: required and cannot be empty.");
                if (options.EShotAPIM.ApiRetryCount == 0)
                    failures.Add("EShotAPIM.ApiRetryCount: required and cannot be Zero.");
                if (options.EShotAPIM.ChunkSizeKB == 0)
                    failures.Add("EShotAPIM.ChunkSizeKB: required and cannot be Zero.");
            }

            return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
        }
    }
}
