namespace DAS.DigitalEngagement.Application.Services.Interfaces
{
    public interface IODataPagedImporter
    {
        Task<long> ImportEndpointToTableAsync(string endpointTemplate, string[] destinationTable, CancellationToken cancellationToken = default);
    }
}