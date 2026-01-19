namespace DAS.DigitalEngagement.Application.Services.Interfaces
{
    public interface IExternalApiService
    {
        Task<string> GetDataAsync(string endpoint);
        Task<string> PostDataAsync(string endpoint, object body);
    }
}