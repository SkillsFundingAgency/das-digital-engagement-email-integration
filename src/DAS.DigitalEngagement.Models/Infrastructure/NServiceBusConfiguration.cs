using System.Diagnostics.CodeAnalysis;

namespace DAS.DigitalEngagement.Models.Infrastructure
{
    [ExcludeFromCodeCoverage]
    public class NServiceBusConfiguration
    {
        public string? NServiceBusConnectionString { get; set; }

        public string? NServiceBusLicense
        {
            get => _nServiceBusLicense;
            set => _nServiceBusLicense = System.Net.WebUtility.HtmlDecode(value);
        }

        private string? _nServiceBusLicense;
    }
}
