#nullable disable 

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace DAS.DigitalEngagement.Domain.Models;

[ExcludeFromCodeCoverage]
public class DisplayedEmails
{
    [Key]
    public long Id { get; set; }
    public int ExternalId { get; set; }
    public long CampaignId { get; set; }
    public string ContactEmail { get; set; }
    public DateTime DisplayedDate { get; set; }
    public string Format { get; set; }
    public int TimeDisplayed { get; set; }
    public bool IsSuspectedBot { get; set; }
    public string Device { get; set; }
    public string ClientName { get; set; }
    public string Os { get; set; }
    public string OsFamily { get; set; }
    public string IpAddress { get; set; }
    public string ClientType { get; set; }
    public string ClientFamily { get; set; }
}