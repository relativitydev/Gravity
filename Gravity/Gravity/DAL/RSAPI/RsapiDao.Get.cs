using kCura.Relativity.Client;
using kCura.Relativity.Client.DTOs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Gravity.Base;
using Gravity.Exceptions;
using Gravity.Extensions;

namespace Gravity.DAL.RSAPI
{
	public partial class RsapiDao
	{
		#region RDO GET Protected stuff		
		protected RDO GetRdo(int artifactId)
		{
			using (IRSAPIClient proxyToWorkspace = CreateProxy())
			{
				try
				{
					return invokeWithRetryService.InvokeWithRetry(() => proxyToWorkspace.Repositories.RDO.ReadSingle(artifactId));
				}
				catch (Exception ex)
				{
					throw new ProxyOperationFailedException("Failed in method: " + MethodInfo.GetCurrentMethod(), ex);
				}
			}
		}

		protected List<RDO> GetRdos(int[] artifactIds)
		{
			ResultSet<RDO> resultSet;
			using (IRSAPIClient proxyToWorkspace = CreateProxy())
			{
				try
				{
					resultSet = invokeWithRetryService.InvokeWithRetry(() => proxyToWorkspace.Repositories.RDO.Read(artifactIds));
				}
				catch (Exception ex)
				{
					throw new ProxyOperationFailedException("Failed in method: " + MethodInfo.GetCurrentMethod(), ex);
				}
			}

			if (resultSet.Success == false)
			{
				throw new ArgumentException(resultSet.Message);
			}

			return resultSet.Results.Select(items => items.Artifact).ToList();
		}

		protected List<RDO> GetRdos<T>(Condition queryCondition = null)
			where T : BaseDto
		{
			Query<RDO> query = new Query<RDO>()
			{
				ArtifactTypeGuid = BaseDto.GetObjectTypeGuid<T>(),
				Condition = queryCondition,
				Fields = FieldValue.AllFields
			};

			ResultSet<RDO> results;
			using (IRSAPIClient proxyToWorkspace = CreateProxy())
			{
				try
				{
					results = invokeWithRetryService.InvokeWithRetry(() => proxyToWorkspace.Repositories.RDO.Query(query));
				}
				catch (Exception ex)
				{
					throw new ProxyOperationFailedException("Failed in method: " + MethodInfo.GetCurrentMethod(), ex);
				}
			}

			if (results.Success == false)
			{
				throw new ArgumentException(results.Message);
			}

			return results.Results.Select(result => result.Artifact).ToList();
		}

		protected RelativityFile GetFile(int fileFieldArtifactId, int ourFileContainerInstanceArtifactId)
		{
			using (IRSAPIClient proxyToWorkspace = CreateProxy())
			{
				var fileRequest = new FileRequest(proxyToWorkspace.APIOptions)
				{
					Target =
					{
						FieldId = fileFieldArtifactId,
						ObjectArtifactId = ourFileContainerInstanceArtifactId
					}
				};

				KeyValuePair<DownloadResponse, Stream> fileData;

				try
				{
					fileData = invokeWithRetryService.InvokeWithRetry(() => proxyToWorkspace.Download(fileRequest));
				}
				catch (Exception ex)
				{
					throw new ProxyOperationFailedException("Failed in method: " + MethodBase.GetCurrentMethod(), ex);
				}

				using (MemoryStream ms = (MemoryStream)fileData.Value)
				{
					FileValue fileValue = new FileValue(null, ms.ToArray());
					FileMetadata fileMetadata = fileData.Key.Metadata;

					return new RelativityFile(fileFieldArtifactId, fileValue, fileMetadata);
				}
			}
		}

		#endregion

		public List<T> GetAllDTOs<T>(Condition queryCondition = null, ObjectFieldsDepthLevel depthLevel = ObjectFieldsDepthLevel.FirstLevelOnly)
			where T : BaseDto, new()
		{
			List<RDO> objectsRdos = GetRdos<T>(queryCondition);
			return objectsRdos.Select(rdo => GetHydratedDTO<T>(rdo, depthLevel)).ToList();
		}

		public List<T> GetAllChildDTOs<T>(Guid parentFieldGuid, int parentArtifactID, ObjectFieldsDepthLevel depthLevel)
			where T : BaseDto, new()
		{
			Condition queryCondition = new WholeNumberCondition(parentFieldGuid, NumericConditionEnum.EqualTo, parentArtifactID);
			return GetAllDTOs<T>(queryCondition, depthLevel);
		}

		public List<T> GetDTOs<T>(int[] artifactIDs, ObjectFieldsDepthLevel depthLevel)
			where T : BaseDto, new()
		{
			List<RDO> objectsRdos = GetRdos(artifactIDs);
			return objectsRdos.Select(rdo => GetHydratedDTO<T>(rdo, depthLevel)).ToList();
		}

