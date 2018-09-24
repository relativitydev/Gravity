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
		private void UpdateSingleObjectFields<T>(IList<T> objectsToUpdate, bool recursive) where T : BaseDto
		{
			var singleObjectProperties =
				typeof(T).GetProperties()
				.Where(x => x.GetCustomAttribute<RelativityObjectFieldAttribute>()?.FieldType == RdoFieldType.SingleObject);

			foreach (var propertyInfo in singleObjectProperties)
			{
				var associatedObjectsToUpdate = objectsToUpdate
					.Select(objectToUpdate => (BaseDto)objectToUpdate.GetPropertyValue(propertyInfo.Name))
					.Where(x => x != null);

				var associatedType = propertyInfo.PropertyType;

				this.InvokeGenericMethod(associatedType, nameof(InsertOrUpdate), BaseExtensionMethods.MakeGenericList(associatedObjectsToUpdate, associatedType), recursive);
			}
		}

		private void UpdateMultipleObjectFields<T>(IList<T> objectsToUpdate, bool recursive) where T : BaseDto
		{
			var multipleObjectProperties =
				typeof(T).GetProperties()
				.Where(x => x.GetCustomAttribute<RelativityObjectFieldAttribute>()?.FieldType == RdoFieldType.MultipleObject);

			foreach (var propertyInfo in multipleObjectProperties)
			{
				var associatedObjectsToUpdate = objectsToUpdate
					.Select(objectToUpdate => (IEnumerable)objectToUpdate.GetPropertyValue(propertyInfo.Name))
					.Where(x => x != null)
					.SelectMany(x => x.Cast<BaseDto>());

				var associatedType = propertyInfo.PropertyType.GetEnumerableInnerType();

				this.InvokeGenericMethod(associatedType, nameof(InsertOrUpdate), BaseExtensionMethods.MakeGenericList(associatedObjectsToUpdate, associatedType), recursive);
			}
		}

		private void UpdateChildListObjects<T>(IList<T> objectsToUpdate, bool recursive) where T : BaseDto
		{
			var childProperties = typeof(T)
				.GetPropertyAttributeTuples<RelativityObjectChildrenListAttribute>()
				.Select(x => x.Item1);

			foreach (var childPropertyInfo in childProperties)
			{
				var childType = childPropertyInfo.PropertyType.GetEnumerableInnerType();
				var parentArtifactIdProperty = childType.GetPropertyAttributeTuples<RelativityObjectFieldParentArtifactIdAttribute>()
								.FirstOrDefault()?
								.Item1;

				IEnumerable<BaseDto> GetObjectsToUpdate(T theObjectToUpdate)
				{
					return ((IEnumerable)childPropertyInfo.GetValue(theObjectToUpdate))?
						.Cast<BaseDto>()
						.Select(childObject =>
						{
							var currentParentArtifactId = (int)parentArtifactIdProperty.GetValue(childObject);
							if (currentParentArtifactId == 0)
							{
								parentArtifactIdProperty.SetValue(childObject, theObjectToUpdate.ArtifactId);
							}
							else if (currentParentArtifactId != theObjectToUpdate.ArtifactId)
							{
								throw new InvalidOperationException("Cannot reassign the parent artifact ID of a child object");
							}
							return childObject;
						});
				}

				var childObjectsToUpdate = objectsToUpdate
					.Select(GetObjectsToUpdate)
					.Where(x => x != null)
					.SelectMany(x => x)
					.ToList();

				this.InvokeGenericMethod(childType, nameof(InsertOrUpdate), BaseExtensionMethods.MakeGenericList(childObjectsToUpdate, childType), recursive);
			
				var existingChildren = (List<int>)this.InvokeGenericMethod(
					childType, 
					nameof(GetAllChildIds), 
					objectsToUpdate.Select(x => x.ArtifactId).ToArray());

				//TODO: replace with bulk delete call
				foreach (var artifactId in existingChildren.Except(childObjectsToUpdate.Select(x => x.ArtifactId)))
				{
					this.InvokeGenericMethod(childType, nameof(Delete), new object[] {
						artifactId,
						recursive ? ObjectFieldsDepthLevel.FullyRecursive : ObjectFieldsDepthLevel.OnlyParentObject });
				}
			}
		}

		public void Update<T>(T theObjectToUpdate, ObjectFieldsDepthLevel depthLevel) where T : BaseDto
		{
			if (theObjectToUpdate.ArtifactId == 0)
				throw new ArgumentException("Artifact must have an ArtifactId.", nameof(theObjectToUpdate));

			Update<T>(new[] { theObjectToUpdate }, depthLevel);
		}

		public void Update<T>(IList<T> theObjectsToUpdate, ObjectFieldsDepthLevel depthLevel) where T : BaseDto
		{
			if (theObjectsToUpdate.Any(x => x.ArtifactId == 0))
				throw new ArgumentException("Artifacts must have an ArtifactId.", nameof(theObjectsToUpdate));

			var parentOnly = depthLevel == ObjectFieldsDepthLevel.OnlyParentObject;
			var recursive = depthLevel == ObjectFieldsDepthLevel.FullyRecursive;
			InsertOrUpdate(theObjectsToUpdate, parentOnly, recursive);
		}

		private void InsertOrUpdate<T>(IList<T> theObjectsToUpdate, bool recursive) where T : BaseDto
			=> InsertOrUpdate(theObjectsToUpdate, recursive, recursive);

		private void InsertOrUpdate<T>(IList<T> theObjectsToUpdate, bool parentOnly, bool recursive) where T : BaseDto
		{
			if (theObjectsToUpdate.Count == 0)
			{
				return;
			}

			if (!parentOnly)
			{
				UpdateSingleObjectFields(theObjectsToUpdate, recursive);
				UpdateMultipleObjectFields(theObjectsToUpdate, recursive);
			}

			var objectsToUpdateLookup = theObjectsToUpdate.ToLookup(x => x.ArtifactId != 0);
			var existingObjectsToUpdate = objectsToUpdateLookup[true].ToList();
			var newObjectsToUpdate = objectsToUpdateLookup[false].ToList();

			if (existingObjectsToUpdate.Any())
			{ 
				rsapiProvider.Update(existingObjectsToUpdate.Select(x => x.ToRdo(true)).ToList());
				InsertUpdateFileFields(existingObjectsToUpdate, false);
			}
			if (newObjectsToUpdate.Any())
			{
				ExecuteObjectInsert(newObjectsToUpdate);
				InsertUpdateFileFields(newObjectsToUpdate, true);
			}

			if (!parentOnly)
			{
				UpdateChildListObjects(theObjectsToUpdate, recursive);
			}
		}
	}
}
