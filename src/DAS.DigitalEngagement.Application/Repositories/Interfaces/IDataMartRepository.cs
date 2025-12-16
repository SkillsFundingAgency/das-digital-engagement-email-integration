using System.Collections.Generic;

namespace DAS.DigitalEngagement.Application.Repositories.Interfaces
{
    public interface IDataMartRepository
    {
        Task<IList<dynamic>> RetrieveEmployeeRegistrationData(string? viewName);
    }
}