-- PARAMS:
--DECLARE @ArtifactID int
--DECLARE @MultipleObjectFieldArtifactGuid uniqueidentifier
--SET @ArtifactID = 1068172
--SET @MultipleObjectFieldArtifactGuid = 'd0770889-8a4d-436a-9647-33419b96e37e'

DECLARE @MultipleObjectFieldArtifactID INT

SELECT @MultipleObjectFieldArtifactID = [ArtifactID]
  FROM [EDDSDBO].[ArtifactGuid] (NOLOCK)
  WHERE ArtifactGuid = @MultipleObjectFieldArtifactGuid

-- SELECT @MultipleObjectFieldArtifactID

DECLARE @RelationalTableSchemaName NVARCHAR(100)
DECLARE @FieldArtifactIDColumnName NVARCHAR(100)
DECLARE @ChildArtifactIDsColumnName NVARCHAR(100)
DECLARE @Relation1to2 INT

-- Get the multi-relation 1-to-2 or 2-to-1
SELECT
	@RelationalTableSchemaName = [RelationalTableSchemaName]
	,@FieldArtifactIDColumnName = [RelationalTableFieldColumnName1]
	,@ChildArtifactIDsColumnName = [RelationalTableFieldColumnName2]
	,@Relation1to2 = [FieldArtifactId1] - @MultipleObjectFieldArtifactID
  FROM [EDDSDBO].[ObjectsFieldRelation] (NOLOCK)
  WHERE [FieldArtifactId1] = @MultipleObjectFieldArtifactID OR [FieldArtifactId2] = @MultipleObjectFieldArtifactId

If (@Relation1to2 <> 0)
BEGIN
	DECLARE @TempColumnName NVARCHAR(100)
	SET @TempColumnName = @FieldArtifactIDColumnName
	SET @FieldArtifactIDColumnName = @ChildArtifactIDsColumnName
	SET @ChildArtifactIDsColumnName = @TempColumnName
END

-- SELECT @RelationalTableSchemaName, @FieldArtifactIDColumnName ,@ChildArtifactIDsColumnName, @Relation1to2

-- Now that we know the relation table and the kyes, get the child artifact IDs
DECLARE @sqlGetChildrenArtifactIDsScript NVARCHAR(MAX)
SET @sqlGetChildrenArtifactIDsScript =
	N'SELECT [' + @ChildArtifactIDsColumnName  + '] 
	FROM  [EDDSDBO].[' + @RelationalTableSchemaName + '] (NOLOCK)
	WHERE [' + @FieldArtifactIDColumnName  + ']=' + CONVERT(NVARCHAR(20), @ArtifactID)

--SELECT @sqlGetChildrenArtifactIDsScript

EXECUTE sp_executesql @sqlGetChildrenArtifactIDsScript