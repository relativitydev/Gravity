using kCura.Relativity.Client;
using kCura.Relativity.Client.DTOs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Gravity.Base;
using Gravity.Exceptions;
using Gravity.Extensions;
using Artifact = kCura.Relativity.Client.DTOs.Artifact;

namespace Gravity.DAL.RSAPI
{
	public partial class RsapiDao
	{
		#region RDO GET Protected stuff		
		protected RDO GetRdo(int artifactId)
		{
			return rsapiProvider.ReadSingle(artifactId);
		}

		protected List<RDO> GetRdos(int[] artifactIds)
		{
			return artifactIds.Any()
				? rsapiProvider.Read(artifactIds).GetResultData()
				: new List<RDO>();
		}

		protected IEnumerable<RDO> GetRdos<T>(Condition queryCondition = null)
			where T : BaseDto
		{
			Query<RDO> query = new Query<RDO>()
			{
				ArtifactTypeGuid = BaseDto.GetObjectTypeGuid<T>(),
				Condition = queryCondition,
				Fields = BaseDto.GetFieldsGuids<T>().Select(x => new FieldValue(x)).ToList()
			};

			return rsapiProvider.Query(query).SelectMany(x => x.GetResultData());
		}

		protected ByteArrayFileDto GetFile(Guid fieldGuid, int objectArtifactId)
		{
			//TODO: cache this?
			var fileFieldArtifactId = this.guidCache.Get(fieldGuid);

			(var fileMetadata, var fileStream) = rsapiProvider.DownloadFile(fileFieldArtifactId, objectArtifactId);

			using (fileStream)
			{
				var fileDto = new ByteArrayFileDto
				{
					ByteArray = fileStream.ToArray(),
					FileName = fileMetadata.FileName
				};
				fileMd5Cache.Set(fieldGuid, objectArtifactId, fileDto.GetMD5());
				return fileDto;
			}
		}

		#endregion

		public IEnumerable<T> Query<T>(Condition queryCondition = null, ObjectFieldsDepthLevel depthLevel = ObjectFieldsDepthLevel.FirstLevelOnly)
			where T : BaseDto, new()
		{
			IEnumerable<RDO> objectsRdos = GetRdos<T>(queryCondition);
			return GetHydratedDTOs<T>(objectsRdos.ToList(), depthLevel);
		}

		private List<int> GetAllChildIds<T>(params int[] parentArtifactIDs) where T : BaseDto
		{
			var parentFieldGuid = typeof(T)
				.GetPropertyAttributeTuples<RelativityObjectFieldParentArtifactIdAttribute>()
				.First().Item2.FieldGuid;

			Condition queryCondition = new WholeNumberCondition(parentFieldGuid, NumericConditionEnum.In, parentArtifactIDs);
			Query<RDO> query = new Query<RDO>()
			{
				ArtifactTypeGuid = BaseDto.GetObjectTypeGuid<T>(),
				Condition = queryCondition,
			};

			return rsapiProvider.Query(query).SelectMany(x => x.GetResultData()).Select(x => x.ArtifactID).ToList();
		}

		internal IEnumerable<T> GetAllChildDTOs<T>(int parentArtifactID, ObjectFieldsDepthLevel depthLevel)
			where T : BaseDto, new()
		{
			var parentFieldGuid = typeof(T)
				.GetPropertyAttributeTuples<RelativityObjectFieldParentArtifactIdAttribute>()
				.First().Item2.FieldGuid;

			return Query<T>(new WholeNumberCondition(parentFieldGuid, NumericConditionEnum.EqualTo, parentArtifactID), depthLevel);
		}

		public List<T> Get<T>(IList<int> artifactIDs, ObjectFieldsDepthLevel depthLevel)
			where T : BaseDto, new()
		{
			List<RDO> objectsRdos = GetRdos(artifactIDs.ToArray());
			return GetHydratedDTOs<T>(objectsRdos.ToList(), depthLevel);
		}

