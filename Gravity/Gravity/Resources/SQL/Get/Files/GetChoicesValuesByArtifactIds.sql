SELECT [Name]
  FROM [EDDSDBO].[Code] (NOLOCK)
  WHERE [ArtifactID] IN (%%ArtifactIds%%)