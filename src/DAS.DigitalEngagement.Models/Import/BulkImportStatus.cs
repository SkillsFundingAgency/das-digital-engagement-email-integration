using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;



namespace DAS.DigitalEngagement.Models.Import
{
    [ExcludeFromCodeCoverage]
    public class BulkImportStatus
    {
        public BulkImportStatus()
        {
            StartTime = DateTime.Now;
            BulkImportJobs = new List<BulkImportJob>();
        }
        public required string Name { get; set; }
        public required string Id { get; set; }
        public int? ContactImportTemplate { get; set; }
        public DateTime StartTime { get; set; }
        public IList<BulkImportJob> BulkImportJobs { get; set; }
        public  double? Duration => (DateTime.Now - StartTime).TotalMilliseconds;
        public ImportStatus Status
        {
            get
            {
                var status = ImportStatus.Queued;

                if (BulkImportJobs.Any(s => s.Status == "Failed"))
                {
                    status = ImportStatus.Failed;
                }
                else if (BulkImportJobs.Any(s => s.Status == "Importing"))
                {
                    status = ImportStatus.Processing;
                }
                else if (BulkImportJobs.All(s => s.Status == "Complete"))
                {
                    status = ImportStatus.Completed;
                }

                return status;
            }
        }

        public required List<BulkImportJobStatus> BulkImportJobStatus { get; set; }
    }
}