SELECT [ArtifactID]
  FROM [EDDSDBO].[Artifact] (NOLOCK)
  WHERE [ParentArtifactID] = @ParentID AND [ArtifactTypeID] = @ArtifactTypeID
  ORDER BY [ArtifactID]
  OFFSET %%OffsetRows%% ROWS
  FETCH NEXT @FetchRows ROWS ONLY