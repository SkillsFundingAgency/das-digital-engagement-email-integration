#nullable disable 

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace DAS.DigitalEngagement.CampaignInterest.Data.Models;

[ExcludeFromCodeCoverage]
public class UnsubscribedContacts
{
    [Key]
    public long Id { get; set; }
    public int ExternalId { get; set; }
    public long CampaignId { get; set; }
    public string ContactEmail { get; set; }
    public DateTime UnsubscribedDate { get; set; }
    public bool IsGlobalUnscribe { get; set; }
    public bool IsComplaint { get; set; }
}