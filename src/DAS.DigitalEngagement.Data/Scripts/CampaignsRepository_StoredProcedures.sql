-- =============================================
-- Stored Procedures for CampaignsRepository
-- =============================================
-- These stored procedures support the refactored CampaignsRepository
-- that uses stored procedures instead of inline SQL for get operations.
-- =============================================

-- =============================================
-- Stored Procedure: Usp_Campaigns_Get
-- Description: Retrieves campaigns by their Ids (primary key). If no Ids are provided, returns all campaigns.
-- Parameters: @CampaignIds - Comma-separated list of campaign Ids (BIGINT)
-- Returns: Campaign records matching the provided Ids or all campaigns if no Ids are provided
-- =============================================
CREATE PROCEDURE Usp_Campaigns_Get
(
    @CampaignIds    VARCHAR(MAX) = NULL
)
AS
BEGIN

    SELECT  Id, ExternalId, [Name], [Type], CreatedBy, CreatedOn, ModifiedBy, ModifiedOn,
            FirstSendDate, LastSendDate, FromEmailAddress, FromName, ReplyEmailAddress,
            [Subject], SubStatus, ContactCount, Account
    FROM    dbo.Campaigns WITH (NOLOCK)
    WHERE   (@CampaignIds IS NULL OR Id IN (SELECT id FROM STRING_SPLIT(@CampaignIds, ',') WHERE RTRIM(id) <> ''))

END
GO

-- =============================================
-- Stored Procedure: Usp_Campaign_Upsert
-- Description: Inserts a new campaign or updates an existing campaign based on the provided Id.
-- Parameters: @CampaignId - Id of the campaign to update (BIGINT). If 0 or not provided, a new campaign will be inserted.
-- Returns: The Id of the inserted or updated campaign
-- =============================================
CREATE PROCEDURE Usp_Campaign_Upsert
(
    @CampaignId         BIGINT,
    @ExternalId         INT,
    @Name               VARCHAR(MAX),
    @Type               VARCHAR(255) = NULL,
    @CreatedBy          VARCHAR(255),
    @CreatedOn          DATETIME2(7),
    @ModifiedBy         VARCHAR(255) = NULL,
    @ModifiedOn         DATETIME2(7) = NULL,
    @FirstSendDate      DATETIME2(7),
    @LastSendDate       DATETIME2(7) = NULL,
    @FromEmailAddress   VARCHAR(255),
    @FromName           VARCHAR(255),
    @ReplyEmailAddress  VARCHAR(255),
    @Subject            VARCHAR(MAX),
    @SubStatus          VARCHAR(255),
    @ContactCount       INT,
    @Account            VARCHAR(255)
)
AS
BEGIN

    MERGE INTO dbo.Campaigns AS [Target]
    USING (SELECT @CampaignId AS CampaignId) AS [Source] ON [Target].Id = [Source].CampaignId
    WHEN MATCHED THEN
        UPDATE SET
            [Name] = @Name,
            [Type] = @Type,
            CreatedBy = @CreatedBy,
            CreatedOn = @CreatedOn,
            ModifiedBy = @ModifiedBy,
            ModifiedOn = @ModifiedOn,
            FirstSendDate = @FirstSendDate,
            LastSendDate = @LastSendDate,
            FromEmailAddress = @FromEmailAddress,
            FromName = @FromName,
            ReplyEmailAddress = @ReplyEmailAddress,
            [Subject] = @Subject,
            SubStatus = @SubStatus,
            ContactCount = @ContactCount,
            Account = @Account
    WHEN NOT MATCHED THEN
        INSERT (ExternalId, [Name], [Type], CreatedBy, CreatedOn, ModifiedBy, ModifiedOn, 
                FirstSendDate, LastSendDate, FromEmailAddress, FromName, ReplyEmailAddress, 
                [Subject], SubStatus, ContactCount, Account)
        VALUES (@ExternalId, @Name, @Type, @CreatedBy, @CreatedOn, @ModifiedBy, @ModifiedOn,
                @FirstSendDate, @LastSendDate, @FromEmailAddress, @FromName, @ReplyEmailAddress,
                @Subject, @SubStatus, @ContactCount, @Account);

END
GO
