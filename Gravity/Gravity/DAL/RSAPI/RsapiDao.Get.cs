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
			RDO returnObject = null;

			using (IRSAPIClient proxyToWorkspace = CreateProxy())
			{
				try
				{
					returnObject = invokeWithRetryService.InvokeWithRetry(() => proxyToWorkspace.Repositories.RDO.ReadSingle(artifactId));
				}
				catch (Exception ex)
				{
					throw new ProxyOperationFailedException("Failed in method: " + System.Reflection.MethodInfo.GetCurrentMethod(), ex);
				}
			}

			return returnObject;
		}

		protected List<RDO> GetRdos(int[] artifactIds)
		{
			ResultSet<RDO> resultSet = new ResultSet<RDO>();
			using (IRSAPIClient proxyToWorkspace = CreateProxy())
			{
				try
				{
					resultSet = invokeWithRetryService.InvokeWithRetry(() => proxyToWorkspace.Repositories.RDO.Read(artifactIds));
				}
				catch (Exception ex)
				{
					throw new ProxyOperationFailedException("Failed in method: " + System.Reflection.MethodInfo.GetCurrentMethod(), ex);
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
				Condition = queryCondition
			};

			query.Fields = FieldValue.AllFields;

			QueryResultSet<RDO> results;
			using (IRSAPIClient proxyToWorkspace = CreateProxy())
			{
				try
				{
					results = invokeWithRetryService.InvokeWithRetry(() => proxyToWorkspace.Repositories.RDO.Query(query));
				}
				catch (Exception ex)
				{
					throw new ProxyOperationFailedException("Failed in method: " + System.Reflection.MethodInfo.GetCurrentMethod(), ex);
				}
			}

			if (results.Success == false)
			{
				throw new ArgumentException(results.Message);
			}

			return results.Results.Select<Result<RDO>, RDO>(result => result.Artifact as RDO).ToList();
		}

		protected RelativityFile GetFile(int fileFieldArtifactId, int ourFileContainerInstanceArtifactId)
		{
			using (IRSAPIClient proxyToWorkspace = CreateProxy())
			{
				try
				{
					var fileRequest = new FileRequest(proxyToWorkspace.APIOptions);
					fileRequest.Target.FieldId = fileFieldArtifactId;
					fileRequest.Target.ObjectArtifactId = ourFileContainerInstanceArtifactId;

					RelativityFile returnValue;
					var fileData = invokeWithRetryService.InvokeWithRetry(() => proxyToWorkspace.Download(fileRequest));

					using (MemoryStream ms = (MemoryStream)fileData.Value)
					{
						FileValue fileValue = new FileValue(null, ms.ToArray());
						FileMetadata fileMetadata = fileData.Key.Metadata;

						returnValue = new RelativityFile(fileFieldArtifactId, fileValue, fileMetadata);
					}

					return returnValue;
				}
				catch (Exception ex)
				{
					throw new ProxyOperationFailedException("Failed in method: " + System.Reflection.MethodInfo.GetCurrentMethod(), ex);
				}
			}
		}
		#endregion

		public List<T> GetAllDTOs<T>()
			where T : BaseDto, new()
		{
			List<RDO> objectsRdos = GetRdos<T>();

			return objectsRdos.Select<RDO, T>(rdo => rdo.ToHydratedDto<T>()).ToList();
		}

		public List<T> GetAllDTOs<T>(Condition queryCondition = null, ObjectFieldsDepthLevel depthLevel = ObjectFieldsDepthLevel.FirstLevelOnly)
			where T : BaseDto, new()
		{
			List<T> returnList = null;

			List<RDO> objectsRdos = GetRdos<T>(queryCondition);

			switch (depthLevel)
			{
				case ObjectFieldsDepthLevel.FirstLevelOnly:
					returnList = objectsRdos.Select<RDO, T>(rdo => rdo.ToHydratedDto<T>()).ToList();
					break;
				case ObjectFieldsDepthLevel.FullyRecursive:
					var allDtos = new List<T>();

					foreach (var rdo in objectsRdos)
					{
						var dto = rdo.ToHydratedDto<T>();

						PopulateChildrenRecursively<T>(dto, rdo, depthLevel);

						allDtos.Add(dto);
					}

					returnList = allDtos;
					break;
				default:
					return objectsRdos.Select<RDO, T>(rdo => rdo.ToHydratedDto<T>()).ToList();

			}

			return returnList;
		}

		public List<T> GetAllChildDTOs<T>(Guid parentFieldGuid, int parentArtifactID, ObjectFieldsDepthLevel depthLevel)
			where T : BaseDto, new()
		{
			Condition queryCondition = new WholeNumberCondition(parentFieldGuid, NumericConditionEnum.EqualTo, parentArtifactID);
			List<RDO> objectsRdos = GetRdos<T>(queryCondition);

			switch (depthLevel)
			{
				case ObjectFieldsDepthLevel.FirstLevelOnly:
					return objectsRdos.Select<RDO, T>(rdo => rdo.ToHydratedDto<T>()).ToList();
				case ObjectFieldsDepthLevel.FullyRecursive:
					var allChildDtos = new List<T>();
					foreach (var childRdo in objectsRdos)
					{
						var childDto = childRdo.ToHydratedDto<T>();

						PopulateChildrenRecursively<T>(childDto, childRdo, depthLevel);

						allChildDtos.Add(childDto);
					}
					return allChildDtos;
				default:
					return objectsRdos.Select<RDO, T>(rdo => rdo.ToHydratedDto<T>()).ToList();
			}
		}

		public List<T> GetDTOs<T>(int[] artifactIDs, ObjectFieldsDepthLevel depthLevel)
			where T : BaseDto, new()
		{
			List<RDO> objectsRdos = GetRdos(artifactIDs);
			switch (depthLevel)
			{
				case ObjectFieldsDepthLevel.FirstLevelOnly:
					return objectsRdos.Select<RDO, T>(rdo => rdo.ToHydratedDto<T>()).ToList();
				case ObjectFieldsDepthLevel.FullyRecursive:
					var allDtos = new List<T>();

					foreach (var rdo in objectsRdos)
					{
						var dto = rdo.ToHydratedDto<T>();

						PopulateChildrenRecursively<T>(dto, rdo, depthLevel);

						allDtos.Add(dto);
					}

					return allDtos;
				default:
					return objectsRdos.Select<RDO, T>(rdo => rdo.ToHydratedDto<T>()).ToList();
			}
		}

		internal T GetDTO<T>(int artifactID, ObjectFieldsDepthLevel depthLevel)
			where T : BaseDto, new()
		{
			RDO objectRdo = GetRdo(artifactID);

			switch (depthLevel)
			{
				case ObjectFieldsDepthLevel.FirstLevelOnly:
					return objectRdo.ToHydratedDto<T>();
				case ObjectFieldsDepthLevel.FullyRecursive:
					T dto = objectRdo.ToHydratedDto<T>();
					PopulateChildrenRecursively<T>(dto, objectRdo, depthLevel);
					return dto;
				default:
					return objectRdo.ToHydratedDto<T>();
			}
		}

		internal void PopulateChildrenRecursively<T>(BaseDto baseDto, RDO objectRdo, ObjectFieldsDepthLevel depthLevel)
		{
			foreach (var objectPropertyInfo in BaseDto.GetRelativityMultipleObjectPropertyInfos<T>())
			{
				var propertyInfo = objectPropertyInfo.Key;
				var theMultipleObjectAttribute = objectPropertyInfo.Value;

				Type childType = objectPropertyInfo.Value.ChildType;

				int[] childArtifactIds = objectRdo[objectPropertyInfo.Value.FieldGuid].GetValueAsMultipleObject<kCura.Relativity.Client.DTOs.Artifact>()
							.Select<kCura.Relativity.Client.DTOs.Artifact, int>(artifact => artifact.ArtifactID).ToArray();

				MethodInfo method = GetType().GetMethod("GetDTOs", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(new Type[] { childType });

				var allObjects = method.Invoke(this, new object[] { childArtifactIds, depthLevel }) as IEnumerable;

				var listType = typeof(List<>).MakeGenericType(theMultipleObjectAttribute.ChildType);
				IList returnList = (IList)Activator.CreateInstance(listType);

				foreach (var item in allObjects)
				{
					returnList.Add(item);
				}

				propertyInfo.SetValue(baseDto, returnList);
			}

			foreach (var ObjectPropertyInfo in BaseDto.GetRelativitySingleObjectPropertyInfos<T>())
			{
				var propertyInfo = ObjectPropertyInfo.Key;

				Type objectType = ObjectPropertyInfo.Value.ChildType;
				var singleObject = Activator.CreateInstance(objectType);

				int childArtifactId = objectRdo[ObjectPropertyInfo.Value.FieldGuid].ValueAsSingleObject.ArtifactID;

				MethodInfo method = GetType().GetMethod("GetDTO", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(new Type[] { objectType });

				if (childArtifactId != 0)
				{
					singleObject = method.Invoke(this, new object[] { childArtifactId, depthLevel });
				}

				propertyInfo.SetValue(baseDto, singleObject);
			}

			foreach (var childPropertyInfo in BaseDto.GetRelativityObjectChildrenListInfos<T>())
			{
				var propertyInfo = childPropertyInfo.Key;
				var theChildAttribute = childPropertyInfo.Value;

				Type childType = childPropertyInfo.Value.ChildType;
				MethodInfo method = GetType().GetMethod("GetAllChildDTOs", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(new Type[] { childType });

				Guid parentFieldGuid = childType.GetRelativityObjectGuidForParentField();

				var allChildObjects = method.Invoke(this, new object[] { parentFieldGuid, baseDto.ArtifactId, depthLevel }) as IEnumerable;

				var listType = typeof(List<>).MakeGenericType(theChildAttribute.ChildType);
				IList returnList = (IList)Activator.CreateInstance(listType);

				foreach (var item in allChildObjects)
				{
					returnList.Add(item);
				}

				propertyInfo.SetValue(baseDto, returnList);
			}

			foreach (var filePropertyInfo in baseDto.GetType().GetPublicProperties().Where(prop => prop.PropertyType == typeof(RelativityFile)))
			{
				var filePropertyValue = filePropertyInfo.GetValue(baseDto, null) as RelativityFile;

				if (filePropertyValue != null)
				{
					filePropertyValue = GetFile(filePropertyValue.ArtifactTypeId, baseDto.ArtifactId);
				}

				filePropertyInfo.SetValue(baseDto, filePropertyValue);
			}
		}

		public T GetRelativityObject<T>(int artifactId, ObjectFieldsDepthLevel depthLevel)
			where T : BaseDto, new()
		{
			RDO objectRdo = GetRdo(artifactId);

			T theObject = objectRdo.ToHydratedDto<T>();

			if (depthLevel != ObjectFieldsDepthLevel.OnlyParentObject)
			{
				PopulateChildrenRecursively<T>(theObject, objectRdo, depthLevel);
			}

			return theObject;
		}

		public ResultSet<Document> QueryDocumentsByDocumentViewID(int documentViewId)
		{
			ResultSet<Document> returnObject;

			Query<Document> query = new Query<Document>();
			query.Condition = new ViewCondition(documentViewId);
			query.Fields = FieldValue.SelectedFields;

			using (IRSAPIClient proxy = CreateProxy())
			{
				try
				{
					returnObject = invokeWithRetryService.InvokeWithRetry(() => proxy.Repositories.Document.Query(query));
				}
				catch (Exception ex)
				{
					throw new ProxyOperationFailedException("Failed in method: " + System.Reflection.MethodInfo.GetCurrentMethod(), ex);
				}
			}

			return returnObject;
		}

		public KeyValuePair<byte[], kCura.Relativity.Client.FileMetadata> DownloadDocumentNative(int documentId)
		{
			kCura.Relativity.Client.DTOs.Document doc = new kCura.Relativity.Client.DTOs.Document(documentId);
			byte[] documentBytes;

			KeyValuePair<DownloadResponse, Stream> documentNativeResponse = new KeyValuePair<DownloadResponse, Stream>();

			using (IRSAPIClient proxy = CreateProxy())
			{
				try
				{
					documentNativeResponse = invokeWithRetryService.InvokeWithRetry(() => proxy.Repositories.Document.DownloadNative(doc));
				}
				catch (Exception ex)
				{
					throw new ProxyOperationFailedException("Failed in method: " + System.Reflection.MethodInfo.GetCurrentMethod(), ex);
				}
			}

			using (MemoryStream ms = (MemoryStream)documentNativeResponse.Value)
			{
				documentBytes = ms.ToArray();
			}

			return new KeyValuePair<byte[], FileMetadata>(documentBytes, documentNativeResponse.Key.Metadata);
		}

	}
}
