#nullable disable 

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace DAS.DigitalEngagement.CampaignInterest.Data.Models;

[ExcludeFromCodeCoverage]
public class BouncedEmails
{
    [Key]
    public long Id { get; set; }
    public int ExternalId { get; set; }
    public long CampaignId { get; set; }
    public string ContactEmail { get; set; }
    public DateTime BounceDate { get; set; }
    public string BounceReason { get; set; }
    public string BounceType { get; set; }
    public string ResponseText { get; set; }
}