  SELECT [ObjectArtifactID]
      ,[Filename]
      ,[Size]
      ,[Location]
  FROM [EDDSDBO].[File{0}] (NOLOCK)
  WHERE [FileID] = @FileId
