using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAS.DigitalEngagement.Application.Handlers.CampaignToStaging
{
    public interface IImportCampaignStagingHandler
    {
        Task Handle(CancellationToken cancellationToken = default);
    }
}
