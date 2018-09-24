using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using Gravity.Base;
using Gravity.Extensions;
using Gravity.Resources.SQL.Get;
using kCura.Relativity.Client.DTOs;

namespace Gravity.DAL.SQL
{
	public partial class SqlDao
	{
		public T Get<T>(int artifactId, ObjectFieldsDepthLevel depthLevel)
			where T : BaseDto, new()
		{
			return GetRelativityObjectWithParent<T>(artifactId, depthLevel, null);
		}

		//TODO: optimize
		public List<T> Get<T>(IList<int> artifactIDs, ObjectFieldsDepthLevel depthLevel)
			where T : BaseDto, new()
		{
			return artifactIDs.Select(x => Get<T>(x, depthLevel)).ToList();
		}


		private T GetRelativityObjectWithParent<T>(int artifactId, ObjectFieldsDepthLevel depthLevel, int? parentArtifactId)
			where T : BaseDto, new()
		{
			IEnumerable<Guid> propertyGuids = typeof(T).GetPropertyAttributeTuples<RelativityObjectFieldAttribute>().Select(x => x.Item2.FieldGuid);
			Dictionary<Guid, string> fieldsGuidsToColumnNameMappings = GetArtifactGuidsMappingsToColumnNames(propertyGuids);

			DataTable dtTable = GetDtTable<T>(artifactId);
			DataRow objRow = dtTable.Rows[0];

			var returnObject = objRow.ToHydratedDto<T>(fieldsGuidsToColumnNameMappings, parentArtifactId);
			PopulateChoices(returnObject);
			PopulateFiles(fieldsGuidsToColumnNameMappings, returnObject, objRow);

			switch (depthLevel)
			{
				case ObjectFieldsDepthLevel.OnlyParentObject:
					break;
				case ObjectFieldsDepthLevel.FirstLevelOnly:
					PopulateChildrenRecursively<T>(fieldsGuidsToColumnNameMappings, returnObject, objRow, ObjectFieldsDepthLevel.OnlyParentObject);
					break;
				case ObjectFieldsDepthLevel.FullyRecursive:
					PopulateChildrenRecursively<T>(fieldsGuidsToColumnNameMappings, returnObject, objRow, ObjectFieldsDepthLevel.FullyRecursive);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(depthLevel));
			}

			return returnObject;
		}

		private void PopulateChildrenRecursively<T>(Dictionary<Guid, string> fieldsGuidsToColumnNameMappings, BaseDto baseDto, DataRow objRow, ObjectFieldsDepthLevel depthLevel)
		where T : BaseDto
		{
			foreach (var objectPropertyInfo in baseDto.GetType().GetPublicProperties())
			{
				var childValue = GetChildObjectRecursively(fieldsGuidsToColumnNameMappings, baseDto, objRow, depthLevel, objectPropertyInfo);

				if (childValue != null)
				{
					objectPropertyInfo.SetValue(baseDto, childValue);
				}
			}
		}

		private void PopulateChoices<T>(T dto)
			where T : BaseDto, new()
		{
			Type objectType;

			foreach ((PropertyInfo property, RelativityObjectFieldAttribute fieldAttribute)
				in dto.GetType().GetPropertyAttributeTuples<RelativityObjectFieldAttribute>())
			{
				switch (fieldAttribute.FieldType)
				{
					case RdoFieldType.SingleChoice:
						{
							objectType = property.PropertyType;
							IEnumerable<int> choiceArtifactIds = GetChoicesArtifactIds(dto.ArtifactId, fieldAttribute.FieldGuid);
							int choiceArtifactId = choiceArtifactIds.Count() > 0 ? choiceArtifactIds.Single() : 0;
							property.SetValue(dto, this.InvokeGenericMethod(objectType, nameof(GetChoiceValueByArtifactId), choiceArtifactId));
							break;
						}
					case RdoFieldType.MultipleChoice:
						{
							objectType = property.PropertyType.GetEnumerableInnerType();
							var multipleChoices = GetChoicesArtifactIds(dto.ArtifactId, fieldAttribute.FieldGuid);
							property.SetValue(dto, this.InvokeGenericMethod(objectType, nameof(GetChoicesValuesByArtifactIds), multipleChoices));
							break;
						}
					default:
						break;
				}
			}
		}

