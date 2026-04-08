DROP VIEW [ASData_PL].[vw_DAS_EmailIntegration]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE VIEW [ASData_PL].[vw_DAS_EmailIntegration]
AS
/*
Logic of [ASData_PL].[vw_DAS_EmailIntegration]:

- Purpose: Unify Account Users and Campaign Users by email, providing a consolidated view for downstream integration.

1. AccountUsersRanked:
   - Selects all account users, joining user, user-account settings, and account tables.
   - Adds the employer's levy status and account ID.
   - Ranks each user's accounts by last login date (most recent first).

2. ReservationsSummary:
   - For each account, counts distinct reservations and determines reservation status flags/text for non-levy accounts.

3. EmailReservationsAggregate:
   - For each email, aggregates reservation status across all associated accounts.
   - If all accounts for an email have the same reservation status, returns that value; otherwise, returns blank.

4. AccountLevyAggregate:
   - For each email, counts the number of unique accounts (AccountCount).
   - Determines a consolidated levy status:
     - 'Non Levy' if all accounts are non-levy.
     - 'Levy' if all accounts are levy.
     - 'Both' if the email has both levy and non-levy accounts.
     - 'Unknown' otherwise.

5. AccountUsers:
   - Selects the most recent account per email (using the ranking).
   - Joins in the account count, consolidated levy status, and reservation status.

6. CampaignUsersRanked:
   - Selects all campaign users, ranking by the most recent sign-up date per email.

7. CampaignUsers:
   - Selects the most recent campaign user per email.

8. Merged:
   - Full outer joins AccountUsers and CampaignUsers on email.
   - For each email, prefers account user data when available, otherwise uses campaign user data.
   - Sets 'RecordSource' to 'Account', 'Campaign', or 'Both' depending on data presence.
   - If the email is only in CampaignUsers, sets ConsolidatedLevyStatus to 'Unknown'.

9. Final SELECT:
   - Returns unified user data, account info, levy status, reservation status, campaign info, and record source for each unique email.
*/

WITH AccountUsersRanked AS (
    SELECT
        au.Email                        AS EmployerEmail,
        acc.Id                          AS EmployerAccountID,
        acc.ApprenticeshipEmployerType  AS LevyStatus,
        CONVERT(VARCHAR(10), au.LastLogin, 120) AS LastLogin,
        CONVERT(VARCHAR(10), GETDATE(), 120) AS DateOfLastAPIAutoSync,

        -- pick ANY record by Email using a stable column (au.Id)
        ROW_NUMBER() OVER (
            PARTITION BY au.Email
            ORDER BY au.Id
        ) AS rn

    FROM ASData_PL.Acc_User AS au
    INNER JOIN ASData_PL.Acc_UserAccountSettings AS aus
        ON aus.UserId = au.Id
    INNER JOIN ASData_PL.Acc_Account AS acc
        ON acc.Id = aus.AccountId
),

ReservationsSummary AS (
    SELECT 
        [AccountId],
        COUNT(DISTINCT [Id]) AS Reservations,
        CASE WHEN COUNT(DISTINCT [Id]) > 0 THEN 1 ELSE 0 END AS [HasReservationsFlag],
        CASE WHEN COUNT(DISTINCT [Id]) > 0 THEN 'true' ELSE 'false' END AS [HasReservationsText]
    FROM [ASData_PL].[Resv_Reservation]
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
    FROM ASData_PL.Acc_User AS au
    INNER JOIN ASData_PL.Acc_UserAccountSettings AS aus
        ON aus.UserId = au.Id
    INNER JOIN ASData_PL.Acc_Account AS acc
        ON acc.Id = aus.AccountId
    LEFT JOIN ReservationsSummary rs
        ON rs.AccountId = acc.Id
    GROUP BY au.Email
),

-- Ensures FirstName/LastName are only returned when identical for all duplicates
EmailNameAggregate AS (
    SELECT
        au.Email AS EmployerEmail,
        IIF(COUNT(DISTINCT au.FirstName) = 1, MAX(au.FirstName), '') 
            AS EmployerFirstName,

        IIF(COUNT(DISTINCT au.LastName) = 1, MAX(au.LastName), '') 
            AS EmployerLastName

    FROM ASData_PL.Acc_User AS au
    GROUP BY au.Email
),

