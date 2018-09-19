SELECT TOP 1  [FirstName]
      ,[LastName]
  FROM [EDDSDBO].[User] AS instUser (NOLOCK)
  JOIN [EDDSDBO].[UserCaseUser] AS caseUser (NOLOCK)
  ON instUser.[ArtifactID] = caseUser.[UserArtifactID]
  WHERE caseUser.[CaseUserArtifactID] = @CaseUserArtifactId AND caseUser.[CaseArtifactID] = @CaseArtifactId