		public T Get<T>(int artifactID, ObjectFieldsDepthLevel depthLevel)
			where T : BaseDto, new()
		{
			return Get<T>(new[] { artifactID }, depthLevel).Single();
		}

		private List<T> GetHydratedDTOs<T>(List<RDO> objectRdos, ObjectFieldsDepthLevel depthLevel)
			where T : BaseDto, new()
		{

			var pairings = objectRdos.Select(objectRdo => {
				var dto = objectRdo.ToHydratedDto<T>();
				PopulateChoices(dto, objectRdo);
				PopulateFiles(dto, objectRdo);
				return Tuple.Create(dto, objectRdo);
			}).ToList();

			switch (depthLevel)
			{
				case ObjectFieldsDepthLevel.OnlyParentObject:
					break;
				case ObjectFieldsDepthLevel.FirstLevelOnly:
					PopulateChildrenRecursively(pairings, ObjectFieldsDepthLevel.OnlyParentObject);
					break;
				case ObjectFieldsDepthLevel.FullyRecursive:
					PopulateChildrenRecursively(pairings, ObjectFieldsDepthLevel.FullyRecursive);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(depthLevel));
			}

			return pairings.Select(x => x.Item1).ToList();
		}

		private void PopulateChoices(BaseDto dto, RDO objectRdo)
		{
			foreach ((PropertyInfo property, RelativityObjectFieldAttribute fieldAttribute) 
				in dto.GetType().GetPropertyAttributeTuples<RelativityObjectFieldAttribute>())
			{
				object GetEnum(Type enumType, int artifactId) => choiceCache.InvokeGenericMethod(enumType, nameof(ChoiceCache.GetEnum), artifactId);

				switch (fieldAttribute.FieldType)
				{
					case RdoFieldType.SingleChoice:
						{ 
							if (objectRdo[fieldAttribute.FieldGuid].ValueAsSingleChoice?.ArtifactID is int artifactId)
							{
								var enumType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
								property.SetValue(dto, GetEnum(enumType, artifactId));
							}
						}
						break;
					case RdoFieldType.MultipleChoice:
						{ 
							var enumType = property.PropertyType.GetEnumerableInnerType();
							var fieldValue = objectRdo[fieldAttribute.FieldGuid].ValueAsMultipleChoice?
								.Select(x => GetEnum(enumType, x.ArtifactID))
								.ToList();
							if (fieldValue != null)
							{ 
								property.SetValue(dto, BaseExtensionMethods.MakeGenericList(fieldValue, enumType));
							}
						}
						break;
					default:
						break;
				}
			}
		}

		private void PopulateFiles(BaseDto dto, RDO objectRdo)
		{
			foreach ((PropertyInfo property, RelativityObjectFieldAttribute fieldAttribute)
						in dto.GetType()
							.GetPropertyAttributeTuples<RelativityObjectFieldAttribute>()
							.Where(x => x.Item2.FieldType == RdoFieldType.File)
				)
			{
				if (objectRdo[fieldAttribute.FieldGuid].Value != null) // value is file name string, so if present will show up
				{
					property.SetValue(dto, this.GetFile(fieldAttribute.FieldGuid, objectRdo.ArtifactID));
				}
			}
		}

		internal void PopulateChildrenRecursively<T>(
				List<Tuple<T,RDO>> pairings, ObjectFieldsDepthLevel depthLevel)
			where T : BaseDto
		{
			var baseArtifactIds = pairings.Select(x => x.Item1.ArtifactId).ToList();
			var objectRdos = pairings.Select(x => x.Item2).ToList();
			
			foreach (var objectPropertyInfo in typeof(T).GetPublicProperties())
			{
				var childValues = GetAssociatedObjects(objectRdos, objectPropertyInfo, depthLevel) 
					?? GetChildObjects(baseArtifactIds, objectPropertyInfo, depthLevel);

				if (childValues == null)
				{
					continue;
				}
				
				foreach (var (baseDto, _) in pairings)
				{
					if (childValues.TryGetValue(baseDto.ArtifactId, out var val) && val != null)
					{
						objectPropertyInfo.SetValue(baseDto, val);
					}
				}
			}			
		}

