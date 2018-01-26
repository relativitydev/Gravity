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
		protected WriteResultSet<RDO> UpdateRdos(params RDO[] rdos)
		{
			return InvokeProxyWithRetry(proxyToWorkspace => proxyToWorkspace.Repositories.RDO.Update(rdos));
		}

		protected void UpdateRdo(RDO theRdo)
		{
			InvokeProxyWithRetry(proxyToWorkspace => proxyToWorkspace.Repositories.RDO.UpdateSingle(theRdo));
		}
		#endregion

		internal void UpdateChildListObjects<T>(IList<T> objectsToUpdated, int parentArtifactId)
			where T : BaseDto, new()
		{
			Dictionary<PropertyInfo, RelativityObjectChildrenListAttribute> childObjectsInfo = BaseDto.GetRelativityObjectChildrenListInfos<T>();
			if (childObjectsInfo.Count == 0)
			{
				var objectsToBeInserted = objectsToUpdated.Where(o => o.ArtifactId == 0).ToList();

				if (objectsToBeInserted.Count != 0)
				{
					Type type = objectsToBeInserted[0].GetType();
					this.InvokeGenericMethod(type, nameof(InsertChildListObjects), objectsToBeInserted, parentArtifactId);
				}

				bool isFilePropertyPresent = typeof(T).GetProperties().ToList().Where(c => c.DeclaringType.IsAssignableFrom(typeof(RelativityFile))).Count() > 0;
				if (isFilePropertyPresent)
				{
					List<RDO> rdosToBeUpdated = new List<RDO>();
					foreach (var objectToBeUpdated in objectsToUpdated.Where(o => o.ArtifactId != 0))
					{
						rdosToBeUpdated.Add(objectToBeUpdated.ToRdo());
					}

					if (rdosToBeUpdated.Count != 0)
					{
						UpdateRdos(rdosToBeUpdated.ToArray());
					}
				}
				else
				{
					foreach (var objectToBeUpdated in objectsToUpdated.Where(o => o.ArtifactId != 0))
					{
						UpdateRdo(objectToBeUpdated.ToRdo());
						InsertUpdateFileField(objectToBeUpdated, objectToBeUpdated.ArtifactId);
					}
				}
			}
			else
			{
				var objectsToBeInserted = objectsToUpdated.Where(o => o.ArtifactId == 0).ToList();

				if (objectsToBeInserted.Count != 0)
				{
					Type type = objectsToBeInserted[0].GetType();
					this.InvokeGenericMethod(type, nameof(InsertChildListObjects), objectsToBeInserted, parentArtifactId);

				}

				foreach (var objectToBeUpdated in objectsToUpdated.Where(o => o.ArtifactId != 0))
				{
					UpdateRdo(objectToBeUpdated.ToRdo());
					InsertUpdateFileField(objectToBeUpdated, objectToBeUpdated.ArtifactId);

					foreach (var childPropertyInfo in childObjectsInfo)
					{
						var propertyInfo = childPropertyInfo.Key;
						var theChildAttribute = childPropertyInfo.Value;

						Type childType = childPropertyInfo.Value.ChildType;

						var childObjectsList = childPropertyInfo.Key.GetValue(objectToBeUpdated, null) as IList;

						if (childObjectsList != null && childObjectsList.Count != 0)
						{
							this.InvokeGenericMethod(childType, nameof(UpdateChildListObjects), childObjectsList, parentArtifactId);
						}
					}
				}
			}
		}

		public void UpdateRelativityObject<T>(BaseDto theObjectToUpdate)
			where T : BaseDto , new()
		{
			RDO rdo = theObjectToUpdate.ToRdo();

			UpdateRdo(rdo);
			InsertUpdateFileField(theObjectToUpdate, theObjectToUpdate.ArtifactId);

			Dictionary<PropertyInfo, RelativityObjectChildrenListAttribute> childObjectsInfo = BaseDto.GetRelativityObjectChildrenListInfos<T>();
			if (childObjectsInfo.Count != 0)
			{
				foreach (var childPropertyInfo in childObjectsInfo)
				{
					var propertyInfo = childPropertyInfo.Key;
					var theChildAttribute = childPropertyInfo.Value;

					Type childType = childPropertyInfo.Value.ChildType;

					var childObjectsList = childPropertyInfo.Key.GetValue(theObjectToUpdate, null) as IList;

					if (childObjectsList != null && childObjectsList.Count != 0)
					{
						this.InvokeGenericMethod(childType, nameof(UpdateChildListObjects), childObjectsList, theObjectToUpdate.ArtifactId);
					}
				}
			}
		}

		public void UpdateField<T>(int rdoID, Guid fieldGuid, object value)
			where T : BaseDto, new()
		{
			RDO theRdo = new RDO(rdoID);
			theRdo.ArtifactTypeGuids.Add(BaseDto.GetObjectTypeGuid<T>());

			Type fieldType = typeof(T).GetProperties().Where(p => p.GetFieldGuidValueFromAttribute() == fieldGuid).FirstOrDefault().PropertyType;

			if (fieldType.IsGenericType)
			{
				if (fieldType.GetGenericTypeDefinition() == typeof(IList<>))
				{
					if ((value as IList).HeuristicallyDetermineType().IsEnum)
					{
						MultiChoiceFieldValueList choices = new MultiChoiceFieldValueList();
						List<Guid> choiceValues = new List<Guid>();
						foreach (var enumValue in (value as IList))
						{
							choices.Add(new kCura.Relativity.Client.DTOs.Choice(((Enum)enumValue).GetRelativityObjectAttributeGuidValue()));
						}

						theRdo.Fields.Add(new FieldValue(fieldGuid, choices));
					}

					if (value.GetType().GetGenericArguments() != null && value.GetType().GetGenericArguments().Length != 0)
					{
						if (value.GetType().GetGenericArguments()[0].IsSubclassOf(typeof(BaseDto)))
						{
							var listOfObjects = new FieldValueList<kCura.Relativity.Client.DTOs.Artifact>();

							foreach (var objectValue in value as IList)
							{
								listOfObjects.Add(new kCura.Relativity.Client.DTOs.Artifact((int)objectValue.GetType().GetProperty("ArtifactId").GetValue(objectValue, null)));
							}

							theRdo.Fields.Add(new FieldValue(fieldGuid, listOfObjects));
						}

						if (value.GetType().GetGenericArguments()[0].IsEquivalentTo(typeof(int)))
						{
							var listOfObjects = new FieldValueList<kCura.Relativity.Client.DTOs.Artifact>();

							foreach (var objectValue in value as IList)
							{
								listOfObjects.Add(new kCura.Relativity.Client.DTOs.Artifact((int)objectValue));
							}

							theRdo.Fields.Add(new FieldValue(fieldGuid, listOfObjects));
						}
					}
				}
				else if (value == null)
				{
					theRdo.Fields.Add(new FieldValue(fieldGuid, value));
				}
				else if (value.GetType() == typeof(string) ||
					value.GetType() == typeof(int) ||
					value.GetType() == typeof(bool) ||
					value.GetType() == typeof(decimal) ||
					value.GetType() == typeof(DateTime))
				{
					theRdo.Fields.Add(new FieldValue(fieldGuid, value));
				}

				UpdateRdo(theRdo);
			}
			else
			{
				RelativityObjectFieldAttribute fieldAttributeValue = typeof(T).GetProperties().Where(p => p.GetFieldGuidValueFromAttribute() == fieldGuid).FirstOrDefault().GetCustomAttribute<RelativityObjectFieldAttribute>();

				if (fieldAttributeValue != null)
				{
					if (fieldAttributeValue.FieldType == (int)RdoFieldType.File)
					{
						if (value.GetType().BaseType != null)
						{
							if (value.GetType().BaseType.IsAssignableFrom(typeof(RelativityFile)))
							{
								InsertUpdateFileField(value as RelativityFile, rdoID);
							}
						}
					}

					if (fieldAttributeValue.FieldType == (int)RdoFieldType.User)
					{
						if (value.GetType() == typeof(User))
						{
							theRdo.Fields.Add(new FieldValue(fieldGuid, value));
							UpdateRdo(theRdo);
						}
					}

					if (value.GetType().IsEnum)
					{
						var choice = new kCura.Relativity.Client.DTOs.Choice(((Enum)value).GetRelativityObjectAttributeGuidValue());

						theRdo.Fields.Add(new FieldValue(fieldGuid, choice));
						UpdateRdo(theRdo);
					}

					if (value.GetType() == typeof(string) ||
							value.GetType() == typeof(int) ||
							value.GetType() == typeof(bool) ||
							value.GetType() == typeof(decimal) ||
							value.GetType() == typeof(DateTime))
					{
						theRdo.Fields.Add(new FieldValue(fieldGuid, value));
						UpdateRdo(theRdo);
					}
				}
			}
		}
	}
}
