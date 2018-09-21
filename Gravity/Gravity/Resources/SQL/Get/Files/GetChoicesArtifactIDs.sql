-- PARAMS:
--DECLARE @ArtifactID int
--DECLARE @ChoiceFieldArtifactGuid uniqueidentifier

DECLARE @ChoiceFieldArtifactID INT
DECLARE @ChoiceFieldCodeTypeID INT

SELECT @ChoiceFieldArtifactID = [ArtifactID]
  FROM [EDDSDBO].[ArtifactGuid] (NOLOCK)
  WHERE [ArtifactGuid] = @ChoiceFieldArtifactGuid

SELECT @ChoiceFieldCodeTypeID = [CodeTypeID] 
  FROM Field (NOLOCK)
  WHERE [ArtifactID] = @ChoiceFieldArtifactID

DECLARE @sqlGetChildrenArtifactIDs NVARCHAR(500)
SET @sqlGetChildrenArtifactIDs =
	N'SELECT [CodeArtifactID] 
	FROM [EDDSDBO].[ZCodeArtifact_' + CONVERT(NVARCHAR(20), @ChoiceFieldCodeTypeID) + '] (NOLOCK)
	WHERE [AssociatedArtifactID]=' + CONVERT(NVARCHAR(20), @ArtifactID)

EXECUTE sp_executesql @sqlGetChildrenArtifactIDs