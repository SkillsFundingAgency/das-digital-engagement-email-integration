namespace DAS.DigitalEngagement.Application.Services.Interfaces
{
    public interface IExternalApiService
    {
        Task<string> GetDataAsync(string endpoint);
        Task<string> PostDataAsync(string endpoint, Stream csvBodyStream);
        Task<string> PostDataAsync(string endpoint, string csvBodyString);
    }
}