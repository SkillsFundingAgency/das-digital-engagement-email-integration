using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Import;
using System.Net.Http.Headers;

namespace DAS.DigitalEngagement.Application.Services
{
    public class ImportService : IImportService
    {
        public Task<string> GetFailures(int jobId)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetWarnings(int jobId)
        {
            throw new NotImplementedException();
        }

        public async Task<BulkImportStatus> ImportEmployeeRegistration<T>(IList<T> leads)
        {
            var fileStatus = new BulkImportStatus()
            {
                BulkImportJobStatus = new List<BulkImportJobStatus>(),
                Container = "Test",
                Id = "1",
                Name = "Test",
                ValidationError = "Test",
               
            };

            // ToDo : Call the API and return the result

            fileStatus.BulkImportJobs.Add(new BulkImportJob (){ batchId=1,ImportId="1",Status="Failed" });

            return await Task.FromResult(fileStatus);
        }
    }
}
