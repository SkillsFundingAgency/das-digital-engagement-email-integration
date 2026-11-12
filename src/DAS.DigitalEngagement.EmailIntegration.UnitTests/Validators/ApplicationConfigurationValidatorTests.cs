using DAS.DigitalEngagement.EmailIntegration.Validators;
using DAS.DigitalEngagement.Models.Infrastructure;
using Microsoft.Extensions.Options;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Validators;

[TestFixture]
public class ApplicationConfigurationValidatorTests
{
    [Test]
    public void Validate_WhenOptionsAreNull_Fails()
    {
        var result = Validate(null);

        Assert.That(result.Failures, Is.EqualTo(new[]
        {
            "ApplicationConfiguration: section is missing or could not be bound."
        }));
    }

    [Test]
    public void Validate_WhenDataMartIsMissingOrEmpty_Fails()
    {
        var missingConfiguration = CreateValidConfiguration();
        missingConfiguration.DataMart = null!;
        var emptyConfiguration = CreateValidConfiguration();
        emptyConfiguration.DataMart = [];

        var missingResult = Validate(missingConfiguration);
        var emptyResult = Validate(emptyConfiguration);

        Assert.Multiple(() =>
        {
            Assert.That(missingResult.Failures, Has.Some.EqualTo(
                "DataMart: section is missing or contains no entries."));
            Assert.That(emptyResult.Failures, Has.Some.EqualTo(
                "DataMart: section is missing or contains no entries."));
        });
    }

    [Test]
    public void Validate_WhenDataMartEntryIsNull_Fails()
    {
        var configuration = CreateValidConfiguration();
        configuration.DataMart = [null!];

        var result = Validate(configuration);

        Assert.That(result.Failures, Has.Some.EqualTo("DataMart[0]: entry is null."));
    }

