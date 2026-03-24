using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAS.DigitalEngagement.Models.Infrastructure;

[ExcludeFromCodeCoverage]
public class EmailMarketingApi
{
    public required string ApiBaseUrl { get; set; }
    public required string ApiKey { get; set; }
    public required int ApiRetryCount { get; set; }
    public required int ChunkSizeKB { get; set; } = 10240; // Default to 10 MB
    public int PageSize { get; set; } = 5000; // Default page size for OData pagination (max supported by e-shot)
}
