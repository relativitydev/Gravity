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
			return rsapiProvider.Read(artifactIds).GetResultData();
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

			return rsapiProvider.Query(query).SelectMany(x => x.GetResultData());
		}

		protected RelativityFile GetFile(int fileFieldArtifactId, int ourFileContainerInstanceArtifactId)
		{
			var fileData = rsapiProvider.DownloadFile(fileFieldArtifactId, ourFileContainerInstanceArtifactId);

			using (MemoryStream ms = (MemoryStream)fileData.Value)
			{
				FileValue fileValue = new FileValue(null, ms.ToArray());
				FileMetadata fileMetadata = fileData.Key.Metadata;

				return new RelativityFile(fileFieldArtifactId, fileValue, fileMetadata);
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
            
		    IEnumerable<PropertyInfo> singleObjectFields = typeof(T).GetPublicProperties().Where(x => x.GetCustomAttribute<RelativityObjectFieldAttribute>() != null && x.GetCustomAttribute<RelativityObjectFieldAttribute>().FieldType == RdoFieldType.SingleObject);

            foreach (PropertyInfo property in singleObjectFields)
            {
                RelativityObjectFieldAttribute fieldAttribute = property.GetCustomAttribute<RelativityObjectFieldAttribute>();
                object newValueObject = null;
                object newObject = null;

                if (fieldAttribute != null)
                {
                    FieldValue theFieldValue = objectRdo[fieldAttribute.FieldGuid];
                    if (theFieldValue != null)
                    {
                        switch (fieldAttribute.FieldType)
                        {
                            case RdoFieldType.SingleObject:
                                MethodInfo method = this.GetType().GetMethod("GetRelativityObject")
                                    .MakeGenericMethod(new Type[]
                                    {
                                        property.GetCustomAttribute<RelativityObjectFieldAttribute>().ObjectFieldDTOType
                                    });

                                switch (depthLevel)
                                {
                                    case ObjectFieldsDepthLevel.OnlyParentObject:
                                        //create new instance and only get artifact ID
                                        newObject = Activator.CreateInstance(property
                                            .GetCustomAttribute<RelativityObjectFieldAttribute>().ObjectFieldDTOType);
                                        (newObject as BaseDto).ArtifactId =
                                            theFieldValue.ValueAsSingleObject.ArtifactID;
                                        break;
                                    case ObjectFieldsDepthLevel.FirstLevelOnly:
                                        newObject = method.Invoke(this,
                                            new object[]
                                            {
                                                theFieldValue.ValueAsSingleObject.ArtifactID,
                                                ObjectFieldsDepthLevel.OnlyParentObject
                                            });
                                        break;
                                    case ObjectFieldsDepthLevel.FullyRecursive:
                                        newObject = method.Invoke(this,
                                            new object[] {theFieldValue.ValueAsSingleObject.ArtifactID, depthLevel});
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException(nameof(depthLevel));
                                }
                                property.SetValue(dto, newObject);
                                break;
                        }
                    }
                }
            }



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
			//multiple object
			var multiObjectAttribute = property.GetCustomAttribute<RelativityMultipleObjectAttribute>();
			if (multiObjectAttribute != null)
			{
				Type objectType = property.PropertyType.GetEnumerableInnerType();

				int[] childArtifactIds = objectRdo[multiObjectAttribute.FieldGuid]
					.GetValueAsMultipleObject<kCura.Relativity.Client.DTOs.Artifact>()
					.Select(artifact => artifact.ArtifactID)
					.ToArray();

				var allObjects = this.InvokeGenericMethod(objectType, nameof(GetDTOs), childArtifactIds, depthLevel) as IEnumerable;

				return MakeGenericList(allObjects, objectType);
			}

			//single object
			var singleObjectAttribute = property.GetCustomAttribute<RelativitySingleObjectAttribute>();
			if (singleObjectAttribute != null)
			{
				var objectType = property.PropertyType;
				int childArtifactId = objectRdo[singleObjectAttribute.FieldGuid].ValueAsSingleObject.ArtifactID;
				return childArtifactId == 0
					? Activator.CreateInstance(objectType)
					: this.InvokeGenericMethod(objectType, nameof(GetRelativityObject), childArtifactId, depthLevel);
			}

			//child object
			if (property.GetCustomAttribute<RelativityObjectChildrenListAttribute>() != null)
			{
				var childType = property.PropertyType.GetEnumerableInnerType();
				Guid parentFieldGuid = childType.GetRelativityObjectGuidForParentField();

				var allChildObjects = this.InvokeGenericMethod(childType, nameof(GetAllChildDTOs), parentFieldGuid, baseDto.ArtifactId, depthLevel) as IEnumerable;

				return MakeGenericList(allChildObjects, childType);
			}

			//file
			var relativityFile = property.GetValue(baseDto, null) as RelativityFile;
			if (relativityFile != null)
			{
				return GetFile(relativityFile.ArtifactTypeId, baseDto.ArtifactId);
			}

			return null;
		}

		private static IList MakeGenericList(IEnumerable items, Type type)
		{
			var listType = typeof(List<>).MakeGenericType(type);
			IList returnList = (IList)Activator.CreateInstance(listType, items);
			return returnList;
		}
	}
}
