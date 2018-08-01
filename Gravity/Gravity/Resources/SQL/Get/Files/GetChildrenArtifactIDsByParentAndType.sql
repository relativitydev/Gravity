SELECT [ArtifactID]
  FROM [EDDSDBO].[Artifact] (NOLOCK)
  WHERE ParentArtifactID = @ParentID AND ArtifactTypeID = @ArtifactTypeID