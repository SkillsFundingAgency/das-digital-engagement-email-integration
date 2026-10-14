using DAS.DigitalEngagement.Models.Import;

namespace DAS.DigitalEngagement.Application.Services.Interfaces
{
    public interface IExternalApiService
    {
        Task<string> GetDataAsync(string endpoint);
        Task<string> GetDataAsync(string endpoint, CancellationToken cancellationToken);
        Task<BatchResultDetail> PostDataAsync(string endpoint, string csvBodyString);
    }
}