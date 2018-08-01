-- PARAMS:
--DECLARE @ArtifactID int
--DECLARE @ChoiceFieldArtifactGuid uniqueidentifier

DECLARE @ChoiceFieldArtifactID int
DECLARE @ChoiceFieldCodeTypeID int

SELECT @ChoiceFieldArtifactID=[ArtifactID]
  FROM [EDDSDBO].[ArtifactGuid] (NOLOCK)
  where ArtifactGuid=@ChoiceFieldArtifactGuid

SELECT @ChoiceFieldCodeTypeID=[CodeTypeID] FROM Field (NOLOCK)
	WHERE [ArtifactID] = @ChoiceFieldArtifactID

DECLARE @sqlGetChildrenArtifactIDs nvarchar(500)
SET @sqlGetChildrenArtifactIDs =
	N'SELECT [CodeArtifactID] 
	FROM [EDDSDBO].[ZCodeArtifact_' + CONVERT(nvarchar(20), @ChoiceFieldCodeTypeID) + '] (NOLOCK)
	WHERE [AssociatedArtifactID]=' + CONVERT(nvarchar(20), @ArtifactID)

EXECUTE sp_executesql @sqlGetChildrenArtifactIDs