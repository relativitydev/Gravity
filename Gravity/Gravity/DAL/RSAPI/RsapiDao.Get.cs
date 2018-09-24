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
			return objectsRdos.Select(rdo => GetHydratedDTO<T>(rdo, depthLevel));
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
			return objectsRdos.Select(rdo => GetHydratedDTO<T>(rdo, depthLevel)).ToList();
		}

		public T Get<T>(int artifactID, ObjectFieldsDepthLevel depthLevel)
			where T : BaseDto, new()
		{
			RDO objectRdo = GetRdo(artifactID);

			return GetHydratedDTO<T>(objectRdo, depthLevel);
		}

		private T GetHydratedDTO<T>(RDO objectRdo, ObjectFieldsDepthLevel depthLevel)
			where T : BaseDto, new()
		{
			T dto = objectRdo.ToHydratedDto<T>();
			PopulateChoices(dto, objectRdo);
			PopulateFiles(dto, objectRdo);
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


		internal void PopulateChildrenRecursively<T>(BaseDto baseDto, RDO objectRdo, ObjectFieldsDepthLevel depthLevel)
		{
			foreach (var objectPropertyInfo in baseDto.GetType().GetPublicProperties())
			{
				var childValue = GetChildObjectRecursively(baseDto, objectRdo, depthLevel, objectPropertyInfo);
				if (childValue != null)
				{
					objectPropertyInfo.SetValue(baseDto, childValue);
				}
			}
		}

		private object GetChildObjectRecursively(BaseDto baseDto, RDO objectRdo, ObjectFieldsDepthLevel depthLevel, PropertyInfo property)
		{
			var relativityObjectFieldAttibute = property.GetCustomAttribute<RelativityObjectFieldAttribute>();

			if (relativityObjectFieldAttibute != null)
			{
				var fieldType = relativityObjectFieldAttibute.FieldType;
				var fieldGuid = relativityObjectFieldAttibute.FieldGuid;

				//multiple object
				if (fieldType == RdoFieldType.MultipleObject)
				{
					Type objectType = property.PropertyType.GetEnumerableInnerType();

					int[] childArtifactIds = objectRdo[fieldGuid]
						.GetValueAsMultipleObject<kCura.Relativity.Client.DTOs.Artifact>()
						.Select(artifact => artifact.ArtifactID)
						.ToArray();

					var allObjects = this.InvokeGenericMethod(objectType, nameof(Get), childArtifactIds, depthLevel) as IEnumerable;

					return BaseExtensionMethods.MakeGenericList(allObjects, objectType);
				}

				//single object
				if (fieldType == RdoFieldType.SingleObject)
				{
					var childArtifact = objectRdo[fieldGuid].ValueAsSingleObject;
					if (childArtifact == null || childArtifact.ArtifactID == 0)
					{
						return null;
					}

					var objectType = property.PropertyType;
					var childArtifactId = childArtifact.ArtifactID;
					return this.InvokeGenericMethod(objectType, nameof(Get), childArtifactId, depthLevel);
				}

			}

			//child object
			if (property.GetCustomAttribute<RelativityObjectChildrenListAttribute>() != null)
			{
				var childType = property.PropertyType.GetEnumerableInnerType();

				var allChildObjects = this.InvokeGenericMethod(childType, nameof(GetAllChildDTOs), baseDto.ArtifactId, depthLevel) as IEnumerable;

				return BaseExtensionMethods.MakeGenericList(allChildObjects, childType);
			}

			
			return null;
		}
	}
}
