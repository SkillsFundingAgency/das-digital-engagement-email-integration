DROP VIEW [ASData_PL].[vw_DAS_RegistrationStages]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE VIEW [ASData_PL].[vw_DAS_RegistrationStages]
AS
WITH cte_Paye AS (
    SELECT AccountId, COUNT(DISTINCT PayeRef) AS PayeAdded
    FROM ASData_PL.Acc_AccountHistory
    GROUP BY AccountId
),
cte_Providers AS (
    SELECT AccountId, COUNT(ProviderUkprn) AS Providers
    FROM ASData_PL.PREL_AccountProviders
    GROUP BY AccountId
),
cte_Agreement AS (
    SELECT 
        a.Id AS AccountId,
        ale.SignedAgreementId,
        ea.Acknowledged
    FROM ASData_PL.Acc_Account a
    JOIN ASData_PL.Acc_AccountLegalEntity ale ON ale.AccountId = a.Id
    JOIN ASData_PL.Acc_EmployerAgreement ea ON ea.AccountLegalEntityId = ale.Id
),
-- sonar-ignore-start
cte_Stages AS (
    SELECT
        a.Id AS EmployerAccountId,
        a.Name AS EmployerName,
        u.Email AS UserEmail,

        CASE WHEN u.Email IS NOT NULL THEN 'true' ELSE 'false' END AS Stage1a,
        CASE WHEN aur.Role IS NOT NULL THEN 'true' ELSE 'false' END AS Stage1b,
        CASE WHEN paye.PayeAdded IS NOT NULL THEN 'true' ELSE 'false' END AS Stage2,

        CASE 
            WHEN a.NameConfirmed = 1 
             AND a.ApprenticeshipEmployerType <> 2
             AND a.Name <> 'MY ACCOUNT'
            THEN 'true'
            ELSE 'false'
        END AS Stage3,

        CASE WHEN ag.SignedAgreementId IS NOT NULL THEN 'true' ELSE 'false' END AS Stage4a,
        CASE WHEN ag.Acknowledged = 1 AND ag.SignedAgreementId IS NULL THEN 'true' ELSE 'false' END AS Stage4b,

        CASE WHEN a.AddTrainingProviderAcknowledged = 1 THEN 'true' ELSE 'false' END AS Stage5a,
        CASE 
            WHEN prov.AccountId IS NULL 
             AND a.AddTrainingProviderAcknowledged = 0
            THEN 'true' ELSE 'false'
        END AS Stage5b

    FROM ASData_PL.Acc_User u
    LEFT JOIN ASData_PL.Acc_AccountUserRole aur ON u.Id = aur.UserId
    LEFT JOIN ASData_PL.Acc_Account a ON aur.AccountId = a.Id
    LEFT JOIN cte_Paye paye ON a.Id = paye.AccountId
    LEFT JOIN cte_Providers prov ON a.Id = prov.AccountId
    LEFT JOIN cte_Agreement ag ON a.Id = ag.AccountId
    WHERE a.Name <> 'MY ACCOUNT' Or a.Name IS NULL
)
-- sonar-ignore-end

SELECT
    UserEmail,

    COUNT(DISTINCT EmployerAccountId) AS AccountCount,

    -- Optional: expose account only if unambiguous 
    CASE
        WHEN COUNT(DISTINCT EmployerAccountId) = 1
            THEN MAX(EmployerAccountId)
        ELSE NULL
    END AS EmployerAccountId,

    MAX(EmployerName) AS EmployerName,

    CASE WHEN MIN(Stage1a) = MAX(Stage1a) THEN MIN(Stage1a) ELSE '' END AS Stage1a,
    CASE WHEN MIN(Stage1b) = MAX(Stage1b) THEN MIN(Stage1b) ELSE '' END AS Stage1b,
    CASE WHEN MIN(Stage2)  = MAX(Stage2)  THEN MIN(Stage2)  ELSE '' END AS Stage2,
    CASE WHEN MIN(Stage3)  = MAX(Stage3)  THEN MIN(Stage3)  ELSE '' END AS Stage3,
    CASE WHEN MIN(Stage4a) = MAX(Stage4a) THEN MIN(Stage4a) ELSE '' END AS Stage4a,
    CASE WHEN MIN(Stage4b) = MAX(Stage4b) THEN MIN(Stage4b) ELSE '' END AS Stage4b,
    CASE WHEN MIN(Stage5a) = MAX(Stage5a) THEN MIN(Stage5a) ELSE '' END AS Stage5a,
    CASE WHEN MIN(Stage5b) = MAX(Stage5b) THEN MIN(Stage5b) ELSE '' END AS Stage5b

FROM cte_Stages
GROUP BY
    UserEmail;