		public T GetRelativityObject<T>(int artifactID, ObjectFieldsDepthLevel depthLevel)
			where T : BaseDto, new()
		{
			RDO objectRdo = GetRdo(artifactID);

			return GetHydratedDTO<T>(objectRdo, depthLevel);
		}

		private T GetHydratedDTO<T>(RDO objectRdo, ObjectFieldsDepthLevel depthLevel)
			where T : BaseDto, new()
		{
			T dto = objectRdo.ToHydratedDto<T>();

			switch (depthLevel)
			{
				case ObjectFieldsDepthLevel.OnlyParentObject:
					break;
				case ObjectFieldsDepthLevel.FirstLevelOnly:
					PopulateChildrenRecursively<T>(dto, objectRdo, ObjectFieldsDepthLevel.OnlyParentObject);
					break;
				case ObjectFieldsDepthLevel.FullyRecursive:
					PopulateChildrenRecursively<T>(dto, objectRdo, ObjectFieldsDepthLevel.FullyRecursive);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(depthLevel));
			}

			return dto;
		}

		internal void PopulateChildrenRecursively<T>(BaseDto baseDto, RDO objectRdo, ObjectFieldsDepthLevel depthLevel)
		{
			foreach (var objectPropertyInfo in BaseDto.GetRelativityMultipleObjectPropertyInfos<T>())
			{
				var propertyInfo = objectPropertyInfo.Key;
				var theMultipleObjectAttribute = objectPropertyInfo.Value;

				Type childType = objectPropertyInfo.Value.ChildType;

				int[] childArtifactIds = objectRdo[objectPropertyInfo.Value.FieldGuid]
					.GetValueAsMultipleObject<kCura.Relativity.Client.DTOs.Artifact>()
					.Select(artifact => artifact.ArtifactID)
					.ToArray();

				MethodInfo method = GetGenericMethod(nameof(GetDTOs), childType);
				var allObjects = method.Invoke(this, new object[] { childArtifactIds, depthLevel }) as IEnumerable;

				var returnList = MakeGenericList(allObjects, theMultipleObjectAttribute.ChildType);

				propertyInfo.SetValue(baseDto, returnList);
			}

			foreach (var objectPropertyInfo in BaseDto.GetRelativitySingleObjectPropertyInfos<T>())
			{
				var propertyInfo = objectPropertyInfo.Key;

				Type objectType = objectPropertyInfo.Value.ChildType;
				var singleObject = Activator.CreateInstance(objectType);

				int childArtifactId = objectRdo[objectPropertyInfo.Value.FieldGuid].ValueAsSingleObject.ArtifactID;

				if (childArtifactId != 0)
				{
					MethodInfo method = GetGenericMethod(nameof(GetRelativityObject), objectType);
					singleObject = method.Invoke(this, new object[] { childArtifactId, depthLevel });
				}

				propertyInfo.SetValue(baseDto, singleObject);
			}

			foreach (var childPropertyInfo in BaseDto.GetRelativityObjectChildrenListInfos<T>())
			{
				var propertyInfo = childPropertyInfo.Key;
				var theChildAttribute = childPropertyInfo.Value;

				Type childType = childPropertyInfo.Value.ChildType;
				Guid parentFieldGuid = childType.GetRelativityObjectGuidForParentField();

				MethodInfo method = GetGenericMethod(nameof(GetAllChildDTOs), childType);
				var allChildObjects = method.Invoke(this, new object[] { parentFieldGuid, baseDto.ArtifactId, depthLevel }) as IEnumerable;

				var returnList = MakeGenericList(allChildObjects, theChildAttribute.ChildType);

				propertyInfo.SetValue(baseDto, returnList);
			}

			foreach (var filePropertyInfo in baseDto.GetType().GetPublicProperties()
				.Where(prop => prop.PropertyType == typeof(RelativityFile)))
			{
				var filePropertyValue = filePropertyInfo.GetValue(baseDto, null) as RelativityFile;

				if (filePropertyValue != null)
				{
					filePropertyValue = GetFile(filePropertyValue.ArtifactTypeId, baseDto.ArtifactId);
				}

				filePropertyInfo.SetValue(baseDto, filePropertyValue);
			}
		}

		private static MethodInfo GetGenericMethod(string methodName, params Type[] types)
		{
			return typeof(RsapiDao)
				.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
				.MakeGenericMethod(types);
		}

		private static IList MakeGenericList(IEnumerable items, Type type)
		{
			var listType = typeof(List<>).MakeGenericType(type);
			IList returnList = (IList)Activator.CreateInstance(listType);

			foreach (var item in items)
			{
				returnList.Add(item);
			}

			return returnList;
		}
	}
}
