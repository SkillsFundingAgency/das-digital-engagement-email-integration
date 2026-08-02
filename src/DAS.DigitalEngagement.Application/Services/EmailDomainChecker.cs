using System.Collections.Concurrent;
using DnsClient;
using DnsClient.Protocol;
using DAS.DigitalEngagement.Application.Services.Interfaces;

namespace DAS.DigitalEngagement.Application.Services
{
    public class EmailDomainChecker : IEmailDomainChecker
    {
        private readonly LookupClient _lookupClient;
        private readonly ConcurrentDictionary<string, bool> _cache = new();

        public EmailDomainChecker()
        {

            _lookupClient = new LookupClient();
        }

        public async Task<bool> IsValidDomainAsync(string? email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var domain = email.Contains('@') ? email.Split('@', 2)[1] : email;
            if (string.IsNullOrWhiteSpace(domain))
                return false;

            if (_cache.TryGetValue(domain, out var cached))
                return cached;

            try
            {
                var result = await _lookupClient.QueryAsync(domain, QueryType.MX, cancellationToken: cancellationToken);
                var hasMx = result.Answers.Any(a => a.RecordType == ResourceRecordType.MX);
                _cache[domain] = hasMx;
                return hasMx;
            }
            catch
            {
                _cache[domain] = false;
                return false;
            }
        }
    }
}