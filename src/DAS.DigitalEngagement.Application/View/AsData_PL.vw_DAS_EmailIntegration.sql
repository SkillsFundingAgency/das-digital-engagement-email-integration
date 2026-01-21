-- Object:  View [ASData_PL].[vw_DAS_EmailIntegration]    Script Date: 08/01/2026 13:11:56
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE VIEW [ASData_PL].[vw_DAS_EmailIntegration] 
AS
SELECT   Email
        ,FirstName
        ,LastName
        ,acc.Id as EmployerAccountID
        ,acc.CreatedDate as CreatedDate
        ,us.LastLogin as LastLogin
        ,acc.ApprenticeshipEmployerType as ApprenticeshipEmployerType


FROM  [ASData_PL].[Acc_User] as us 
    LEFT JOIN [ASData_PL].[Acc_UserAccountSettings] us_set ON us_set.UserId = us.Id
    LEFT JOIN [ASData_PL].[Acc_Account] acc on acc.Id =us_set.AccountId
GO