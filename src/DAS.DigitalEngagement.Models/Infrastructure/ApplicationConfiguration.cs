using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DAS.DigitalEngagement.Models.Infrastructure
{
    [ExcludeFromCodeCoverage]
    public class ApplicationConfiguration
    {
        public ConnectionString? ConnectionString { get; set; }
        public Functions? Functions { get; set; }
        public EShotAPIM? EShotAPIM { get; set; }
        public IList<DataMartSettings> DataMart { get; set; } = new List<DataMartSettings>();
    }
}
