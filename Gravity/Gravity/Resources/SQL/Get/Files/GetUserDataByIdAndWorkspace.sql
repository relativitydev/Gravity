SELECT TOP 1  [FirstName]
      ,[LastName]
  FROM [EDDSDBO].[User] as instUser (NOLOCK)
  INNER JOIN [EDDSDBO].[UserCaseUser] as caseUser (NOLOCK)
  ON instUser.ArtifactID = caseUser.UserArtifactID
  WHERE caseUser.CaseUserArtifactID = @CaseUserArtifactId and caseUser.CaseArtifactID = @CaseArtifactId