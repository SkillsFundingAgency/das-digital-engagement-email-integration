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
        END AS Ukprn

    FROM ASData_PL.PAS_User u
    LEFT JOIN ASData_PL.FAT_ROATPV2_ProviderRegistrationDetail p
        ON u.Ukprn = p.Ukprn
    WHERE u.Email IS NOT NULL
    GROUP BY u.Email
)

SELECT
    pu.Email,                          -- VARCHAR
    pu.FirstName,                      -- VARCHAR
    pu.LastName,                       -- VARCHAR
    '' AS EmployerAccountID,           -- VARCHAR (empty string to match main view)
    0 AS AccountCount,                 -- INT (changed from NULL to 0)
    'Unknown' AS LevyStatus,           -- VARCHAR
    CAST(NULL AS VARCHAR(10)) AS LastLogin,                -- VARCHAR(10) - explicitly cast
    CAST(NULL AS VARCHAR(10)) AS DateOfLastAPIAutoSync,    -- VARCHAR(10) - explicitly cast
    '' AS ReservedFunding,             -- VARCHAR
    '' AS HasActiveReservation,        -- VARCHAR
    '' AS EmployerSize,                -- VARCHAR
    '' AS SectorEstimate,              -- VARCHAR
    CAST(NULL AS VARCHAR(10)) AS AccountCreationDate,      -- VARCHAR(10) - explicitly cast
    '' AS Registrationtype,            -- VARCHAR
    CAST(NULL AS VARCHAR(10)) AS DateOfFirstStart,         -- VARCHAR(10) - explicitly cast
    CAST(NULL AS VARCHAR(10)) AS DateOfLastStart,          -- VARCHAR(10) - explicitly cast
    CAST(NULL AS VARCHAR(10)) AS DateOfLastCompletion,     -- VARCHAR(10) - explicitly cast
    '' AS ActiveApprentices,           -- VARCHAR
    '' AS ActiveVacancies,             -- VARCHAR
    '' AS AccountUserRole,             -- VARCHAR
    '' AS Stage1a,                     -- VARCHAR/CHAR
    '' AS Stage1b,                     -- VARCHAR/CHAR
    '' AS Stage2,                      -- VARCHAR/CHAR
    '' AS Stage3,                      -- VARCHAR/CHAR
    '' AS Stage4a,                     -- VARCHAR/CHAR
    '' AS Stage4b,                     -- VARCHAR/CHAR
    '' AS Stage5a,                     -- VARCHAR/CHAR
    '' AS Stage5b,                     -- VARCHAR/CHAR
    0 AS RegistrationProgressScore,    -- INT (changed from NULL to 0)
    '' AS CurrentRegistrationStage,    -- VARCHAR
    '' AS UkEmployerSize,              -- VARCHAR
    '' AS PrimaryIndustry,             -- VARCHAR
    '' AS PrimaryLocation,             -- VARCHAR
    CAST(NULL AS VARCHAR(10)) AS AppsgovSignUpDate,        -- VARCHAR(10) - explicitly cast
    '' AS PersonOrigin,                -- VARCHAR
    '' AS IncludeInUR,                 -- VARCHAR
    'Provider' AS RecordSource,        -- VARCHAR
    pu.Ukprn                           -- VARCHAR
FROM ProviderUsers pu
GO
