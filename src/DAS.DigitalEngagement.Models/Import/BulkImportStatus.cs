using System;
using System.Collections.Generic;
using System.Linq;



namespace DAS.DigitalEngagement.Models.Import
{
    public class BulkImportStatus
    {
        public BulkImportStatus()
        {
            StartTime = DateTime.Now;
            BulkImportJobs = new List<BulkImportJob>();
        }
        public required string Container { get; set; }
        public required string Name { get; set; }
        public required string Id { get; set; } 
        public DateTime StartTime { get; set; }
        public IList<BulkImportJob> BulkImportJobs { get; set; }
        public  double? Duration => (DateTime.Now - StartTime).TotalMilliseconds;
        public ImportStatus Status
        {
            get
            {
                var status = ImportStatus.Queued;


                if (ImportFileIsValid == false || HeaderErrors.Any())
                {
                    status = ImportStatus.ValidationFailed;
                }
                else if (BulkImportJobs.Any(s => s.Status == "Failed"))
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

        public bool ImportFileIsValid { get; set; } = true;
        public required string ValidationError { get; set; }
        public IEnumerable<string> HeaderErrors { get; set; } = new List<string>();
    }
}