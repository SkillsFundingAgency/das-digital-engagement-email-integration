namespace DAS.DigitalEngagement.Application.Services.Interfaces
{
    public interface IEmailDomainChecker
    {
        Task<bool> IsValidDomainAsync(string? email, CancellationToken cancellationToken = default);
    }
}
