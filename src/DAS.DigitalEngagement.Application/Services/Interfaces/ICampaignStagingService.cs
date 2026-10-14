using DAS.DigitalEngagement.CampaignInterest.Data.Models;
using DAS.DigitalEngagement.Models.Campaigns;
using System.Data;

namespace DAS.DigitalEngagement.Application.Services.Interfaces
{
    public interface ICampaignStagingService
    {
        Task<DataTable> GetAllSendsFromEShot(int? subAccountId = null, CancellationToken cancellationToken = default);
        Task<DataTable> GetEligibleSendsAsync(int? subAccountId = null, CancellationToken cancellationToken = default);
        Task<int> BulkInsertSendsAsync(DataTable sends, CancellationToken cancellationToken = default);
        Task<DataSet?> GetSendsAndCampaign(List<long> sendIds, CancellationToken cancellationToken);
        Task<int> BulkInsertCampaignAsync(DataTable? dataTable, CancellationToken cancellationToken);
        DataTable PrepareImportMetaData(DataTable value);
        DataTable UpdateImportMetaData(DataTable insertedImportMetaData);
        Task<int> BulkInsertCampaignImportMetadataAsync(DataTable sends, CancellationToken cancellationToken = default);
        Task ImportSendsAndCampaign(List<long> sendIds, DateTime importStartDateTime);
    }
}