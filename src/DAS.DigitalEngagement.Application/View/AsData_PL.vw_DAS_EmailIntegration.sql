DROP VIEW IF EXISTS [ASData_PL].[vw_DAS_EmailIntegration_Reg_Stages];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE VIEW [ASData_PL].[vw_DAS_EmailIntegration_Reg_Stages]
AS

WITH AccountUsersRanked AS (
    SELECT
        au.Email                        AS EmployerEmail,
        acc.Id                          AS EmployerAccountID,
        acc.ApprenticeshipEmployerType  AS LevyStatus,
        CONVERT(VARCHAR(10), au.LastLogin, 120) AS LastLogin,
        CONVERT(VARCHAR(10), GETDATE(), 120) AS DateOfLastAPIAutoSync,

        ROW_NUMBER() OVER (
            PARTITION BY au.Email
            ORDER BY au.Id
        ) AS rn

    FROM ASData_PL.Acc_User au
    LEFT JOIN ASData_PL.Acc_UserAccountSettings aus
        ON aus.UserId = au.Id
    LEFT JOIN ASData_PL.Acc_Account acc
        ON acc.Id = aus.AccountId
),

ReservationsSummary AS (
    SELECT 
        AccountId,
        COUNT(DISTINCT Id) AS Reservations,
        CASE WHEN COUNT(DISTINCT Id) > 0 THEN 'true' ELSE 'false' END AS HasReservationsText
    FROM ASData_PL.Resv_Reservation
    WHERE IsLevyAccount = 0
    GROUP BY AccountId
),

EmailReservationsAggregate AS (
    SELECT
        au.Email AS EmployerEmail,
        CASE 
            WHEN COUNT(DISTINCT rs.HasReservationsText) = 1 
                THEN MAX(rs.HasReservationsText)
            ELSE '' 
        END AS HasReservationsText
    FROM ASData_PL.Acc_User au
    INNER JOIN ASData_PL.Acc_UserAccountSettings aus
        ON aus.UserId = au.Id
    INNER JOIN ASData_PL.Acc_Account acc
        ON acc.Id = aus.AccountId
    LEFT JOIN ReservationsSummary rs
        ON rs.AccountId = acc.Id
    GROUP BY au.Email
),

EmailNameAggregate AS (
    SELECT
        au.Email AS EmployerEmail,
        CASE WHEN COUNT(DISTINCT au.FirstName) = 1 THEN MAX(au.FirstName) ELSE '' END AS EmployerFirstName,
        CASE WHEN COUNT(DISTINCT au.LastName)  = 1 THEN MAX(au.LastName)  ELSE '' END AS EmployerLastName
    FROM ASData_PL.Acc_User au
    GROUP BY au.Email
),

AccountLevyAggregate AS (
    SELECT
        EmployerEmail,
        COUNT(DISTINCT EmployerAccountID) AS AccountCount,
        CASE
            WHEN MIN(LevyStatus) = MAX(LevyStatus) AND MIN(LevyStatus) = 0 THEN 'Non Levy'
            WHEN MIN(LevyStatus) = MAX(LevyStatus) AND MIN(LevyStatus) = 1 THEN 'Levy'
            WHEN MIN(LevyStatus) <> MAX(LevyStatus) THEN 'Both'
            ELSE 'Unknown'
        END AS ConsolidatedLevyStatus
    FROM AccountUsersRanked
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
        END AS NormalizedEmployerSize
    FROM ASData_PL.DimEmployer de
),

EmployerSizeAggregate AS (
    SELECT
        au.Email AS EmployerEmail,
        CASE 
            WHEN COUNT(DISTINCT es.NormalizedEmployerSize) = 1
                THEN MAX(es.NormalizedEmployerSize)
            ELSE ''
        END AS EmployerSize
    FROM ASData_PL.Acc_User au
    LEFT JOIN ASData_PL.Acc_UserAccountSettings aus
        ON aus.UserId = au.Id
    LEFT JOIN ASData_PL.Acc_Account acc
        ON acc.Id = aus.AccountId
    LEFT JOIN EmployerSizeCTE es
        ON es.EmployerAccountId = acc.Id
    GROUP BY au.Email
),

AccountUsers AS (
    SELECT
        aur.EmployerEmail,
        ena.EmployerFirstName,
        CASE WHEN ena.EmployerFirstName = '' THEN '' ELSE ena.EmployerLastName END AS EmployerLastName,
        aur.EmployerAccountID,
        aur.LevyStatus,
        aur.LastLogin,
        aur.DateOfLastAPIAutoSync,
        ala.AccountCount,
        ala.ConsolidatedLevyStatus,
        era.HasReservationsText AS ReservedFunding,
        esa.EmployerSize,

        rs.Stage1a, rs.Stage1b, rs.Stage2, rs.Stage3,
        rs.Stage4a, rs.Stage4b, rs.Stage5a, rs.Stage5b,

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

        --  Highest completed stage label
        CASE
            WHEN rs.Stage5a = 'Y' THEN 'Stage 5 – Provider added'
            WHEN rs.Stage5b = 'Y' THEN 'Stage 5 – Provider pending'
            WHEN rs.Stage4a = 'Y' THEN 'Stage 4 – Agreement signed'
            WHEN rs.Stage4b = 'Y' THEN 'Stage 4 – Agreement acknowledged'
            WHEN rs.Stage3  = 'Y' THEN 'Stage 3 – Account confirmed'
            WHEN rs.Stage2  = 'Y' THEN 'Stage 2 – PAYE added'
            WHEN rs.Stage1b = 'Y' THEN 'Stage 1 – Role assigned'
            WHEN rs.Stage1a = 'Y' THEN 'Stage 1 – User registered'
            ELSE 'Not started'
        END AS CurrentRegistrationStage

    FROM AccountUsersRanked aur
    INNER JOIN AccountLevyAggregate ala
        ON ala.EmployerEmail = aur.EmployerEmail
    LEFT JOIN EmailNameAggregate ena
        ON ena.EmployerEmail = aur.EmployerEmail
    LEFT JOIN EmailReservationsAggregate era
        ON era.EmployerEmail = aur.EmployerEmail
    LEFT JOIN EmployerSizeAggregate esa
        ON esa.EmployerEmail = aur.EmployerEmail
    LEFT JOIN [ASData_PL].[vw_DAS_RegistrationStages] rs
        ON rs.UserEmail = aur.EmployerEmail
       AND ISNULL(rs.EmployerAccountId, -1) = ISNULL(aur.EmployerAccountID, -1)
    WHERE aur.rn = 1
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
        -- Registration
        au.Stage1a, au.Stage1b, au.Stage2, au.Stage3,
        au.Stage4a, au.Stage4b, au.Stage5a, au.Stage5b,
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

    Stage1a, Stage1b, Stage2, Stage3,
    Stage4a, Stage4b, Stage5a, Stage5b,
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