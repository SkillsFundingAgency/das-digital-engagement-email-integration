using System.Data;

namespace DAS.DigitalEngagement.Application.Services.Interfaces
{
    public interface IJsonToDataTableConverter
    {
        DataSet ConvertODataPageToDataSet(string json);
        DataTable ConvertODataPageToDataTable(string json);
        DataTable JsonToDataTable(string json);

        
    }
}