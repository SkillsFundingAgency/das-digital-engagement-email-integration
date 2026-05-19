DROP VIEW IF EXISTS [ASData_PL].[vw_DAS_EmailIntegration]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- NOSONAR
CREATE VIEW [ASData_PL].[vw_DAS_EmailIntegration]
AS
WITH AccountUsersBase AS (
    SELECT
        au.Email AS EmployerEmail,
        acc.Id AS EmployerAccountID,
        acc.ApprenticeshipEmployerType AS LevyStatus,
        acc.CreatedDate,
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
        r.AccountId,

        -- Has EVER had any reservation (past or present)
        CAST(
            CASE
                WHEN COUNT(*) > 0 THEN 1
                ELSE 0
            END
        AS BIT) AS HasReservations,

        -- Has CURRENTLY ACTIVE reservation
        CAST(
            CASE
                WHEN MAX(
                    CASE
                        WHEN r.ExpiryDate > GETDATE()
                             OR r.ExpiryDate IS NULL
                        THEN 1
                        ELSE 0
                    END
                ) = 1
                THEN 1
                ELSE 0
            END
        AS BIT) AS HasActiveReservation

    FROM ASData_PL.Resv_Reservation r
    WHERE r.IsLevyAccount = 0
    GROUP BY r.AccountId
),
EmailActiveReservationAggregate AS (
    SELECT
        aub.EmployerEmail,
        CAST(
            CASE
                -- No linked accounts  FALSE
                WHEN COUNT(aub.EmployerAccountID) = 0
                    THEN 0

                -- Any active OR missing value  TRUE
                WHEN MAX(COALESCE(rs.HasActiveReservation, 1)) = 1
                    THEN 1

                -- All explicitly inactive
                ELSE 0
            END
        AS BIT) AS HasActiveReservation
    FROM AccountUsersBase aub
    LEFT JOIN ReservationsSummary rs
        ON rs.AccountId = aub.EmployerAccountID
    GROUP BY aub.EmployerEmail
),
EmailReservationsAggregate AS (
    SELECT
        aub.EmployerEmail,
        CASE
            WHEN COUNT(DISTINCT rs.HasReservations) = 1
                THEN CASE
                        WHEN MAX(CAST(rs.HasReservations AS INT)) = 1
                            THEN 'true'
                        ELSE 'false'
                     END
            ELSE 'false'
        END AS HasReservationsText
    FROM AccountUsersBase aub
    LEFT JOIN ReservationsSummary rs
        ON rs.AccountId = aub.EmployerAccountID
    GROUP BY
        aub.EmployerEmail
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
        END AS EmployerSector,
        CASE
            WHEN COUNT(DISTINCT aub.CreatedDate) = 1
                THEN MAX(aub.CreatedDate)
            ELSE NULL
        END AS AccountCreationDate

    FROM AccountUsersBase aub
    LEFT JOIN EmployerSizeCTE es
        ON es.EmployerAccountId = aub.EmployerAccountID
    GROUP BY aub.EmployerEmail
),
EmployerLedAccountCTE AS (
    SELECT
        acc.Id AS EmployerAccountID,
        CASE
            WHEN pr.RequestType IS NOT NULL THEN 'ProviderLed'
            ELSE 'EmployerLed'
        END AS EmployerOrProviderLed
    FROM ASData_PL.Acc_Account acc
    LEFT JOIN ASData_PL.PREL_Requests pr
        ON acc.Name = pr.EmployerOrganisationName
       AND pr.RequestType = 'CreateAccount'
       AND pr.Status = 'Accepted'
),
EmployerLedAggregate AS (
    SELECT
        aub.EmployerEmail,
        CASE
            WHEN COUNT(DISTINCT ela.EmployerOrProviderLed) = 1
                THEN MAX(ela.EmployerOrProviderLed)
            ELSE ''
        END AS EmployerOrProviderLed
    FROM AccountUsersBase aub
    LEFT JOIN EmployerLedAccountCTE ela
        ON ela.EmployerAccountID = aub.EmployerAccountID
    GROUP BY aub.EmployerEmail
),
FoundationApprenticeshipAccountCTE AS (
    SELECT
        e.Id AS EmployerAccountID,
        CAST(
            CASE
                WHEN MAX(
                    CASE
                        -- Foundation apprenticeship started within last 24 months
                        WHEN ss.ApprenticeshipType = 'FoundationApprenticeship'
                             AND a.StartDate >= DATEADD(MONTH, -24, GETDATE())
                        THEN 1
                        ELSE 0
                    END
                ) = 1
                THEN 1
                ELSE 0
            END
        AS BIT) AS HasFoundationAppLast24Months
    FROM ASData_PL.Acc_Account e
    LEFT JOIN ASData_PL.Comt_Commitment c
        ON e.Id = c.EmployerAccountId
    LEFT JOIN ASData_PL.Comt_Apprenticeship a
        ON c.Id = a.CommitmentId
        AND a.IsApproved = 1
        AND a.HasHadDataLockSuccess = 1
        AND a.TrainingType = 0
    LEFT JOIN ASData_PL.FAT2_StandardSector ss
        ON a.TrainingCode = ss.Larscode
    GROUP BY e.Id
),
FoundationApprenticeshipAggregate AS (
    SELECT
        aub.EmployerEmail,
        CAST(
            CASE
                -- No accounts → FALSE
                WHEN COUNT(aub.EmployerAccountID) = 0
                    THEN 0
                -- Any TRUE or missing account data → TRUE
                WHEN MAX(COALESCE(faa.HasFoundationAppLast24Months, 0)) = 1
                    THEN 1
                -- All explicitly FALSE
                ELSE 0
            END
        AS BIT) AS HasFoundationAppLast24Months
    FROM AccountUsersBase aub
    LEFT JOIN FoundationApprenticeshipAccountCTE faa
        ON faa.EmployerAccountID = aub.EmployerAccountID
    GROUP BY aub.EmployerEmail
),
EmployerCommitmentAccountCTE AS (
    SELECT
        e.Id AS EmployerAccountID,
     
        --  StartDate: blank if multiple different values for the same account
        CASE
            WHEN COUNT(DISTINCT a.StartDate) = 1
                THEN MAX(a.StartDate)
            ELSE NULL
        END AS StartDate,

        -- EndDate (same rule applied for consistency)
        CASE
            WHEN COUNT(DISTINCT a.EndDate) = 1
                THEN MAX(a.EndDate)
            ELSE NULL
        END AS EndDate,

        MAX(
        CASE
            WHEN a.StopDate IS NOT NULL
                AND a.EndDate  IS NOT NULL
            THEN IIF(a.StopDate > a.EndDate, a.StopDate, a.EndDate)

            ELSE COALESCE(a.StopDate, a.EndDate)
        END
        ) AS CompletionDate,

        -- Active Apprentices flag (account-level)
        CAST(
            CASE
                WHEN MAX(
                    CASE
                        WHEN l.CompletionStatus = 1 THEN 1
                        ELSE 0
                    END
                ) = 1
                THEN 1
                ELSE 0
            END
        AS BIT) AS HasActiveApprentices

     FROM [ASData_PL].[Acc_Account] e
        LEFT JOIN ASData_PL.Comt_Commitment  c ON e.Id = c.EmployerAccountId
        LEFT JOIN ASData_PL.Comt_Apprenticeship a ON c.Id = a.CommitmentId       
        LEFT JOIN [ASData_PL].[Assessor_Learner] l ON a.Id = l.ApprenticeshipId
    GROUP BY
        e.Id
),
EmployerCommitmentAggregate AS (
    SELECT
        aub.EmployerEmail,

        -- Start Date
        CASE
            WHEN COUNT(DISTINCT eca.StartDate) = 1
                THEN MAX(eca.StartDate)
            ELSE NULL
        END AS ApprenticeshipStartDate,

        -- End Date
        CASE
            WHEN COUNT(DISTINCT eca.EndDate) = 1
                THEN MAX(eca.EndDate)
            ELSE NULL
        END AS ApprenticeshipEndDate,

        -- Completion Date
        CASE
            WHEN COUNT(DISTINCT eca.CompletionDate) = 1
                THEN MAX(eca.CompletionDate)
            ELSE NULL
        END AS ApprenticeshipCompletionDate

    FROM AccountUsersBase aub
    LEFT JOIN EmployerCommitmentAccountCTE eca
        ON eca.EmployerAccountID = aub.EmployerAccountID
    GROUP BY
        aub.EmployerEmail
),
EmailActiveApprenticesAggregate AS (
    SELECT
        aub.EmployerEmail,
        CAST(
            CASE
                -- No accounts
                WHEN COUNT(aub.EmployerAccountID) = 0
                    THEN 0
                -- Any TRUE or missing account data
                WHEN MAX(COALESCE(eca.HasActiveApprentices, 1)) = 1
                    THEN 1
                -- All explicitly FALSE
                ELSE 0
            END
        AS BIT) AS HasActiveApprentices
    FROM AccountUsersBase aub
    LEFT JOIN EmployerCommitmentAccountCTE eca
        ON eca.EmployerAccountID = aub.EmployerAccountID
    GROUP BY aub.EmployerEmail
 ),
 EmployerVacancyAccountCTE AS (
    SELECT
        a.Id AS EmployerAccountID,
        CAST(
            CASE
                WHEN COUNT(v.EmployerId) > 0 THEN 1
                ELSE 0
            END
        AS BIT) AS HasVacancies
    FROM ASData_PL.Acc_Account a
    LEFT JOIN ASData_PL.Va_Employer e
        ON a.HashedId = e.DasAccountId_v2
        AND e.DasAccountId_v2 IS NOT NULL
        AND e.DasAccountId_v2 <> 'N/A'
    LEFT JOIN ASData_PL.Va_Vacancy v
        ON v.EmployerId = e.EmployerId
    GROUP BY
        a.Id
),
EmployerVacancyAggregate AS (
    SELECT
        aub.EmployerEmail,
        CASE
            WHEN COUNT(DISTINCT eva.HasVacancies) = 1
                THEN MAX(CAST(eva.HasVacancies AS INT))
            ELSE NULL
        END AS HasVacancies
    FROM AccountUsersBase aub
    LEFT JOIN EmployerVacancyAccountCTE eva
        ON eva.EmployerAccountID = aub.EmployerAccountID
    GROUP BY
        aub.EmployerEmail
),
EmployerAccountRoleCTE AS (
    SELECT
        e.Id AS EmployerAccountID,
        CASE
            WHEN aur.Role = 1 THEN 'Owner'
            WHEN aur.Role = 2 THEN 'Transactor'
            WHEN aur.Role = 3 THEN 'Viewer'
            ELSE ''
        END AS AccountRole
    FROM ASData_PL.Acc_Account e
    LEFT JOIN ASData_PL.Acc_AccountUserRole aur
        ON aur.AccountId = e.Id
),
EmployerAccountRoleAggregate AS (
    SELECT
        aub.EmployerEmail,
        CASE
            -- No linked accounts blank
            WHEN COUNT(aub.EmployerAccountID) = 0
                THEN ''

            -- All accounts have the same role return it
            WHEN COUNT(DISTINCT ear.AccountRole) = 1
                THEN MAX(ear.AccountRole)

            -- Mixed roles blank
            ELSE ''
        END AS EmployerRole
    FROM AccountUsersBase aub
    LEFT JOIN EmployerAccountRoleCTE ear
        ON ear.EmployerAccountID = aub.EmployerAccountID
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
       CASE
            WHEN ear.HasActiveReservation = 1 THEN 'true'
            WHEN ear.HasActiveReservation = 0 THEN 'false'
            ELSE ''
        END AS HasActiveReservation,
        eaa.EmployerSize,
        eaa.EmployerSector,
        ela.EmployerOrProviderLed,
        CONVERT(VARCHAR(10), eaa.AccountCreationDate, 120) AS AccountCreationDate,
         
        CASE
            WHEN eaaa.HasActiveApprentices = 1 THEN 'true'
            WHEN eaaa.HasActiveApprentices = 0 THEN 'false'
            ELSE ''
        END AS ActiveApprentices,

        CASE
            WHEN eva.HasVacancies = 1 THEN 'true'
            WHEN eva.HasVacancies = 0 THEN 'false'
            ELSE ''
        END AS ActiveVacancies,

        er.EmployerRole AS AccountUserRole,

        CONVERT(VARCHAR(10), ecm.ApprenticeshipStartDate, 120) AS ApprenticeshipStartDate,
        CONVERT(VARCHAR(10), ecm.ApprenticeshipEndDate, 120) AS ApprenticeshipEndDate,
        CONVERT(VARCHAR(10), ecm.ApprenticeshipCompletionDate, 120) AS ApprenticeshipCompletionDate,
        
        CASE
            WHEN faa.HasFoundationAppLast24Months = 1 THEN 'true'
            WHEN faa.HasFoundationAppLast24Months = 0 THEN 'false'
            ELSE ''
        END AS HasFoundationAppLast24Months,

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
    LEFT JOIN EmailActiveReservationAggregate ear
        ON ear.EmployerEmail = aa.EmployerEmail
    LEFT JOIN EmployerAttributesAggregate eaa
        ON eaa.EmployerEmail = aa.EmployerEmail          
    LEFT JOIN EmployerLedAggregate ela
        ON ela.EmployerEmail = aa.EmployerEmail       
    LEFT JOIN EmployerCommitmentAggregate ecm
        ON ecm.EmployerEmail = aa.EmployerEmail
    LEFT JOIN EmailActiveApprenticesAggregate eaaa
        on ecm.EmployerEmail =eaaa.EmployerEmail        
    LEFT JOIN EmployerVacancyAggregate eva
        ON eva.EmployerEmail = aa.EmployerEmail
    LEFT JOIN EmployerAccountRoleAggregate er
        ON er.EmployerEmail = aa.EmployerEmail
    LEFT JOIN FoundationApprenticeshipAggregate faa
        ON faa.EmployerEmail = aa.EmployerEmail
    LEFT JOIN [ASData_PL].[vw_DAS_RegistrationStages] rs
        ON rs.UserEmail = aa.EmployerEmail
        AND aa.EmployerAccountID IS NOT NULL
        AND ISNULL(NULLIF(rs.EmployerAccountId, ''), '') = ISNULL(NULLIF(aa.EmployerAccountID, ''), '')
),
CampaignUsersRanked AS (
    SELECT
        cud.Email,
        cud.FirstName,
        cud.LastName,
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
CampaignUsers AS (
    SELECT
        cur.FirstName AS CampaignFirstName,
        CASE WHEN cur.FirstName = '' THEN '' ELSE cur.LastName END AS CampaignLastName,
        cur.Email,
        cur.UkEmployerSize,
        cur.PrimaryIndustry,
        cur.PrimaryLocation,
        cur.AppsgovSignUpDate,
        cur.PersonOrigin,
        cur.IncludeInUR
    FROM CampaignUsersRanked cur
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
        au.HasActiveReservation,
        au.EmployerSize,
        au.EmployerSector AS SectorEstimate,
        au.EmployerOrProviderLed,
        au.AccountCreationDate,
        
        au.ApprenticeshipStartDate,
        au.ApprenticeshipEndDate,
        au.ApprenticeshipCompletionDate,

        au.ActiveApprentices,
        au.ActiveVacancies,

        au.AccountUserRole,

        au.HasFoundationAppLast24Months,

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
),
-- Combine Employer/Campaign data with Provider data before final output
CombinedData AS (
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
        HasActiveReservation,
        EmployerSize,
        SectorEstimate,
        AccountCreationDate,
        EmployerOrProviderLed AS Registrationtype,
        ApprenticeshipStartDate AS DateOfFirstStart,
        ApprenticeshipEndDate AS DateOfLastStart,
        ApprenticeshipCompletionDate AS DateOfLastCompletion,
        ActiveApprentices,
        ActiveVacancies,
        AccountUserRole,
        HasFoundationAppLast24Months AS Foundationappinlast24months,
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
        RecordSource,
        '' AS Ukprn,
        '' AS ProviderType,
        'false' AS Employerprovider,
        'false' AS IsProvider,
        '' AS Providersactivelinkedapps,
        '' AS Providersactivelinkedvacancies,
        CASE
            WHEN RecordSource IN ('Both', 'Account') THEN 'true'
            ELSE 'false'
        END AS IsEmployer
    FROM Merged

    UNION ALL

    SELECT 
        p.*
    FROM ASData_PL.vw_DAS_EmailIntegration_Provider p
    WHERE p.Email IS NOT NULL
      AND NOT EXISTS (
          SELECT 1
          FROM Merged m
          WHERE m.Email = p.Email)
)
-- Final output with Primary User Type determined from combined data
SELECT
    *,
    -- Primary User Type: Single decision based on ALL combined data
    CASE

        -- Rule 1: If email is in provider table (not employer provider) = provider always
        WHEN IsProvider = 'true' THEN 'Provider'

        -- Rule 2: If email is employer-provider (ProviderTypeId = 2) = always employer provider
        WHEN Employerprovider = 'true' THEN 'Employer Provider'
        
        -- Rule 3: If email is in both levy and non levy, but not provider = always levy
        WHEN LevyStatus = 'Both' THEN 'Levy'
        
        -- Rule 4: If email is levy (not provider)
        WHEN LevyStatus = 'Levy' THEN 'Levy'
        
        -- Rule 5: If email is non levy only (not provider, not employer provider, not in multiple levy types)
        WHEN LevyStatus = 'Non Levy' THEN 'Non Levy'
        
        ELSE ''
    END AS Primaryusertype
FROM CombinedData
GO

