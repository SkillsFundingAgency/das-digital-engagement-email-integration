DROP VIEW [ASData_PL].[vw_DAS_EmailIntegration_Provider]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE   VIEW [ASData_PL].[vw_DAS_EmailIntegration_Provider]
AS


WITH ProviderUsers AS (
    SELECT
        u.Email,

        -- FirstName from DisplayName (consistency rule)
        CASE
            WHEN COUNT(DISTINCT u.DisplayName) = 1
                THEN MAX(u.DisplayName)
            ELSE ''
        END AS FirstName,

        '' AS LastName,

        -- UKPRN consistency rule
        CASE
            WHEN COUNT(DISTINCT p.Ukprn) = 1
                THEN MAX(p.Ukprn)
            ELSE NULL
        END AS Ukprn,

        -- ProviderTypeId consistency rule (unique value per email)
        CASE
            WHEN COUNT(DISTINCT p.ProviderTypeId) = 1
                THEN MAX(p.ProviderTypeId)
            ELSE NULL
        END AS ProviderTypeId,

        -- IsEmployer: true if any provider for this email is ProviderTypeId = 2 AND StatusId != 0
        CASE
            WHEN MAX(CASE WHEN p.ProviderTypeId = 2 AND p.StatusId != 0 THEN 1 ELSE 0 END) = 1
                THEN 'true'
            ELSE 'false'
        END AS Employerprovider,

        -- IsProvider: true if email has matching provider registration, false otherwise
        CASE
            WHEN MAX(CASE WHEN p.Ukprn IS NOT NULL THEN 1 ELSE 0 END) = 1
                THEN 'true'
            ELSE 'false'
        END AS IsProvider

    FROM ASData_PL.PAS_User u
    LEFT JOIN ASData_PL.FAT_ROATPV2_ProviderRegistrationDetail p
        ON u.Ukprn = p.Ukprn
    WHERE u.Email IS NOT NULL
    GROUP BY u.Email
)

SELECT
    pu.Email,
    pu.FirstName,
    pu.LastName,
    '' AS EmployerAccountID,
    0 AS AccountCount,
    'Unknown' AS LevyStatus,
    CAST(NULL AS VARCHAR(10)) AS LastLogin,
    CAST(NULL AS VARCHAR(10)) AS DateOfLastAPIAutoSync,
    '' AS ReservedFunding,
    '' AS HasActiveReservation,
    '' AS EmployerSize,
    '' AS SectorEstimate,
    CAST(NULL AS VARCHAR(10)) AS AccountCreationDate,
    '' AS Registrationtype,
    CAST(NULL AS VARCHAR(10)) AS DateOfFirstStart,
    CAST(NULL AS VARCHAR(10)) AS DateOfLastStart,
    CAST(NULL AS VARCHAR(10)) AS DateOfLastCompletion,
    '' AS ActiveApprentices,
    '' AS ActiveVacancies,
    '' AS AccountUserRole,
    '' AS Stage1a,
    '' AS Stage1b,
    '' AS Stage2,
    '' AS Stage3,
    '' AS Stage4a,
    '' AS Stage4b,
    '' AS Stage5a,
    '' AS Stage5b,
    0 AS RegistrationProgressScore,
    '' AS CurrentRegistrationStage,
    '' AS UkEmployerSize,
    '' AS PrimaryIndustry,
    '' AS PrimaryLocation,
    CAST(NULL AS VARCHAR(10)) AS AppsgovSignUpDate,
    '' AS PersonOrigin,
    '' AS IncludeInUR,
    'Provider' AS RecordSource,
    pu.Ukprn,
    CASE
        WHEN pu.ProviderTypeId = 1 THEN 'Main Provider'
        WHEN pu.ProviderTypeId = 2 THEN 'Employer Provider'
        WHEN pu.ProviderTypeId = 3 THEN 'Supporting Provider'
        ELSE ''
    END AS ProviderType,
    pu.Employerprovider,
    pu.IsProvider
FROM ProviderUsers pu
WHERE pu.IsProvider = 'true'
GO