#nullable disable 

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace DAS.DigitalEngagement.Domain.Models;

[ExcludeFromCodeCoverage]
public class Campaigns
{
    [Key]
    public long Id { get; set; }
    public int ExternalId { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public string ModifiedBy { get; set; }
    public DateTime ModifiedOn { get; set; }
    public DateTime FirstSendDate { get; set; }
    public DateTime LastSendDate { get; set; }
    public string FromEmailAddress { get; set; }
    public string FromName { get; set; }
    public string ReplyEmailAddress { get; set; }
    public string Subject { get; set; }
    public string SubStatus { get; set; }
    public int ContactCount { get; set; }
    public string Account { get; set; }
}