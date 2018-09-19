SELECT OT.[DescriptorArtifactTypeID]
  FROM [EDDSDBO].[ArtifactGuid] AS AG (NOLOCK)
  JOIN [EDDSDBO].[ObjectType] AS OT (NOLOCK)
  ON AG.[ArtifactID] = OT.[ArtifactID]
  WHERE AG.[ArtifactGuid] = @ArtifactGuid