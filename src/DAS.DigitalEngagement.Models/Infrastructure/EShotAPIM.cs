using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAS.DigitalEngagement.Models.Infrastructure
{

    public class EShotAPIM
    {
        public string? ApiBaseUrl { get; set; }
        public string? ApiClientId { get; set; }
        public int ApiRetryCount { get; set; }
    }

}
