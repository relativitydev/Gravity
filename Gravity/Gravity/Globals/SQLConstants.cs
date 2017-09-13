namespace Gravity.Globals
{
	public static class SQLConstants
	{
		#region SqlGetConstants

		public const string sqlGetArtifactGuidsMappingToColumnNames = @"
SELECT artg.ArtifactGuid, art.TextIdentifier
  FROM [EDDSDBO].[Artifact] art (NOLOCK)
  JOIN [EDDSDBO].[ArtifactGuid] artg (NOLOCK) on art.ArtifactID = artg.ArtifactID
  WHERE artg.ArtifactGuid IN (
";

		public const string sqlGetArtifactGuidMappings = @"
SELECT [ArtifactID], [ArtifactGuid]
  FROM [EDDSDBO].[ArtifactGuid] (NOLOCK)
  WHERE [ArtifactID] IN (
";

		public const string sqlGetMultipleObjectArtifactIDs = @"
-- PARAMS:
--DECLARE @ArtifactID int
--DECLARE @MultipleObjectFieldArtifactGuid uniqueidentifier

DECLARE @MultipleObjectFieldArtifactID int
DECLARE @MultipleObjectFieldAssociativeArtifactID int

SELECT @MultipleObjectFieldArtifactID=[ArtifactID]
  FROM [EDDSDBO].[ArtifactGuid] (NOLOCK)
  where ArtifactGuid=@MultipleObjectFieldArtifactGuid

SELECT @MultipleObjectFieldAssociativeArtifactID=[ArtifactID] FROM Field (NOLOCK)
	WHERE DisplayName = (SELECT DisplayName FROM Field (NOLOCK) WHERE ArtifactID=@MultipleObjectFieldArtifactID)
		AND [ArtifactID] <> @MultipleObjectFieldArtifactID

DECLARE @sqlGetChildrenArtifactIDs nvarchar(500)
SET @sqlGetChildrenArtifactIDs =
	N'SELECT [f' + CONVERT(nvarchar(20), @MultipleObjectFieldAssociativeArtifactID)  + 'ArtifactID] 
	FROM [EDDSDBO].[f' + CONVERT(nvarchar(20), @MultipleObjectFieldAssociativeArtifactID) + 'f' + CONVERT(nvarchar(20), @MultipleObjectFieldArtifactID) + '] (NOLOCK)
	WHERE [f' + CONVERT(nvarchar(20), @MultipleObjectFieldArtifactID)  + 'ArtifactID]=' + CONVERT(nvarchar(20), @ArtifactID)

EXECUTE sp_executesql @sqlGetChildrenArtifactIDs
";

		public const string sqlGetSingleObjectArtifactIDFormat = @"
SELECT [{0}]
  FROM [EDDSDBO].[{1}] (NOLOCK)
  WHERE ArtifactID=@ArtifactID
";

		public const string sqlGetChoicesArtifactIDs = @"
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

print @sqlGetChildrenArtifactIDs

EXECUTE sp_executesql @sqlGetChildrenArtifactIDs
";

		public const string sqlGetChildrenArtifactIDs = @"
SELECT TOP 1000 [ArtifactID]
  FROM [EDDSDBO].[Artifact]
  where ParentArtifactID=@ArtifactID";

		public const string sqlGetChildrenArtifactIDsByParentAndType = @"
SELECT TOP 1000 [ArtifactID]
  FROM [EDDSDBO].[Artifact]
  WHERE ParentArtifactID = @ParentArtifactID AND ArtifactTypeID = @ArtifactTypeID";

		public const string sqlGetArtifactTypeIdByArtifactGuid = @"
SELECT OT.[DescriptorArtifactTypeID]
  FROM [EDDSDBO].[ArtifactGuid] as AG WITH (NOLOCK)
  INNER JOIN [EDDSDBO].[ObjectType] as OT WITH (NOLOCK)
  ON AG.ArtifactID = OT.ArtifactID 
  WHERE AG.ArtifactGuid = @ArtifactGuid
";
		#endregion
	}
}
