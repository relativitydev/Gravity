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
		#region UPDATE Protected Stuff
		protected void UpdateRdos(params RDO[] rdos)
		{
			rsapiProvider.Update(rdos).GetResultData();
		}

		protected void UpdateRdo(RDO theRdo)
		{
			rsapiProvider.UpdateSingle(theRdo);
		}

		//inserts *child* lists of a parent artifact ID (not associated artifacts)
		protected void UpdateChildListObjects<T>(IList<T> objectsToUpdate, int parentArtifactId)
			where T : BaseDto, new()
		{
			var objectsToBeInsertedLookup = objectsToUpdate.ToLookup(x => x.ArtifactId == 0);
			var objectsToBeInserted = objectsToBeInsertedLookup[true];
			var objectsToBeUpdated = objectsToBeInsertedLookup[false];

			//insert ones that do not exist
			if (objectsToBeInserted.Any())
			{
				this.InvokeGenericMethod(typeof(T), nameof(InsertChildListObjects), objectsToBeInserted, parentArtifactId);
			}

			if (!objectsToBeUpdated.Any())
				return;

			var childObjectsInfo = BaseDto.GetRelativityObjectChildrenListProperties<T>();

			//if do not have child objects in turn, we can take a shortcut
			//and batch update all the items at once
			if (childObjectsInfo.Count == 0)
			{
				//update RDOs in bulk
				UpdateRdos(objectsToBeUpdated.Select(x => x.ToRdo()).ToArray());

				//Cannot update files in bulk; do here
				if (typeof(T).GetProperties().ToList()
					.Any(c => c.DeclaringType.IsAssignableFrom(typeof(RelativityFile))))
				{
					foreach (var objectToBeUpdated in objectsToBeUpdated)
					{
						InsertUpdateFileFields(objectToBeUpdated, objectToBeUpdated.ArtifactId);
					}
				}

				return;
			}

			//if have child lists, recurse (UpdateRelativityObject and UpdateChildListObjects form a recursion)
			foreach (var objectToUpdate in objectsToBeUpdated)
			{
				UpdateRelativityObject(objectToUpdate, childObjectsInfo);
			}
		}

		#endregion

		public void UpdateRelativityObject<T>(BaseDto theObjectToUpdate)
			where T : BaseDto , new()
		{
			var childObjectsInfo = BaseDto.GetRelativityObjectChildrenListProperties<T>();
			UpdateRelativityObject(theObjectToUpdate, childObjectsInfo);
		}

		private void UpdateRelativityObject(BaseDto theObjectToUpdate, IEnumerable<PropertyInfo> childObjectsInfo)
		{
			//update root object
			UpdateRdo(theObjectToUpdate.ToRdo());

			//update files on object
			InsertUpdateFileFields(theObjectToUpdate, theObjectToUpdate.ArtifactId);

			//loop through each child object property
			foreach (var childPropertyInfo in childObjectsInfo)
			{
				var childObjectsList = childPropertyInfo.GetValue(theObjectToUpdate, null) as IList;

				if (childObjectsList != null && childObjectsList.Count > 0)
				{
					Type childType = childPropertyInfo.PropertyType.GetEnumerableInnerType();
					this.InvokeGenericMethod(childType, nameof(UpdateChildListObjects), childObjectsList, theObjectToUpdate.ArtifactId);
				}
			}
		}

		public void UpdateField<T>(int rdoID, Guid fieldGuid, object value)
			where T : BaseDto, new()
		{
			PropertyInfo fieldProperty = typeof(T).GetProperties()
				.SingleOrDefault(p => p.GetCustomAttribute<RelativityObjectFieldAttribute>()?.FieldGuid == fieldGuid);
			if (fieldProperty == null)
				throw new InvalidOperationException($"Field not on type {typeof(T)}");


			object rdoValue;
			if (!TryGetRelativityFieldValue<T>(fieldProperty, value, out rdoValue))
				return;

			if (rdoValue is RelativityFile rdoValueFile)
			{
				InsertUpdateFileField(rdoValueFile, rdoID);
				return;
			}

			RDO theRdo = new RDO(rdoID);
			theRdo.ArtifactTypeGuids.Add(BaseDto.GetObjectTypeGuid<T>());
			theRdo.Fields.Add(new FieldValue(fieldGuid, rdoValue));
			UpdateRdo(theRdo);
		}

		private static bool TryGetRelativityFieldValue<T>(PropertyInfo fieldProperty, object value, out object rdoValue)
			where T : BaseDto, new()
		{
			rdoValue = null;	

			Type fieldType = fieldProperty.PropertyType;

			if (fieldType.IsGenericType)
			{
				if (fieldType.GetGenericTypeDefinition() == typeof(IList<>))
				{
					var valueList = value as IList;
					if (valueList.HeuristicallyDetermineType().IsEnum)
					{
						var choices = valueList.Cast<Enum>()
							.Select(x => new Choice(x.GetRelativityObjectAttributeGuidValue()))
							.ToList();

						rdoValue = choices; return true;
					}

					var genericArg = value.GetType().GetGenericArguments().FirstOrDefault();

					if (genericArg?.IsSubclassOf(typeof(BaseDto)) == true)
					{
						rdoValue =
							valueList.Cast<object>()
							.Select(x => new Artifact((int)x.GetType().GetProperty(nameof(BaseDto.ArtifactId)).GetValue(x, null)))
							.ToList();

						return true;
					}

					if (genericArg?.IsEquivalentTo(typeof(int)) == true)
					{
						rdoValue = valueList.Cast<int>().Select(x => new Artifact(x)).ToList();
						return true;
					}
				}
				if (value == null)
				{
					return true;
				}
				if (value.GetType() == typeof(string) ||
					value.GetType() == typeof(int) ||
					value.GetType() == typeof(bool) ||
					value.GetType() == typeof(decimal) ||
					value.GetType() == typeof(DateTime))
				{
					rdoValue = value; return true;
				}

				return false;

			}

			RelativityObjectFieldAttribute fieldAttributeValue = fieldProperty.GetCustomAttribute<RelativityObjectFieldAttribute>();

			if (fieldAttributeValue == null)
			{
				return false;
			}

			if ((fieldAttributeValue.FieldType == RdoFieldType.File)
				&& value.GetType().BaseType?.IsAssignableFrom(typeof(RelativityFile)) == true)
			{
				rdoValue = value; return true;
			}

			if ((fieldAttributeValue.FieldType == RdoFieldType.User)
				&& (value.GetType() == typeof(User)))
			{
				rdoValue = value; return true;
			}

			if (value.GetType().IsEnum)
			{
				rdoValue = new Choice(((Enum)value).GetRelativityObjectAttributeGuidValue());
				return true;
			}

			if (value.GetType() == typeof(string) ||
				value.GetType() == typeof(int) ||
				value.GetType() == typeof(bool) ||
				value.GetType() == typeof(decimal) ||
				value.GetType() == typeof(DateTime))
			{
				rdoValue = value; return true;
			}

			return false;
		}

	}
}