		IDictionary<int, object> GetAssociatedObjects(List<RDO> objectRdos, PropertyInfo property, ObjectFieldsDepthLevel depthLevel)
		{
			var relativityObjectFieldAttibute = property.GetCustomAttribute<RelativityObjectFieldAttribute>();

			if (relativityObjectFieldAttibute?.FieldType == RdoFieldType.MultipleObject)
			{
				return GetMultipleObjects();
			}

			if (relativityObjectFieldAttibute?.FieldType == RdoFieldType.SingleObject)
			{
				return GetSingleObject();
			}

			return null;

			IDictionary<int, object> GetMultipleObjects()
			{
				Type objectType = property.PropertyType.GetEnumerableInnerType();

				var rootObjectsDict = objectRdos.ToDictionary(
					objectRdo => objectRdo.ArtifactID,
					objectRdo => objectRdo[relativityObjectFieldAttibute.FieldGuid]
						.GetValueAsMultipleObject<Artifact>()
						.Select(artifact => artifact.ArtifactID)
						.ToArray()
					);

				var distinctMultiObjIds = rootObjectsDict
					.SelectMany(x => x.Value)
					.ExceptSingle(0)
					.Distinct()
					.ToList();

				var multiObjDict = this.InvokeGenericMethod<IEnumerable>(
						objectType,
						nameof(Get),
						distinctMultiObjIds,
						depthLevel)
					.Cast<BaseDto>()
					.ToDictionary(x => x.ArtifactId);

				return rootObjectsDict.ToDictionary(
					x => x.Key,
					x => {
						var childObjects = x.Value.Select(y => multiObjDict[y]);
						return (object)BaseExtensionMethods.MakeGenericList(childObjects, objectType);
					}
				);
			}

			IDictionary<int, object> GetSingleObject()
			{
				var objectType = property.PropertyType;

				var rootObjectsDict = objectRdos.ToDictionary(
					objectRdo => objectRdo.ArtifactID,
					objectRdo =>
						objectRdo[relativityObjectFieldAttibute.FieldGuid].ValueAsSingleObject?.ArtifactID ?? 0);

				var distinctSingleObjs = rootObjectsDict.Select(x => x.Value).ExceptSingle(0).Distinct().ToList();

				var singleObjDict = this.InvokeGenericMethod<IEnumerable>(
						objectType,
						nameof(Get),
						distinctSingleObjs,
						depthLevel)
					.Cast<BaseDto>()
					.ToDictionary(x => x.ArtifactId);
				singleObjDict.Add(0, null);

				return rootObjectsDict.ToDictionary(
						x => x.Key,
						x => (object)singleObjDict[x.Value]);
			}
		}


		IDictionary<int, object> GetChildObjects(List<int> baseArtifactIds, PropertyInfo property, ObjectFieldsDepthLevel depthLevel)
		{
			if (property.GetCustomAttribute<RelativityObjectChildrenListAttribute>() == null)
			{
				return null;
			}

			var childType = property.PropertyType.GetEnumerableInnerType();

			var (parentProp, parentAttribute) = childType
				.GetPropertyAttributeTuples<RelativityObjectFieldParentArtifactIdAttribute>()
				.First();

			var parentCondition = new WholeNumberCondition(parentAttribute.FieldGuid, NumericConditionEnum.In, baseArtifactIds);

			return this.InvokeGenericMethod<IEnumerable>(
				childType, nameof(Query), parentCondition, depthLevel)
			.Cast<BaseDto>()
			.ToLookup(x => (int)parentProp.GetValue(x))
			.ToDictionary(
				x => x.Key,
				x => (object)BaseExtensionMethods.MakeGenericList(x, childType)
			);
		}
	}
}
