using kCura.Relativity.Client.DTOs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gravity.Base;
using Gravity.Exceptions;
using Gravity.Extensions;

namespace Gravity.DAL.RSAPI
{
	public partial class RsapiDao
	{
		private void UpdateSingleObjectFields<T>(T theObjectToUpdate, bool recursive) where T : BaseDto
		{
			foreach (var propertyInfo in theObjectToUpdate.GetType().GetProperties())
			{
				var attribute = propertyInfo.GetCustomAttribute<RelativityObjectFieldAttribute>();
				if (attribute?.FieldType == RdoFieldType.SingleObject)
				{
					var fieldValue = (BaseDto)theObjectToUpdate.GetPropertyValue(propertyInfo.Name);
					if (fieldValue == null)
						continue;
					this.InvokeGenericMethod(propertyInfo.PropertyType, nameof(InsertOrUpdate), fieldValue, recursive);
				}
			}
		}

		private void UpdateMultipleObjectFields<T>(T theObjectToUpdate, bool recursive) where T : BaseDto
		{
			foreach (var propertyInfo in theObjectToUpdate.GetType().GetProperties().Where(c =>
				c.GetCustomAttribute<RelativityObjectFieldAttribute>()?.FieldType == RdoFieldType.MultipleObject))
			{
				var childType = propertyInfo.PropertyType.GetEnumerableInnerType();
				IEnumerable<object> fieldValue = (IEnumerable<object>)theObjectToUpdate.GetPropertyValue(propertyInfo.Name);
				if (fieldValue == null)
				{
					continue;
				}

				foreach (var childObject in fieldValue)
				{
					this.InvokeGenericMethod(childType, nameof(InsertOrUpdate), fieldValue, recursive);
				}
			}
		}

		private void UpdateChildListObjects<T>(T theObjectToUpdate, int resultArtifactId, bool recursive) where T : BaseDto
		{
			var childObjectsInfo = BaseDto.GetRelativityObjectChildrenListProperties<T>();
			foreach (var childPropertyInfo in childObjectsInfo)
			{
				var childType = childPropertyInfo.PropertyType.GetEnumerableInnerType();
				var childObjectsList = (childPropertyInfo.GetValue(theObjectToUpdate, null) as IList)?
						.Cast<BaseDto>().ToList() 
					?? new List<BaseDto>();

				//delete artifacts not present
				var artifactIdsToDelete = ((List<int>)this.InvokeGenericMethod(childType, nameof(GetAllChildIds), resultArtifactId))
					.Except(childObjectsList.Select(x => x.ArtifactId));

				foreach (var artifactId in artifactIdsToDelete)
				{
					this.InvokeGenericMethod(childType, nameof(Delete), artifactId, recursive);
				}

				foreach (var childObject in childObjectsList)
				{
					if (childObject.ArtifactId == 0)
					{
						var parentArtifactIdProperty = childObject.GetParentArtifactIdProperty();
						parentArtifactIdProperty.SetValue(childObject, resultArtifactId);
					}
					this.InvokeGenericMethod(childType, nameof(InsertOrUpdate), childObject, recursive);
				}
			}
		}

		public void Update<T>(T theObjectToUpdate, ObjectFieldsDepthLevel depthLevel) where T : BaseDto
		{
			if (theObjectToUpdate.ArtifactId == 0)
				throw new ArgumentException("Artifact must have an ArtifactId.", nameof(theObjectToUpdate));

			var parentOnly = depthLevel == ObjectFieldsDepthLevel.OnlyParentObject;
			var recursive = depthLevel == ObjectFieldsDepthLevel.FullyRecursive;
			InsertOrUpdate(theObjectToUpdate, parentOnly, recursive);
		}

		private void InsertOrUpdate<T>(T theObjectToUpdate, bool recursive) where T : BaseDto
			=> InsertOrUpdate(theObjectToUpdate, recursive, recursive);

		private void InsertOrUpdate<T>(T theObjectToUpdate, bool parentOnly, bool recursive) where T : BaseDto
		{
			if (!parentOnly)
			{
				UpdateSingleObjectFields(theObjectToUpdate, recursive);
				UpdateMultipleObjectFields(theObjectToUpdate, recursive);
			}

			if (theObjectToUpdate.ArtifactId == 0)
			{
				theObjectToUpdate.ArtifactId = rsapiProvider.CreateSingle(theObjectToUpdate.ToRdo());
			}
			else
			{
				rsapiProvider.UpdateSingle(theObjectToUpdate.ToRdo(true));
			}

			if (!parentOnly)
			{
				InsertUpdateFileFields(theObjectToUpdate, theObjectToUpdate.ArtifactId);
				UpdateChildListObjects(theObjectToUpdate, theObjectToUpdate.ArtifactId, recursive);
			}
		}
	}
}
