-- =============================================
-- Stored Procedures for CampaignImportMetadataRepository
-- =============================================
-- These stored procedures support the refactored CampaignImportMetadataRepository
-- that uses stored procedures instead of inline SQL for get operations.
-- =============================================

-- =============================================
-- Stored Procedure: Usp_CampaignImportMetadata_Get
-- Description: Retrieves campaigns by their Ids (primary key). If no Ids are provided, returns all campaigns.
-- Parameters: @CampaignIds - Comma-separated list of campaign Ids (BIGINT)
-- Returns: Campaign records matching the provided Ids or all campaigns if no Ids are provided
-- =============================================
CREATE PROCEDURE Usp_CampaignImportMetadata_Get
(
    @CampaignIds    VARCHAR(MAX) = NULL
)
AS
BEGIN

    SELECT  Id, CampaignId, IsImportComplete, ImportStartDate, ImportEndDate
    FROM    dbo.CampaignImportMetadata
    WHERE   (@CampaignIds IS NULL OR CampaignId IN (SELECT value FROM STRING_SPLIT(@CampaignIds, ',') WHERE RTRIM(value) <> ''))

END
GO

-- =============================================
-- Stored Procedure: Usp_CampaignImportMetadata_Upsert
-- Description: Inserts a new campaign or updates an existing campaign based on the provided Id.
-- Parameters: @CampaignId - Id of the campaign to update (BIGINT). If 0 or not provided, a new campaign will be inserted.
-- Returns: The Id of the inserted or updated campaign
-- =============================================
CREATE PROCEDURE Usp_CampaignImportMetadata_Upsert
(
    @CampaignId         BIGINT,
    @IsImportComplete   BIT,
    @ImportStartDate    DATETIME2(7),
    @ImportEndDate      DATETIME2(7) = NULL
)
AS
BEGIN

    MERGE INTO dbo.CampaignImportMetadata AS [Target]
    USING (SELECT @CampaignId AS CampaignId) AS [Source] ON [Target].CampaignId = [Source].CampaignId
    WHEN MATCHED THEN
        UPDATE SET
            IsImportComplete = @IsImportComplete,
            ImportStartDate = @ImportStartDate,
            ImportEndDate = @ImportEndDate
    WHEN NOT MATCHED THEN
        INSERT (CampaignId, IsImportComplete, ImportStartDate, ImportEndDate)
        VALUES (@CampaignId, @IsImportComplete, @ImportStartDate, @ImportEndDate);

END
GO
