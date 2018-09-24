SELECT [ArtifactID], [ArtifactGuid]
  FROM [EDDSDBO].[ArtifactGuid] (NOLOCK)
  WHERE [ArtifactID] IN (%%ArtifactIDs%%)