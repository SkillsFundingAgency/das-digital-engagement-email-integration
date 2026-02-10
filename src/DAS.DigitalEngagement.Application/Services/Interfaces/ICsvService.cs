namespace DAS.DigitalEngagement.Application.Services.Interfaces
{
    public interface ICsvService
    {
        Stream GenerateStreamFromString(string s);
        int GetByteCount<T>(IList<T> leads);
        string ToCsv<T>(IList<T> leads);
    }
}