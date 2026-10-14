namespace DAS.DigitalEngagement.CampaignInterest.Data.Repositories.Interfaces;

public interface IDataMartRepository
{
    Task<IList<dynamic>> RetrieveEmployeeRegistrationData();
}
