using DAS.DigitalEngagement.Application.Services.Interfaces;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAS.DigitalEngagement.Application.Services
{
    public class ChunkingService : IChunkingService
    {
        public const int BytesInKB = 1000;
        public double maxDensity { get; set; } = 0.95;

       
        private IOptions<EShotAPIM> _eShotAPIMOptions;

        public ChunkingService(IOptions<EShotAPIM> eShotAPIMOptions)
        {
            _eShotAPIMOptions = eShotAPIMOptions;
        }
    

        private int CalculateChunkSize(int itemCount, long myBlobLength)
        {
            var maxSize = _eShotAPIMOptions.Value.ChunkSizeKB * BytesInKB;

            //Calculate the total number of items in a chunk. This allows for 5% just in case some items in the list are larger than the average.   
            var totalChunks = (int)Math.Ceiling(myBlobLength / (maxSize * maxDensity));

            var chunkSize = itemCount / totalChunks;

            return chunkSize;
        }

        private IEnumerable<IList<T>> SplitList<T>(List<T> locations, int nSize)
        {
            for (int i = 0; i < locations.Count; i += nSize)
            {
                yield return locations.GetRange(i, Math.Min(nSize, locations.Count - i));
            }
        }

        public IEnumerable<IList<T>> GetChunks<T>(long totalSize, IList<T> items)
        {
            var chunkSize = CalculateChunkSize(items.Count, totalSize);

            return SplitList(items.ToList(), chunkSize);
        }
    }
}
