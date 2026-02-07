using DAS.DigitalEngagement.Models.Import;
using DAS.DigitalEngagement.Models.Infrastructure;

namespace DAS.DigitalEngagement.Application.Handlers.Import.Interfaces
{
    public interface IImportDataMartHandler
    {
        Task<ImportSummaryResult> Handle(IList<DataMartSettings> config);
    }
}
