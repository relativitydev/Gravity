using Gravity.Base;
using Gravity.Extensions;
using Gravity.Resources.SQL.Get;
using kCura.Relativity.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Gravity.DAL.SQL
{
	public partial class SqlDao
	{
		public T GetRelativityObject<T>(int artifactId, ObjectFieldsDepthLevel depthLevel)
			where T : BaseDto, new()
		{
			return GetRelativityObjectWithParent<T>(artifactId, depthLevel, null);
		}

		public T GetRelativityObjectWithParent<T>(int artifactId, ObjectFieldsDepthLevel depthLevel, int? parentArtifactId)
			where T : BaseDto, new()
		{
			T returnObject = new T();
			IEnumerable<Guid> propertyGuids = typeof(T).GetPropertyAttributeTuples<RelativityObjectFieldAttribute>().Select(x => x.Item2.FieldGuid);
			Dictionary<Guid, string> fieldsGuidsToColumnNameMappings = GetArtifactGuidsMappingsToColumnNames(propertyGuids);

			DataTable dtTable = (DataTable)this.InvokeGenericMethod(typeof(T), nameof(GetDtTable), artifactId);
			DataRow objRow = dtTable.Rows[0];

			returnObject = objRow.ToHydratedDto<T>(fieldsGuidsToColumnNameMappings, parentArtifactId);
			PopulateChoices(fieldsGuidsToColumnNameMappings, returnObject, dtTable);

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

		internal void PopulateChildrenRecursively<T>(Dictionary<Guid, string> fieldsGuidsToColumnNameMappings, BaseDto baseDto, DataRow objRow, ObjectFieldsDepthLevel depthLevel)
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

		private void PopulateChoices<T>(Dictionary<Guid, string> fieldsGuidsToColumnNameMappings, T dto, DataTable dtTable) where T : BaseDto, new()
		{
			foreach ((PropertyInfo property, RelativityObjectFieldAttribute fieldAttribute)
				in dto.GetType().GetPropertyAttributeTuples<RelativityObjectFieldAttribute>())
			{
				Type objectType = property.PropertyType.IsGenericType && (property.PropertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
								|| property.PropertyType.GetGenericTypeDefinition() == typeof(IList<>)) ?
								property.PropertyType.GetEnumerableOrListInnerType() : property.PropertyType;

				switch (fieldAttribute.FieldType)
				{
					case RdoFieldType.SingleChoice:
						{
							IEnumerable<int> choiceArtifactIds = GetChoicesArtifactIds(dto.ArtifactId, fieldAttribute.FieldGuid);
							int choiceArtifactId = choiceArtifactIds.Count() > 0 ? choiceArtifactIds.Single() : 0;
							property.SetValue(dto, this.InvokeGenericMethod(objectType, nameof(GetChoiceValueByArtifactId), choiceArtifactId));
							break;
						}
					case RdoFieldType.MultipleChoice:
						{
							var multipleChoices = GetChoicesArtifactIds(dto.ArtifactId, fieldAttribute.FieldGuid);
							property.SetValue(dto, this.InvokeGenericMethod(objectType, nameof(GetChoicesValuesByArtifactIds), multipleChoices));
							break;
						}
					default:
						break;
				}
			}
		}

		private object GetChildObjectRecursively(Dictionary<Guid, string> fieldsGuidsToColumnNameMappings, BaseDto baseDto, DataRow objRow, ObjectFieldsDepthLevel depthLevel, PropertyInfo property)
		{
			var relativityObjectFieldAttibutes = property.GetCustomAttribute<RelativityObjectFieldAttribute>();

			Type objectType = property.PropertyType.IsGenericType && (property.PropertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
							|| property.PropertyType.GetGenericTypeDefinition() == typeof(IList<>)) ?
							property.PropertyType.GetEnumerableOrListInnerType() : property.PropertyType;

			if (relativityObjectFieldAttibutes != null)
			{
				var fieldType = relativityObjectFieldAttibutes.FieldType;
				var fieldGuid = relativityObjectFieldAttibutes.FieldGuid;

				if (fieldType == RdoFieldType.MultipleObject)
				{
					IEnumerable<int> multipleObjectsArtifactIds = GetMultipleObjectChildrenArtifactIds(baseDto.ArtifactId, relativityObjectFieldAttibutes.FieldGuid);
					return this.InvokeGenericMethod(objectType, nameof(GetMultipleChildObjectsByArtifactIds), multipleObjectsArtifactIds, depthLevel);
				}

				if (fieldType == RdoFieldType.SingleObject)
				{
					string columnName = fieldsGuidsToColumnNameMappings.FirstOrDefault(x => x.Key == relativityObjectFieldAttibutes.FieldGuid).Value;
					int singleObjectArtifactId = objRow[columnName] != DBNull.Value ? Convert.ToInt32(objRow[columnName]) : 0;

					return singleObjectArtifactId == 0
						? Activator.CreateInstance(objectType)
						: this.InvokeGenericMethod(objectType, nameof(GetSingleChildObjectBasedOnDepthLevelByArtifactId), singleObjectArtifactId, depthLevel);
				}
			}

			if (property.GetCustomAttribute<RelativityObjectChildrenListAttribute>() != null)
			{
				IList returnList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(objectType));
				PropertyInfo parentFieldPropertyInfo = objectType.GetProperties().Where(propInfo => propInfo.GetCustomAttribute<RelativityObjectFieldParentArtifactIdAttribute>() != null).Single();
				Guid parentFieldGuid = parentFieldPropertyInfo == null ? new Guid() : parentFieldPropertyInfo.GetCustomAttribute<RelativityObjectFieldParentArtifactIdAttribute>().FieldGuid;

				int currentChildArtifactTypeID = GetArtifactTypeIdByArtifactGuid(objectType.GetCustomAttribute<RelativityObjectAttribute>().ObjectTypeGuid);

				// TODO HH: Do we want to yield this?
				foreach (IEnumerable<int> childArtifactIds in GetChildrenArtifactIdsByParentAndType(baseDto.ArtifactId, currentChildArtifactTypeID, batchSize))
				{
					foreach (int childArtifactId in childArtifactIds)
					{
						object childArtifact = this.InvokeGenericMethod(objectType, nameof(GetRelativityObjectWithParent), childArtifactId, depthLevel, baseDto.ArtifactId);
						returnList.Add(childArtifact);
					}
				}

				return returnList;
			}

			if (property.GetValue(baseDto, null) is RelativityFile relativityFile)
			{
				int fieldArtifactId = GetArtifactIdByArtifactGuid(relativityObjectFieldAttibutes.FieldGuid);
				return GetFileByFileId(fieldArtifactId, relativityFile.ArtifactTypeId);
			}

			if (property.GetValue(baseDto, null) is kCura.Relativity.Client.DTOs.User user)
			{
				return GetUserBasedOnDepthLevelByArtifactId(user.ArtifactID);
			}

			return null;
		}

		private Dictionary<Guid, string> GetArtifactGuidsMappingsToColumnNames(IEnumerable<Guid> guids)
		{
			var returnDictionary = new Dictionary<Guid, string>();

			StringBuilder sqlStringBuilder = new StringBuilder(SQLGetResource.GetArtifactGuidsMappingToColumnNames);

			sqlStringBuilder.Replace("%%ArtifactGuids%%", string.Join(",", guids.Select(g => $"'{g}'")).TrimEnd(','));

			DataTable dtTable = dbContext.ExecuteSqlStatementAsDataTable(sqlStringBuilder.ToString());

			if (dtTable.Rows.Count > 0)
			{
				returnDictionary = dtTable.AsEnumerable().ToDictionary(row => (Guid)row[0], row => (string)row[1]);
			}

			return returnDictionary;
		}

		private Dictionary<int, Guid> GetArtifactIdGuidMappings(int[] artifactIds)
		{
			var returnDictionary = new Dictionary<int, Guid>();

			StringBuilder sqlStringBuilder = new StringBuilder(SQLGetResource.GetArtifactGuidMappings);

			sqlStringBuilder.Replace("%%ArtifactIDs%%", string.Join(",", artifactIds.Select(id => $"'{id}'")).TrimEnd(','));

			DataTable dtTable = dbContext.ExecuteSqlStatementAsDataTable(sqlStringBuilder.ToString());

			if (dtTable.Rows.Count > 0)
			{
				returnDictionary = dtTable.AsEnumerable().ToDictionary(row => (int)row[0], row => (Guid)row[1]);
			}

			return returnDictionary;
		}

		private IEnumerable<int> GetMultipleObjectChildrenArtifactIds(int artifactId, Guid multipleObjectFieldArtifactGuid)
		{
			List<SqlParameter> sqlParameters = new List<SqlParameter>();
			sqlParameters.Add(new SqlParameter("ArtifactID", artifactId));
			sqlParameters.Add(new SqlParameter("MultipleObjectFieldArtifactGuid", multipleObjectFieldArtifactGuid));

			List<int> returnList = new List<int>();
			using (SqlDataReader reader = dbContext.ExecuteParameterizedSQLStatementAsReader(SQLGetResource.GetMultipleObjectArtifactIDs, sqlParameters))
			{
				while (reader.Read() == true)
				{
					returnList.Add(reader.GetInt32(0));
				}
			}

			return returnList;
		}

		private List<int> GetChoicesArtifactIds(int artifactId, Guid choiceFieldArtifactGuid)
		{
			List<SqlParameter> sqlParameters = new List<SqlParameter>();
			sqlParameters.Add(new SqlParameter("ArtifactID", artifactId));
			sqlParameters.Add(new SqlParameter("ChoiceFieldArtifactGuid", choiceFieldArtifactGuid));

			List<int> returnList = new List<int>();
			using (SqlDataReader reader = dbContext.ExecuteParameterizedSQLStatementAsReader(SQLGetResource.GetChoicesArtifactIDs, sqlParameters))
			{
				while (reader.Read() == true)
				{
					returnList.Add(reader.GetInt32(0));
				}
			}

			return returnList;
		}

		private IEnumerable<IEnumerable<int>> GetChildrenArtifactIds(int parentId, int offset)
		{
			int fetchRows = offset,
				offsetRows = 0,
				resultCount = offset;

			List<SqlParameter> sqlParameters;

			StringBuilder sql;
			List<int> returnList;

			while (resultCount >= fetchRows)
			{
				returnList = new List<int>();
				sql = new StringBuilder(SQLGetResource.GetChildrenArtifactIDsOffset);
				sql.Replace("%%OffsetRows%%", offsetRows.ToString());

				sqlParameters = new List<SqlParameter>()
				{
					new SqlParameter("ParentID", parentId),
					new SqlParameter("FetchRows", fetchRows)
				};

				using (SqlDataReader reader = dbContext.ExecuteParameterizedSQLStatementAsReader(sql.ToString(), sqlParameters))
				{
					while (reader.Read() == true)
					{
						returnList.Add(reader.GetInt32(0));
					}
				}

				offsetRows += offset;
				resultCount = returnList.Count;
				yield return returnList;
			}
		}

		private IEnumerable<int> GetChildrenArtifactIds(int artifactId)
		{
			List<SqlParameter> sqlParameters = new List<SqlParameter>();
			sqlParameters.Add(new SqlParameter("ParentID", artifactId));

			List<int> returnList = new List<int>();
			using (SqlDataReader reader = dbContext.ExecuteParameterizedSQLStatementAsReader(SQLGetResource.GetChildrenArtifactIDs, sqlParameters))
			{
				while (reader.Read() == true)
				{
					returnList.Add(reader.GetInt32(0));
				}
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

			while (resultCount >= fetchRows)
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
					while (reader.Read() == true)
					{
						returnList.Add(reader.GetInt32(0));
					}
				}

				offsetRows += offset;
				resultCount = returnList.Count;
				yield return returnList;
			}
		}

		private IEnumerable<int> GetChildrenArtifactIdsByParentAndType(int parentId, int artifactTypeID)
		{
			List<SqlParameter> sqlParameters = new List<SqlParameter>();
			sqlParameters.Add(new SqlParameter("ParentID", parentId));
			sqlParameters.Add(new SqlParameter("ArtifactTypeID", artifactTypeID));

			List<int> returnList = new List<int>();
			using (SqlDataReader reader = dbContext.ExecuteParameterizedSQLStatementAsReader(SQLGetResource.GetChildrenArtifactIDsByParentAndType, sqlParameters))
			{
				while (reader.Read() == true)
				{
					returnList.Add(reader.GetInt32(0));
				}
			}

			return returnList;
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
			StringBuilder selectBuilder = new StringBuilder();

			selectBuilder.Append("SELECT TOP 1 * ");
			selectBuilder.Append($"FROM [EDDSDBO].[{objectName}] ");
			selectBuilder.Append($"WHERE ArtifactID={artifactId}");

			return selectBuilder.ToString();
		}

		private RelativityFile GetFileByFileId(int fieldArtifactId, int? fileId)
		{
			List<SqlParameter> sqlParameters = new List<SqlParameter>();
			sqlParameters.Add(new SqlParameter("FileId", (int)fileId));
			string query = string.Format(SQLGetResource.GetFileInfoByFileId, fieldArtifactId);

			using (var reader = dbContext.ExecuteParameterizedSQLStatementAsReader(query, sqlParameters.ToArray()))
			{
				while (reader.Read())
				{
					RelativityFile file = new RelativityFile(fieldArtifactId);
					file.FileMetadata = new FileMetadata() { FileName = reader.GetString(1), FileSize = reader.GetInt32(2) };

					string location = reader.GetString(3);
					file.FileValue = new FileValue(location, GetFileBytesByLocation(location));

					return file;
				}
			}

			return null;
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
				return returnList?.Count() < 0 && isNullableType ? null : returnList;

			}

			return null;
		}


		private kCura.Relativity.Client.DTOs.User GetUserBasedOnDepthLevelByArtifactId(int artifactId)
		{
			kCura.Relativity.Client.DTOs.User user = new kCura.Relativity.Client.DTOs.User(artifactId);

			List<SqlParameter> sqlParameters = new List<SqlParameter>();
			sqlParameters.Add(new SqlParameter("CaseUserArtifactId", artifactId));
			sqlParameters.Add(new SqlParameter("CaseArtifactId", workspaceId));

			using (var reader = masterDbContext.ExecuteParameterizedSQLStatementAsReader(SQLGetResource.GetUserDataByIdAndWorkspace, sqlParameters))
			{
				while (reader.Read())
				{
					user.SetValueByPropertyName("FirstName", reader.GetString(0));
					user.SetValueByPropertyName("LastName", reader.GetString(1));
				}
			}

			return user;
		}

		private T GetChoiceValueByArtifactId<T>(int choiceArtifactId)
		{
			return choiceArtifactId > 0 ? ((List<T>)this.InvokeGenericMethod(typeof(T), nameof(GetChoicesValuesByArtifactIds), new List<int>() { choiceArtifactId })).Single()
				: default(T);
		}

		private List<T> GetMultipleChildObjectsByArtifactIds<T>(IEnumerable<int> multipleObjectsArtifactIds, ObjectFieldsDepthLevel depthLevel) where T : BaseDto, new()
		{
			List<T> multipleObjectsList = new List<T>();

			foreach (int artifactId in multipleObjectsArtifactIds)
			{
				multipleObjectsList.Add((T)this.InvokeGenericMethod(typeof(T), nameof(GetSingleChildObjectBasedOnDepthLevelByArtifactId), artifactId, depthLevel));
			}

			return multipleObjectsList;
		}

		private T GetSingleChildObjectBasedOnDepthLevelByArtifactId<T>(int artifactId, ObjectFieldsDepthLevel depthLevel) where T : BaseDto, new()
		{
			T childObject = new T();

			if (depthLevel != ObjectFieldsDepthLevel.OnlyParentObject)
			{
				childObject = (T)this.InvokeGenericMethod(typeof(T), nameof(GetRelativityObject), artifactId, depthLevel);
			}
			else
			{
				childObject.ArtifactId = artifactId;
			}

			return childObject;
		}

		private DataTable GetDtTable<T>(int artifactId) where T : BaseDto
		{
			// Get the object GUID from the BaseDto object mapping
			Guid artifactTypeGuid = BaseDto.GetObjectTypeGuid<T>();
			string objectName = GetObjectNameByGuid(artifactTypeGuid);

			string selectSql = GenerateSelectStatementForObject(artifactId, objectName);

			return dbContext.ExecuteSqlStatementAsDataTable(selectSql);
		}

		private byte[] GetFileBytesByLocation(string location)
		{
			byte[] fileBytes = null;

			if (File.Exists(location))
			{
				fileBytes = invokeWithRetryService.InvokeWithRetry(() => File.ReadAllBytes(location));
			}

			return fileBytes;
		}
	}
}