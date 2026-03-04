/****** Object:  StoredProcedure [ASData_PL].[Usp_DAS_EmailIntegration]    Script Date: 04/03/2026 22:02:21 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [ASData_PL].[Usp_DAS_EmailIntegration] 
AS
BEGIN
    SET NOCOUNT ON

   SELECT   Email
        ,FirstName
        ,LastName
        ,acc.Id as EmployerAccountID
        ,acc.CreatedDate as CreatedDate
        ,CONVERT(VARCHAR(10), us.LastLogin, 120) AS LastLogin
        ,CONVERT(VARCHAR(10), GETDATE(), 120) AS DateOfLastAPIAutoSync


FROM  [ASData_PL].[Acc_User] as us 
    LEFT JOIN [ASData_PL].[Acc_UserAccountSettings] us_set ON us_set.UserId = us.Id
    LEFT JOIN [ASData_PL].[Acc_Account] acc on acc.Id =us_set.AccountId

END
GO


