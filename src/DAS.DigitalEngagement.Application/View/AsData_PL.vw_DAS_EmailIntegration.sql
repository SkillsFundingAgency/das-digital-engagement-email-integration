/****** Object:  View [ASData_PL].[vw_DAS_EmailIntegration]    Script Date: 11/03/2026 19:40:31 ******/
DROP VIEW [ASData_PL].[vw_DAS_EmailIntegration]
GO

/****** Object:  View [ASData_PL].[vw_DAS_EmailIntegration]    Script Date: 11/03/2026 19:40:31 ******/
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
AccountUsers AS (
    SELECT
        EmployerEmail,
        EmployerFirstName,
        EmployerLastName,
        EmployerAccountID,
        LastLogin,
        DateOfLastAPIAutoSync
    FROM AccountUsersRanked
    WHERE rn = 1
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


