#nullable disable 

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace DAS.DigitalEngagement.Domain.Models;

[ExcludeFromCodeCoverage]
public class CampaignImportMetadata
{
    [Key]
    public long Id { get; set; }
    public long CampaignId { get; set; }
    public bool IsImportComplete { get; set; }
    public DateTime ImportStartDate { get; set; }
    public DateTime ImportEndDate { get; set; }
}