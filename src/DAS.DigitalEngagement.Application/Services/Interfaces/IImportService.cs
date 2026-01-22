using System.Collections.Generic;
using System.Threading.Tasks;
using DAS.DigitalEngagement.Models.Import;
namespace DAS.DigitalEngagement.Application.Services.Interfaces
{
    public interface IImportService
    {
        Task<BulkImportStatus> ImportEmployeeRegistration<T>(IList<T> leads);

    }
}