		private void PopulateFiles<T>(Dictionary<Guid, string> fieldsGuidsToColumnNameMappings, T dto, DataRow objRow)
			where T : BaseDto
		{
			object fileDto;

			foreach ((PropertyInfo property, RelativityObjectFieldAttribute fieldAttribute)
						in dto.GetType()
							.GetPropertyAttributeTuples<RelativityObjectFieldAttribute>()
							.Where(x => x.Item2.FieldType == RdoFieldType.File))
			{
				fileDto = null;

				// Substract "Name" appendix from column name
				var columnName = fieldsGuidsToColumnNameMappings[fieldAttribute.FieldGuid]
					.Substring(0, fieldsGuidsToColumnNameMappings[fieldAttribute.FieldGuid].Length - 4);
				
				if (property.PropertyType == (typeof(FileDto))
				&& objRow.IsNull(columnName) == false)
				{
					int fieldArtifactId = GetArtifactIdByArtifactGuid(fieldAttribute.FieldGuid);
					fileDto = GetFileByFileId(fieldArtifactId, Convert.ToInt32(objRow[columnName]));
				}

				property.SetValue(dto, fileDto);
			}
		}

		private object GetChildObjectRecursively(Dictionary<Guid, string> fieldsGuidsToColumnNameMappings, BaseDto baseDto, DataRow objRow, ObjectFieldsDepthLevel depthLevel, PropertyInfo property)
		{
			var relativityObjectFieldAttibutes = property.GetCustomAttribute<RelativityObjectFieldAttribute>();
			object returnObj = null;

			Type objectType = property.PropertyType.IsGenericType && (property.PropertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
							|| property.PropertyType.GetGenericTypeDefinition() == typeof(IList<>)) ?
							property.PropertyType.GetEnumerableInnerType() : property.PropertyType;

			if (relativityObjectFieldAttibutes != null)
			{
				var fieldType = relativityObjectFieldAttibutes.FieldType;
				var fieldGuid = relativityObjectFieldAttibutes.FieldGuid;

				string columnName = fieldsGuidsToColumnNameMappings.FirstOrDefault(x => x.Key == relativityObjectFieldAttibutes.FieldGuid).Value;

				switch (fieldType)
				{
					case RdoFieldType.MultipleObject:
						IEnumerable<int> multipleObjectsArtifactIds = GetMultipleObjectChildrenArtifactIds(baseDto.ArtifactId, relativityObjectFieldAttibutes.FieldGuid);
						returnObj = this.InvokeGenericMethod(objectType, nameof(GetMultipleChildObjectsByArtifactIds), multipleObjectsArtifactIds, depthLevel);
						break;

					case RdoFieldType.SingleObject:
						int singleObjectArtifactId = objRow.IsNull(columnName) ? 0 : Convert.ToInt32(objRow[columnName]);

						if (singleObjectArtifactId == 0)
							return null;

						this.InvokeGenericMethod(objectType, nameof(GetSingleChildObjectBasedOnDepthLevelByArtifactId), singleObjectArtifactId, depthLevel);
						break;

					case RdoFieldType.User:
						if (property.PropertyType == typeof(User)
							&& objRow.IsNull(columnName) == false)
						{
							returnObj = GetUserBasedOnDepthLevelByArtifactId(Convert.ToInt32(objRow[columnName]));
						}
						break;
				}
			}

			if (property.GetCustomAttribute<RelativityObjectChildrenListAttribute>() != null)
			{
				IList returnList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(objectType));

				PropertyInfo parentFieldPropertyInfo = objectType.GetProperties().Where(propInfo => propInfo.GetCustomAttribute<RelativityObjectFieldParentArtifactIdAttribute>() != null).Single();

				int currentChildArtifactTypeID = GetArtifactTypeIdByArtifactGuid(objectType.GetCustomAttribute<RelativityObjectAttribute>().ObjectTypeGuid);

				foreach (IEnumerable<int> childArtifactIds in GetChildrenArtifactIdsByParentAndType(baseDto.ArtifactId, currentChildArtifactTypeID, batchSize))
				{
					foreach (int childArtifactId in childArtifactIds)
					{
						object childArtifact = this.InvokeGenericMethod(objectType, nameof(GetRelativityObjectWithParent), childArtifactId, depthLevel, baseDto.ArtifactId);
						returnList.Add(childArtifact);
					}
				}

				returnObj = returnList;
			}

