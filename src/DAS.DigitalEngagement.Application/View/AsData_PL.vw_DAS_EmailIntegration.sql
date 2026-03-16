/****** Object:  View [ASData_PL].[vw_DAS_EmailIntegration]    Script Date: 16/03/2026 23:37:36 ******/
DROP VIEW [ASData_PL].[vw_DAS_EmailIntegration]
GO

/****** Object:  View [ASData_PL].[vw_DAS_EmailIntegration]    Script Date: 16/03/2026 23:37:36 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE VIEW [ASData_PL].[vw_DAS_EmailIntegration]
AS
/*
Purpose:
- Unify Account Users and Campaign Users by email.
- Keep only the most recent record per email from each source.
- Prefer Account values over Campaign values when both exist.
- RecordSource flag: 'Account', 'Campaign', or 'Both'. (Debug only)
*/
WITH AccountUsersRanked AS (
    SELECT
        au.Email                        AS EmployerEmail,
        au.FirstName                    AS EmployerFirstName,
        au.LastName                     AS EmployerLastName,
        acc.Id                          AS EmployerAccountID,
        acc.ApprenticeshipEmployerType  AS LevyStatus,
        CONVERT(VARCHAR(10), au.LastLogin, 120) AS LastLogin,
        CONVERT(VARCHAR(10), GETDATE(), 120) AS DateOfLastAPIAutoSync,
        ROW_NUMBER() OVER (
            PARTITION BY au.Email
            ORDER BY acc.CreatedDate DESC
        ) AS rn
    FROM ASData_PL.Acc_User AS au
    INNER JOIN ASData_PL.Acc_UserAccountSettings AS aus
        ON aus.UserId = au.Id
    INNER JOIN ASData_PL.Acc_Account AS acc
        ON acc.Id = aus.AccountId
),
AccountAggregate AS (
    SELECT
        EmployerEmail,
        COUNT(DISTINCT EmployerAccountID) AS AccountCount,
        CASE
            WHEN MIN(LevyStatus) = 0 AND MAX(LevyStatus) = 0 THEN 'Non Levy'
            WHEN MIN(LevyStatus) = 1 AND MAX(LevyStatus) = 1 THEN 'Levy'
            WHEN MIN(LevyStatus) IN (0,1) AND MAX(LevyStatus) IN (0,1)
                 AND MIN(LevyStatus) <> MAX(LevyStatus)
                THEN 'Both'
            ELSE 'Unknown'
        END AS ConsolidatedLevyStatus
    FROM AccountUsersRanked
    GROUP BY EmployerEmail
),
AccountUsers AS (
    SELECT
        aur.EmployerEmail,
        aur.EmployerFirstName,
        aur.EmployerLastName,
        aur.EmployerAccountID,
        aur.LevyStatus,
        aur.LastLogin,
        aur.DateOfLastAPIAutoSync,
        aa.AccountCount,
        aa.ConsolidatedLevyStatus
    FROM AccountUsersRanked aur
    INNER JOIN AccountAggregate aa
        ON aa.EmployerEmail = aur.EmployerEmail
    WHERE aur.rn = 1
),
CampaignUsersRanked AS (
    SELECT
        cud.FirstName                 AS CampaignFirstName,
        cud.LastName                  AS CampaignLastName,
        cud.Email                     AS CampaignEmail,
        cud.UkEmployerSize,
        cud.PrimaryIndustry,
        cud.PrimaryLocation,
        cud.AppsgovSignUpDate,
        cud.PersonOrigin,
        cud.IncludeInUR,
        ROW_NUMBER() OVER (
            PARTITION BY cud.Email
            ORDER BY cud.AppsgovSignUpDate DESC
        ) AS rn
    FROM ASData_PL.CPG_UserData AS cud
),
CampaignUsers AS (
    SELECT
        CampaignFirstName,
        CampaignLastName,
        CampaignEmail,
        UkEmployerSize,
        PrimaryIndustry,
        PrimaryLocation,
        AppsgovSignUpDate,
        PersonOrigin,
        IncludeInUR
    FROM CampaignUsersRanked
    WHERE rn = 1
),
Merged AS (
    SELECT
        COALESCE(au.EmployerEmail, cu.CampaignEmail)        AS Email,
        COALESCE(au.EmployerFirstName, cu.CampaignFirstName) AS FirstName,
        COALESCE(au.EmployerLastName,  cu.CampaignLastName)  AS LastName,

        au.EmployerAccountID,
        au.LevyStatus,
        au.AccountCount,
        CASE 
            WHEN au.EmployerEmail IS NULL THEN 'Unknown'
            ELSE au.ConsolidatedLevyStatus
        END AS ConsolidatedLevyStatus,
        au.LastLogin,
        au.DateOfLastAPIAutoSync,

        cu.UkEmployerSize,
        cu.PrimaryIndustry,
        cu.PrimaryLocation,
        cu.AppsgovSignUpDate,
        cu.PersonOrigin,
        cu.IncludeInUR,
        -- (Debug only)
        CASE 
            WHEN au.EmployerEmail IS NOT NULL AND cu.CampaignEmail IS NOT NULL THEN 'Both'
            WHEN au.EmployerEmail IS NOT NULL THEN 'Account'
            ELSE 'Campaign'
        END AS RecordSource
    FROM AccountUsers AS au
    FULL OUTER JOIN CampaignUsers AS cu
        ON au.EmployerEmail = cu.CampaignEmail
)
SELECT
    Email,
    FirstName,
    LastName,
    EmployerAccountID,
    LevyStatus,
    AccountCount,
    ConsolidatedLevyStatus,
    LastLogin,
    DateOfLastAPIAutoSync,
    UkEmployerSize,
    PrimaryIndustry,
    PrimaryLocation,
    AppsgovSignUpDate,
    PersonOrigin,
    IncludeInUR,
    RecordSource
FROM Merged;

GO

