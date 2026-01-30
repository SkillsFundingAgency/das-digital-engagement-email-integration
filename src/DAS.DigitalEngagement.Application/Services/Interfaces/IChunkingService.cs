namespace DAS.DigitalEngagement.Application.Services.Interfaces
{
    public interface IChunkingService
    {
        IEnumerable<IList<T>> GetChunks<T>(long totalSize, IList<T> items);
    }
}