			return returnObj;
		}

		private Dictionary<Guid, string> GetArtifactGuidsMappingsToColumnNames(IEnumerable<Guid> guids)
		{
			StringBuilder sqlStringBuilder = new StringBuilder(SQLGetResource.GetArtifactGuidsMappingToColumnNames);

			sqlStringBuilder.Replace("%%ArtifactGuids%%", string.Join(",", guids.Select(g => $"'{g}'")).TrimEnd(','));

			DataTable dtTable = dbContext.ExecuteSqlStatementAsDataTable(sqlStringBuilder.ToString());

			return dtTable.AsEnumerable().ToDictionary(row => (Guid)row[0], row => (string)row[1]);
		}

		private IEnumerable<int> GetMultipleObjectChildrenArtifactIds(int artifactId, Guid multipleObjectFieldArtifactGuid)
		{
			List<SqlParameter> sqlParameters = new List<SqlParameter>();
			sqlParameters.Add(new SqlParameter("ArtifactID", artifactId));
			sqlParameters.Add(new SqlParameter("MultipleObjectFieldArtifactGuid", multipleObjectFieldArtifactGuid));

			List<int> returnList = new List<int>();
			using (SqlDataReader reader = dbContext.ExecuteParameterizedSQLStatementAsReader(SQLGetResource.GetMultipleObjectArtifactIDs, sqlParameters))
			{
				returnList = GetListOfIds(reader, 0);
			}

			return returnList;
		}

		private IList<int> GetChoicesArtifactIds(int artifactId, Guid choiceFieldArtifactGuid)
		{
			List<SqlParameter> sqlParameters = new List<SqlParameter>();
			sqlParameters.Add(new SqlParameter("ArtifactID", artifactId));
			sqlParameters.Add(new SqlParameter("ChoiceFieldArtifactGuid", choiceFieldArtifactGuid));

			IList<int> returnList = new List<int>();
			using (SqlDataReader reader = dbContext.ExecuteParameterizedSQLStatementAsReader(SQLGetResource.GetChoicesArtifactIDs, sqlParameters))
			{
				returnList = GetListOfIds(reader, 0);
			}

			return returnList;
		}

		private IEnumerable<IEnumerable<int>> GetChildrenArtifactIdsByParentAndType(int parentId, int artifactTypeID, int offset)
		{
			int fetchRows = offset,
				offsetRows = 0,
				resultCount = offset;

			StringBuilder sql;
			List<int> returnList;
			List<SqlParameter> sqlParameters;

			while (resultCount >= fetchRows && fetchRows > 0)
			{
				returnList = new List<int>();
				sql = new StringBuilder(SQLGetResource.GetChildrenArtifactIDsByParentAndTypeOffset);
				sql.Replace("%%OffsetRows%%", offsetRows.ToString());

				sqlParameters = new List<SqlParameter>()
				{
					new SqlParameter("ParentID", parentId),
					new SqlParameter("ArtifactTypeID", artifactTypeID),
					new SqlParameter("FetchRows", fetchRows)
				};

				using (SqlDataReader reader = dbContext.ExecuteParameterizedSQLStatementAsReader(sql.ToString(), sqlParameters))
				{
					returnList = GetListOfIds(reader, 0);
				}

				offsetRows += offset;
				resultCount = returnList.Count;
				yield return returnList;
			}
		}

		private int GetArtifactIdByArtifactGuid(Guid artifactGuid)
		{
			SqlParameter[] sqlParameters = new SqlParameter[] { new SqlParameter("ArtifactGuid", artifactGuid) };

			int returnId = (int)dbContext.ExecuteSqlStatementAsScalar(SQLGetResource.GetArtifactIdByArtifactGuid, sqlParameters);

			return returnId;
		}

		private int GetArtifactTypeIdByArtifactGuid(Guid artifactGuid)
		{
			SqlParameter[] sqlParameters = new SqlParameter[] { new SqlParameter("ArtifactGuid", artifactGuid) };

			int returnId = (int)dbContext.ExecuteSqlStatementAsScalar(SQLGetResource.GetArtifactTypeIdByArtifactGuid, sqlParameters);

			return returnId;
		}

		private string GetObjectNameByGuid(Guid artifactTypeGuid)
		{
			int objectArtifactTypeId = GetArtifactTypeIdByArtifactGuid(artifactTypeGuid);

			return GetArtifactTypeByArtifactTypeId(objectArtifactTypeId);
		}

		private string GetArtifactTypeByArtifactTypeId(int artifactTypeId)
		{
			SqlParameter[] sqlParameters = new SqlParameter[] { new SqlParameter("ArtifactTypeID", artifactTypeId) };

			string artifactType = dbContext.ExecuteSqlStatementAsScalar(SQLGetResource.GetArtifactTypeByArtifactTypeId, sqlParameters).ToString();

			return artifactType;
		}

		private string GenerateSelectStatementForObject(int artifactId, string objectName)
		{
			StringBuilder selectBuilder = new StringBuilder(512);

			selectBuilder.Append("SELECT TOP 1 * ");
			selectBuilder.Append($"FROM [EDDSDBO].[{objectName}] ");
			selectBuilder.Append($"WHERE [ArtifactID] = {artifactId}");

			return selectBuilder.ToString();
		}

		private object GetFileByFileId(int fieldArtifactId, int fileId)
		{
			FileDto relativityFile = null;

			var sqlParameters = new List<SqlParameter>();
			sqlParameters.Add(new SqlParameter("FileId", fileId));
			string query = string.Format(SQLGetResource.GetFileInfoByFileId, fieldArtifactId);

			using (var reader = dbContext.ExecuteParameterizedSQLStatementAsReader(query, sqlParameters.ToArray()))
			{
				while (reader.Read())
				{
					// TODO: Implement Caching for SQL
					string location = reader.GetString(0);
					relativityFile = new DiskFileDto(location).StoreInMemory();
				}
			}

			return relativityFile;
		}

		private List<T> GetChoicesValuesByArtifactIds<T>(List<int> choiceArtifactIds)
		{
			List<T> returnList = new List<T>();
			bool isNullableType = typeof(T).IsGenericType == true && typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>);
			Type enumType = isNullableType ? typeof(T).GetGenericArguments()[0] : typeof(T);
			IEnumerable<MemberInfo> typeMembersCollection = enumType.GetMembers().Where(member => member.GetCustomAttribute<RelativityObjectAttribute>() != null);

			if (choiceArtifactIds != null && choiceArtifactIds.Count() > 0)
			{
				string artifactsStringSequence = string.Join(",", choiceArtifactIds.ToArray());
				string sqlStatement = SQLGetResource.GetArtifactGuidMappings.Replace("%%ArtifactIDs%%", artifactsStringSequence);

				using (SqlDataReader choicesReader = dbContext.ExecuteSQLStatementAsReader(sqlStatement))
				{
					while (choicesReader.Read())
					{
						MemberInfo choiceTypeMember = typeMembersCollection.Single(member => member.GetCustomAttribute<RelativityObjectAttribute>()?.ObjectTypeGuid == choicesReader.GetGuid(1));
						T choiceValue = (T)Enum.Parse(enumType, choiceTypeMember.Name);
						returnList.Add(choiceValue);
					}
				}

				return returnList.Count() <= 0 && isNullableType ? null : returnList;
			}

			return returnList;
		}

		private User GetUserBasedOnDepthLevelByArtifactId(int artifactId)
		{
			var user = new User(artifactId);

			List<SqlParameter> sqlParameters = new List<SqlParameter>();
			sqlParameters.Add(new SqlParameter("CaseUserArtifactId", artifactId));
			sqlParameters.Add(new SqlParameter("CaseArtifactId", workspaceId));

			using (var reader = masterDbContext.ExecuteParameterizedSQLStatementAsReader(SQLGetResource.GetUserDataByIdAndWorkspace, sqlParameters))
			{
				while (reader.Read())
				{
					user.FirstName = reader.GetString(0);
					user.LastName = reader.GetString(1);
				}
			}

			return user;
		}

		private T GetChoiceValueByArtifactId<T>(int choiceArtifactId)
		{
			return choiceArtifactId > 0 ? 
				this.GetChoicesValuesByArtifactIds<T>(new List<int>() { choiceArtifactId }).Single()
				: default(T);
		}

		private List<T> GetMultipleChildObjectsByArtifactIds<T>(IEnumerable<int> multipleObjectsArtifactIds, ObjectFieldsDepthLevel depthLevel) where T : BaseDto, new()
		{
			List<T> multipleObjectsList = new List<T>();

			foreach (int artifactId in multipleObjectsArtifactIds)
			{
				multipleObjectsList.Add(this.GetSingleChildObjectBasedOnDepthLevelByArtifactId<T>(artifactId, depthLevel));
			}

			return multipleObjectsList;
		}

		private T GetSingleChildObjectBasedOnDepthLevelByArtifactId<T>(int artifactId, ObjectFieldsDepthLevel depthLevel) where T : BaseDto, new()
		{
			return depthLevel != ObjectFieldsDepthLevel.OnlyParentObject ?
				this.Get<T>(artifactId, depthLevel)
				: new T() { ArtifactId = artifactId };
		}

		private DataTable GetDtTable<T>(int artifactId) where T : BaseDto
		{
			// Get the object GUID from the BaseDto object mapping
			Guid artifactTypeGuid = BaseDto.GetObjectTypeGuid<T>();
			string objectName = GetObjectNameByGuid(artifactTypeGuid);

			string selectSql = GenerateSelectStatementForObject(artifactId, objectName);

			return dbContext.ExecuteSqlStatementAsDataTable(selectSql);
		}

		private List<int> GetListOfIds(SqlDataReader reader, int idsPosition)
		{
			List<int> returnList = new List<int>();

			while (reader.Read())
			{
				returnList.Add(reader.GetInt32(idsPosition));
			}

			return returnList;
		}
	}
}