AccountLevyAggregate AS (
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

EmployerSizeCTE AS (
    -- Normalise Employer Size once per Account
    SELECT 
        de.EmployerAccountId,
        CASE 
            WHEN de.[EmployeeSize1] LIKE '%(Micro)%'  THEN 'Micro'
            WHEN de.[EmployeeSize1] LIKE '%(Small)%'  THEN 'Small'
            WHEN de.[EmployeeSize1] LIKE '%(Medium)%' THEN 'Medium'
            WHEN de.[EmployeeSize1] LIKE '%(Large)%'  THEN 'Large'
            WHEN de.[EmployeeSize1] LIKE '%(Macro)%'  THEN 'Macro'
            ELSE 'Others'
        END AS NormalizedEmployerSize
    FROM ASData_PL.DimEmployer AS de
),

EmployerSizeAggregate AS (
    -- Apply duplicate-handling rule: return blank if inconsistent
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
        IIF(ISNULL(ena.EmployerFirstName, '') = '', '', ena.EmployerLastName) AS EmployerLastName,
        aur.EmployerAccountID,
        aur.LevyStatus,
        aur.LastLogin,
        aur.DateOfLastAPIAutoSync,
        ala.AccountCount,
        ala.ConsolidatedLevyStatus,
        era.HasReservationsText,
        esa.EmployerSize
    FROM AccountUsersRanked aur
    INNER JOIN AccountLevyAggregate ala
        ON ala.EmployerEmail = aur.EmployerEmail
    LEFT JOIN EmailReservationsAggregate era
        ON era.EmployerEmail = aur.EmployerEmail
    LEFT JOIN EmailNameAggregate ena
        ON ena.EmployerEmail = aur.EmployerEmail
    LEFT JOIN EmployerSizeAggregate esa
        ON esa.EmployerEmail = aur.EmployerEmail
    WHERE aur.rn = 1
),

CampaignUsersRanked AS (
    SELECT
        cud.Email                     AS CampaignEmail,
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
    FROM ASData_PL.CPG_UserData AS cud
),

-- Aggregate First/Last Name only if all values match
CampaignNameAggregate AS (
    SELECT
        cud.Email AS CampaignEmail,
        IIF(COUNT(DISTINCT cud.FirstName) = 1, MAX(cud.FirstName), '')
            AS CampaignFirstName,
        IIF(COUNT(DISTINCT cud.LastName) = 1, MAX(cud.LastName), '') 
            AS CampaignLastName
    FROM ASData_PL.CPG_UserData AS cud
    GROUP BY cud.Email
),

CampaignUsers AS (
    SELECT
        -- First name after duplicate-handling
        cna.CampaignFirstName,
        -- Last name only if FirstName is not blank
        IIF(ISNULL(cna.CampaignFirstName, '') = '', '', cna.CampaignLastName)
        AS CampaignLastName,
        cur.CampaignEmail,
        cur.UkEmployerSize,
        cur.PrimaryIndustry,
        cur.PrimaryLocation,
        cur.AppsgovSignUpDate,
        cur.PersonOrigin,
        cur.IncludeInUR
    FROM CampaignUsersRanked cur
    LEFT JOIN CampaignNameAggregate cna
        ON cna.CampaignEmail = cur.CampaignEmail
    WHERE cur.rn = 1
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
        au.HasReservationsText AS ReservedFunding,
        au.EmployerSize,

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
    AccountCount,
    ConsolidatedLevyStatus AS LevyStatus,
    LastLogin,
    DateOfLastAPIAutoSync,
    ReservedFunding,
    EmployerSize,
    UkEmployerSize,
    PrimaryIndustry,
    PrimaryLocation,
    AppsgovSignUpDate,
    PersonOrigin,
    IncludeInUR,
    RecordSource
FROM Merged;

GO