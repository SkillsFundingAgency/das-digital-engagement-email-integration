DROP VIEW IF EXISTS [ASData_PL].[vw_DAS_EmailIntegration];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE VIEW [ASData_PL].[vw_DAS_EmailIntegration]
AS
WITH AccountUsersBase AS (
    SELECT
        au.Email AS EmployerEmail,
        acc.Id AS EmployerAccountID,
        acc.ApprenticeshipEmployerType AS LevyStatus,
        CONVERT(VARCHAR(10), au.LastLogin, 120) AS LastLogin,
        CONVERT(VARCHAR(10), GETDATE(), 120) AS DateOfLastAPIAutoSync
    FROM ASData_PL.Acc_User au
    LEFT JOIN ASData_PL.Acc_UserAccountSettings aus
        ON aus.UserId = au.Id
    LEFT JOIN ASData_PL.Acc_Account acc
        ON acc.Id = aus.AccountId
),
ReservationsSummary AS (
    SELECT
        AccountId,
        CASE WHEN COUNT(DISTINCT Id) > 0 THEN 'true' ELSE 'false' END AS HasReservationsText
    FROM ASData_PL.Resv_Reservation
    WHERE IsLevyAccount = 0
    GROUP BY AccountId
),
EmailReservationsAggregate AS (
    SELECT
        aub.EmployerEmail,
        CASE
            WHEN COUNT(DISTINCT rs.HasReservationsText) = 1
                THEN MAX(rs.HasReservationsText)
            ELSE ''
        END AS HasReservationsText
    FROM AccountUsersBase aub
    LEFT JOIN ReservationsSummary rs
        ON rs.AccountId = aub.EmployerAccountID
    GROUP BY aub.EmployerEmail
),
EmailNameAggregate AS (
    SELECT
        au.Email AS EmployerEmail,
        CASE WHEN COUNT(DISTINCT au.FirstName) = 1 THEN MAX(au.FirstName) ELSE '' END AS EmployerFirstName,
        CASE WHEN COUNT(DISTINCT au.LastName)  = 1 THEN MAX(au.LastName)  ELSE '' END AS EmployerLastName
    FROM ASData_PL.Acc_User au
    GROUP BY au.Email
),
AccountAggregate AS (
    SELECT
        EmployerEmail,
        COUNT(DISTINCT EmployerAccountID) AS AccountCount,
        CASE
            WHEN COUNT(DISTINCT EmployerAccountID) = 1
                THEN CAST(MAX(EmployerAccountID) AS VARCHAR(100))
            ELSE ''
        END AS EmployerAccountID,
        CASE
            WHEN MIN(LevyStatus) = MAX(LevyStatus) AND MIN(LevyStatus) = 0 THEN 'Non Levy'
            WHEN MIN(LevyStatus) = MAX(LevyStatus) AND MIN(LevyStatus) = 1 THEN 'Levy'
            WHEN MIN(LevyStatus) <> MAX(LevyStatus) THEN 'Both'
            ELSE 'Unknown'
        END AS ConsolidatedLevyStatus,
        MAX(LastLogin) AS LastLogin,
        MAX(LevyStatus) AS LevyStatus,
        MAX(DateOfLastAPIAutoSync) AS DateOfLastAPIAutoSync
    FROM AccountUsersBase
    GROUP BY EmployerEmail
),
EmployerSizeCTE AS (
    SELECT
        de.EmployerAccountId,
        CASE
            WHEN de.EmployeeSize1 LIKE '%(Micro)%'  THEN 'Micro'
            WHEN de.EmployeeSize1 LIKE '%(Small)%'  THEN 'Small'
            WHEN de.EmployeeSize1 LIKE '%(Medium)%' THEN 'Medium'
            WHEN de.EmployeeSize1 LIKE '%(Large)%'  THEN 'Large'
            WHEN de.EmployeeSize1 LIKE '%(Macro)%'  THEN 'Macro'
            ELSE 'Others'
        END AS NormalizedEmployerSize,
        de.EmployerSectorEstimate
    FROM ASData_PL.DimEmployer de
),
EmployerAttributesAggregate AS (
    SELECT
        aub.EmployerEmail,
        CASE
            WHEN COUNT(DISTINCT es.NormalizedEmployerSize) = 1
                THEN MAX(es.NormalizedEmployerSize)
            ELSE ''
        END AS EmployerSize,
        CASE
            WHEN COUNT(DISTINCT es.EmployerSectorEstimate) = 1
                THEN MAX(es.EmployerSectorEstimate)
            ELSE ''
        END AS EmployerSector
    FROM AccountUsersBase aub
    LEFT JOIN EmployerSizeCTE es
        ON es.EmployerAccountId = aub.EmployerAccountID
    GROUP BY aub.EmployerEmail
),
AccountUsers AS (
    SELECT
        aa.EmployerEmail,
        ena.EmployerFirstName,
        CASE WHEN ena.EmployerFirstName = '' THEN '' ELSE ena.EmployerLastName END AS EmployerLastName,
        aa.EmployerAccountID,
        aa.LastLogin,
        aa.LevyStatus,
        aa.DateOfLastAPIAutoSync,
        aa.AccountCount,
        aa.ConsolidatedLevyStatus,
        era.HasReservationsText AS ReservedFunding,
        eaa.EmployerSize,
        eaa.EmployerSector,
        rs.Stage1a,
        rs.Stage1b,
        rs.Stage2,
        rs.Stage3,
        rs.Stage4a,
        rs.Stage4b,
        rs.Stage5a,
        rs.Stage5b,

        -- Registration progress score (0–8)
        (
            CASE WHEN rs.Stage1a = 'Y' THEN 1 ELSE 0 END +
            CASE WHEN rs.Stage1b = 'Y' THEN 1 ELSE 0 END +
            CASE WHEN rs.Stage2  = 'Y' THEN 1 ELSE 0 END +
            CASE WHEN rs.Stage3  = 'Y' THEN 1 ELSE 0 END +
            CASE WHEN rs.Stage4a = 'Y' THEN 1 ELSE 0 END +
            CASE WHEN rs.Stage4b = 'Y' THEN 1 ELSE 0 END +
            CASE WHEN rs.Stage5a = 'Y' THEN 1 ELSE 0 END +
            CASE WHEN rs.Stage5b = 'Y' THEN 1 ELSE 0 END
        ) AS RegistrationProgressScore,

        -- Highest completed stage label
        CASE
            WHEN rs.Stage5a = 'Y' THEN 'Stage 5 - Provider added'
            WHEN rs.Stage5b = 'Y' THEN 'Stage 5 - Provider pending'
            WHEN rs.Stage4a = 'Y' THEN 'Stage 4 - Agreement signed'
            WHEN rs.Stage4b = 'Y' THEN 'Stage 4 - Agreement acknowledged'
            WHEN rs.Stage3  = 'Y' THEN 'Stage 3 - Account confirmed'
            WHEN rs.Stage2  = 'Y' THEN 'Stage 2 - PAYE added'
            WHEN rs.Stage1b = 'Y' THEN 'Stage 1 - Role assigned'
            WHEN rs.Stage1a = 'Y' THEN 'Stage 1 - User registered'
            ELSE 'Not started'
        END AS CurrentRegistrationStage
    FROM AccountAggregate aa
    LEFT JOIN EmailNameAggregate ena
        ON ena.EmployerEmail = aa.EmployerEmail
    LEFT JOIN EmailReservationsAggregate era
        ON era.EmployerEmail = aa.EmployerEmail
    LEFT JOIN EmployerAttributesAggregate eaa
        ON eaa.EmployerEmail = aa.EmployerEmail  
    LEFT JOIN [ASData_PL].[vw_DAS_RegistrationStages] rs
        ON rs.UserEmail = aa.EmployerEmail
        AND aa.EmployerAccountID IS NOT NULL
        AND rs.EmployerAccountId = aa.EmployerAccountID
),
CampaignUsersRanked AS (
    SELECT
        cud.Email,
        cud.UkEmployerSize,
        cud.PrimaryIndustry,
        cud.PrimaryLocation,
        CONVERT(VARCHAR(10), cud.AppsgovSignUpDate, 120) AS AppsgovSignUpDate,
        cud.PersonOrigin,
        cud.IncludeInUR,
        ROW_NUMBER() OVER (
            PARTITION BY cud.Email
            ORDER BY cud.AppsgovSignUpDate DESC
        ) AS rn
    FROM ASData_PL.CPG_UserData cud
),
CampaignNameAggregate AS (
    SELECT
        Email,
        CASE WHEN COUNT(DISTINCT FirstName) = 1 THEN MAX(FirstName) ELSE '' END AS CampaignFirstName,
        CASE WHEN COUNT(DISTINCT LastName)  = 1 THEN MAX(LastName)  ELSE '' END AS CampaignLastName
    FROM ASData_PL.CPG_UserData
    GROUP BY Email
),
CampaignUsers AS (
    SELECT
        cna.CampaignFirstName,
        CASE WHEN cna.CampaignFirstName = '' THEN '' ELSE cna.CampaignLastName END AS CampaignLastName,
        cur.Email,
        cur.UkEmployerSize,
        cur.PrimaryIndustry,
        cur.PrimaryLocation,
        cur.AppsgovSignUpDate,
        cur.PersonOrigin,
        cur.IncludeInUR
    FROM CampaignUsersRanked cur
    LEFT JOIN CampaignNameAggregate cna
        ON cna.Email = cur.Email
    WHERE cur.rn = 1
),
Merged AS (
    SELECT
        COALESCE(au.EmployerEmail, cu.Email) AS Email,
        COALESCE(au.EmployerFirstName, cu.CampaignFirstName) AS FirstName,
        COALESCE(au.EmployerLastName,  cu.CampaignLastName)  AS LastName,

        au.EmployerAccountID,
        au.AccountCount,
        CASE
            WHEN au.EmployerEmail IS NULL THEN 'Unknown'
            ELSE au.ConsolidatedLevyStatus
        END AS ConsolidatedLevyStatus,

        au.LastLogin,
        au.DateOfLastAPIAutoSync,
        au.ReservedFunding,
        au.EmployerSize,
        au.EmployerSector AS SectorEstimate,

        -- Registration
        au.Stage1a,
        au.Stage1b,
        au.Stage2,
        au.Stage3,
        au.Stage4a,
        au.Stage4b,
        au.Stage5a,
        au.Stage5b,
        au.RegistrationProgressScore,
        au.CurrentRegistrationStage,

        -- Campaign
        cu.UkEmployerSize,
        cu.PrimaryIndustry,
        cu.PrimaryLocation,
        cu.AppsgovSignUpDate,
        cu.PersonOrigin,
        cu.IncludeInUR,

        CASE
            WHEN au.EmployerEmail IS NOT NULL AND cu.Email IS NOT NULL THEN 'Both'
            WHEN au.EmployerEmail IS NOT NULL THEN 'Account'
            ELSE 'Campaign'
        END AS RecordSource
    FROM AccountUsers au
    FULL OUTER JOIN CampaignUsers cu
        ON au.EmployerEmail = cu.Email
)
SELECT
    Email,
    FirstName,
    LastName,
    EmployerAccountID,
    AccountCount,
    ConsolidatedLevyStatus AS LevyStatus,
    LastLogin,
    DateOfLastAPIAutoSync,
    ReservedFunding,
    EmployerSize,
    SectorEstimate,
    Stage1a,
    Stage1b,
    Stage2,
    Stage3,
    Stage4a,
    Stage4b,
    Stage5a,
    Stage5b,
    RegistrationProgressScore,
    CurrentRegistrationStage,

    UkEmployerSize,
    PrimaryIndustry,
    PrimaryLocation,
    AppsgovSignUpDate,
    PersonOrigin,
    IncludeInUR,
    RecordSource
FROM Merged;
GO