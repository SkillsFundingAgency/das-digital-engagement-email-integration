#nullable disable 

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace DAS.DigitalEngagement.CampaignInterest.Data.Models;

[ExcludeFromCodeCoverage]
public class CampaignImportMetadata
{
    [Key]
    public int Id { get; set; }
    public int SendId { get; set; }
    public int? CampaignId { get; set; }
    public bool IsImportComplete { get; set; }
    public DateTime ImportStartDate { get; set; }
    public DateTime? ImportEndDate { get; set; }
}   