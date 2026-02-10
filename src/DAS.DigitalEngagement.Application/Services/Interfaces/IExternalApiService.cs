using DAS.DigitalEngagement.Models.Import;

namespace DAS.DigitalEngagement.Application.Services.Interfaces
{
    public interface IExternalApiService
    {
        Task<string> GetDataAsync(string endpoint);
        Task<BatchResultDetail> PostDataAsync(string endpoint, string csvBodyString);
    }
}