    [Test]
    public void Validate_WhenDataMartEntryHasMissingValues_Fails()
    {
        var configuration = CreateValidConfiguration();
        configuration.DataMart =
        [
            new DataMartSettings
            {
                ViewName = " ",
                ObjectName = null!,
                FieldMapping = "",
                TemplatedUploadId = []
            }
        ];

        var result = Validate(configuration);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failures, Has.Some.EqualTo(
                "DataMart[0].ViewName: required and cannot be empty."));
            Assert.That(result.Failures, Has.Some.EqualTo(
                "DataMart[0].ObjectName: required and cannot be empty."));
            Assert.That(result.Failures, Has.Some.EqualTo(
                "DataMart[0].FieldMapping: required and cannot be empty."));
            Assert.That(result.Failures, Has.Some.EqualTo(
                "DataMart[0].TemplatedUploadId: required and cannot be empty."));
        });
    }

    [Test]
    public void Validate_WhenDataMartUploadIdsContainNonPositiveValue_Fails()
    {
        var configuration = CreateValidConfiguration();
        configuration.DataMart[0].TemplatedUploadId = [1, 0, -1];

        var result = Validate(configuration);

        Assert.That(result.Failures, Has.Some.EqualTo(
            "DataMart[0].TemplatedUploadId: all IDs must be greater than zero."));
    }

    [Test]
    public void Validate_WhenConnectionStringIsMissingOrDatabaseIsBlank_Fails()
    {
        var missingConfiguration = CreateValidConfiguration();
        missingConfiguration.ConnectionString = null!;
        var blankConfiguration = CreateValidConfiguration();
        blankConfiguration.ConnectionString.DataMart = " ";

        var missingResult = Validate(missingConfiguration);
        var blankResult = Validate(blankConfiguration);

        Assert.Multiple(() =>
        {
            Assert.That(missingResult.Failures, Has.Some.EqualTo(
                "ConnectionString: section is missing."));
            Assert.That(blankResult.Failures, Has.Some.EqualTo(
                "ConnectionString.Database: required and cannot be empty."));
        });
    }

    [Test]
    public void Validate_WhenEmailMarketingApiIsMissing_Fails()
    {
        var configuration = CreateValidConfiguration();
        configuration.EmailMarketingApi = null!;

        var result = Validate(configuration);

        Assert.That(result.Failures, Has.Some.EqualTo("EmailMarketingApi: section is missing."));
    }

    [Test]
    public void Validate_WhenEmailMarketingApiStringsAreBlank_Fails()
    {
        var configuration = CreateValidConfiguration();
        configuration.EmailMarketingApi.ApiKey = "";
        configuration.EmailMarketingApi.ApiBaseUrl = null!;

        var result = Validate(configuration);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failures, Has.Some.EqualTo(
                "EmailMarketingApi.ApiKey: required and cannot be empty."));
            Assert.That(result.Failures, Has.Some.EqualTo(
                "EmailMarketingApi.ApiBaseUrl: required and cannot be empty."));
        });
    }

    [Test]
    public void Validate_WhenEmailMarketingApiNumericValuesAreInvalid_Fails()
    {
        var configuration = CreateValidConfiguration();
        configuration.EmailMarketingApi.ApiRetryCount = 0;
        configuration.EmailMarketingApi.ChunkSizeKB = 0;
        configuration.EmailMarketingApi.PageSize = 0;
        configuration.EmailMarketingApi.ApiFilterBatchSize = -1;
        configuration.EmailMarketingApi.SendContactFilterBatchSize = 0;
        configuration.EmailMarketingApi.SqlWriteBatchSize = -1;

        var result = Validate(configuration);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failures, Has.Some.EqualTo(
                "EmailMarketingApi.ApiRetryCount: required and cannot be Zero."));
            Assert.That(result.Failures, Has.Some.EqualTo(
                "EmailMarketingApi.ChunkSizeKB: required and cannot be Zero."));
            Assert.That(result.Failures, Has.Some.EqualTo(
                "EmailMarketingApi.PageSize: required and must be greater than zero."));
            Assert.That(result.Failures, Has.Some.EqualTo(
                "EmailMarketingApi.ApiFilterBatchSize: required and must be greater than zero."));
            Assert.That(result.Failures, Has.Some.EqualTo(
                "EmailMarketingApi.SendContactFilterBatchSize: required and must be greater than zero."));
            Assert.That(result.Failures, Has.Some.EqualTo(
                "EmailMarketingApi.SqlWriteBatchSize: required and must be greater than zero."));
        });
    }

    [Test]
    public void Validate_WhenLookbackIsNotConfigured_Succeeds()
    {
        var result = Validate(CreateValidConfiguration());

        Assert.That(result.Succeeded, Is.True);
    }

    [Test]
    public void Validate_WhenLookbackIsPositiveAndAtLeastImportWindow_Succeeds()
    {
        var configuration = CreateValidConfiguration();
        configuration.EmailMarketingApi.SendRetrievalLookbackDays = 30;

        var result = Validate(configuration);

        Assert.That(result.Succeeded, Is.True);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Validate_WhenLookbackIsNotPositive_Fails(int lookbackDays)
    {
        var configuration = CreateValidConfiguration();
        configuration.EmailMarketingApi.SendRetrievalLookbackDays = lookbackDays;

        var result = Validate(configuration);

        Assert.That(result.Failed, Is.True);
        Assert.That(result.Failures, Has.Some.EqualTo(
            "EmailMarketingApi.SendRetrievalLookbackDays: when configured, value must be greater than zero."));
    }

    [Test]
    public void Validate_WhenLookbackIsShorterThanImportWindow_Fails()
    {
        var configuration = CreateValidConfiguration();
        configuration.EmailMarketingApi.SendRetrievalLookbackDays = 6;

        var result = Validate(configuration);

        Assert.That(result.Failed, Is.True);
        Assert.That(result.Failures, Has.Some.EqualTo(
            "EmailMarketingApi.SendRetrievalLookbackDays: must be greater than or equal to ImportWindowDays."));
    }

    [Test]
    public void Validate_WhenSubAccountIdsContainNonPositiveValue_Fails()
    {
        var configuration = CreateValidConfiguration();
        configuration.EmailMarketingApi.SubAccountIds = [1, 0, -1];

        var result = Validate(configuration);

        Assert.That(result.Failures, Has.Some.EqualTo(
            "SubAccountIds: all IDs must be greater than zero."));
    }

    private static ValidateOptionsResult Validate(ApplicationConfiguration? configuration) =>
        new ApplicationConfigurationValidator().Validate(Options.DefaultName, configuration);

    private static ApplicationConfiguration CreateValidConfiguration() =>
        new()
        {
            ConnectionString = new ConnectionString
            {
                DataMart = "Server=.;Database=test",
                CampaignsDatabase = "Server=.;Database=campaigns"
            },
            EmailMarketingApi = new EmailMarketingApi
            {
                ApiBaseUrl = "https://api.test.com",
                ApiKey = "test-key",
                ApiRetryCount = 3,
                ChunkSizeKB = 1024,
                PageSize = 2,
                ApiFilterBatchSize = 2,
                SendContactFilterBatchSize = 2,
                SqlWriteBatchSize = 2,
                ImportWindowDays = 7
            },
            DataMart =
            [
                new DataMartSettings
                {
                    ViewName = "view",
                    ObjectName = "object",
                    FieldMapping = "mapping",
                    TemplatedUploadId = [1]
                }
            ]
        };
}
