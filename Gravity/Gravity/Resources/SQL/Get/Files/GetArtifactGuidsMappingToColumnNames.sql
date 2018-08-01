SELECT artg.ArtifactGuid, artvfld.[ColumnName]
	FROM [EDDSDBO].[ArtifactViewField] artvfld (NOLOCK)
	INNER JOIN [EDDSDBO].[Field] fld (NOLOCK)
	ON fld.ArtifactViewFieldID = artvfld.ArtifactViewFieldID
	INNER JOIN [EDDSDBO].[ArtifactGuid] artg (NOLOCK)
	ON fld.ArtifactID = artg.ArtifactID
  WHERE artg.ArtifactGuid IN (%%ArtifactGuids%%)