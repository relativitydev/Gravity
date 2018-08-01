SELECT OT.[DescriptorArtifactTypeID]
  FROM [EDDSDBO].[ArtifactGuid] as AG (NOLOCK)
  INNER JOIN [EDDSDBO].[ObjectType] as OT (NOLOCK)
  ON AG.ArtifactID = OT.ArtifactID 
  WHERE AG.ArtifactGuid = @ArtifactGuid