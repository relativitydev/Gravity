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
			return InvokeProxyWithRetry(proxyToWorkspace => proxyToWorkspace.Repositories.RDO.ReadSingle(artifactId));
		}

		protected List<RDO> GetRdos(int[] artifactIds)
		{
			return InvokeProxyWithRetry(proxyToWorkspace => proxyToWorkspace.Repositories.RDO.Read(artifactIds))
				.GetResultData();
		}

		protected IEnumerable<RDO> GetRdos<T>(Condition queryCondition = null)
			where T : BaseDto
		{
			Query<RDO> query = new Query<RDO>()
			{
				ArtifactTypeGuid = BaseDto.GetObjectTypeGuid<T>(),
				Condition = queryCondition,
				Fields = FieldValue.AllFields
			};

			return QueryWithPaging(query).SelectMany(x => x.GetResultData());
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

				var fileData = InvokeProxyWithRetry(proxyToWorkspace, proxy => proxy.Download(fileRequest));

				using (MemoryStream ms = (MemoryStream)fileData.Value)
				{
					FileValue fileValue = new FileValue(null, ms.ToArray());
					FileMetadata fileMetadata = fileData.Key.Metadata;

					return new RelativityFile(fileFieldArtifactId, fileValue, fileMetadata);
				}
			}
		}

		#endregion

		public IEnumerable<T> GetAllDTOs<T>(Condition queryCondition = null, ObjectFieldsDepthLevel depthLevel = ObjectFieldsDepthLevel.FirstLevelOnly)
			where T : BaseDto, new()
		{
			IEnumerable<RDO> objectsRdos = GetRdos<T>(queryCondition);
			return objectsRdos.Select(rdo => GetHydratedDTO<T>(rdo, depthLevel));
		}

		public IEnumerable<T> GetAllChildDTOs<T>(Guid parentFieldGuid, int parentArtifactID, ObjectFieldsDepthLevel depthLevel)
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

				var allObjects = this.InvokeGenericMethod(childType, nameof(GetDTOs), childArtifactIds, depthLevel) as IEnumerable;

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
					singleObject = this.InvokeGenericMethod(objectType, nameof(GetRelativityObject), childArtifactId, depthLevel );
				}

				propertyInfo.SetValue(baseDto, singleObject);
			}

			foreach (var childPropertyInfo in BaseDto.GetRelativityObjectChildrenListInfos<T>())
			{
				var propertyInfo = childPropertyInfo.Key;
				var theChildAttribute = childPropertyInfo.Value;

				Type childType = childPropertyInfo.Value.ChildType;
				Guid parentFieldGuid = childType.GetRelativityObjectGuidForParentField();

				var allChildObjects = this.InvokeGenericMethod(childType, nameof(GetAllChildDTOs), parentFieldGuid, baseDto.ArtifactId, depthLevel) as IEnumerable;

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

		private static IList MakeGenericList(IEnumerable items, Type type)
		{
			var listType = typeof(List<>).MakeGenericType(type);
			IList returnList = (IList)Activator.CreateInstance(listType, items);
			return returnList;
		}

		/// <summary>
		/// Runs an RSAPI Query in pages.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <returns>An enumerable that yields each batch result set</returns>
		private IEnumerable<QueryResultSet<RDO>> QueryWithPaging(Query<RDO> query)
		{
			using (var proxyToWorkspace = CreateProxy())
			{ 
				var initialResultSet = InvokeProxyWithRetry(proxyToWorkspace, proxy => proxy.Repositories.RDO.Query(query));
				yield return initialResultSet;

				string queryToken = initialResultSet.QueryToken;

				// Iterate though all remaining pages 
				var totalCount = initialResultSet.TotalCount;
				int currentPosition = BatchSize + 1;

				while (currentPosition <= totalCount)
				{
					yield return InvokeProxyWithRetry(proxyToWorkspace, 
						proxy => proxy.Repositories.RDO.QuerySubset(queryToken, currentPosition, BatchSize));
					currentPosition += BatchSize;
				}
			}
		}
	}
}
