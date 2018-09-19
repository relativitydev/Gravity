SELECT artg.[ArtifactGuid], artvfld.[ColumnName]
	FROM [EDDSDBO].[ArtifactViewField] artvfld (NOLOCK)
	JOIN [EDDSDBO].[Field] fld (NOLOCK)
	ON fld.[ArtifactViewFieldID] = artvfld.[ArtifactViewFieldID]
	JOIN [EDDSDBO].[ArtifactGuid] artg (NOLOCK)
	ON fld.ArtifactID = artg.ArtifactID
  WHERE artg.[ArtifactGuid] IN (%%ArtifactGuids%%)