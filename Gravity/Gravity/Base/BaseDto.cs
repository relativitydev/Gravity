using kCura.Relativity.Client.DTOs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using Gravity.Extensions;
using Gravity.Globals;

namespace Gravity.Base
{
	[Serializable]
	public abstract class BaseDto
	{
		public static Guid GetObjectTypeGuid<T>()
			where T : BaseDto
		{
			RelativityObjectAttribute attribute = typeof(T).GetCustomAttribute<RelativityObjectAttribute>(false);
			return attribute.ObjectTypeGuid;
		}

		public static Guid GetParentArtifactIdFieldGuid<T>()
		{
			Guid returnGuid = new Guid();

			foreach (var propertyInfo in typeof(T).GetPublicProperties())
			{
				bool isPropertyAParentIdField = propertyInfo.GetCustomAttribute<RelativityObjectFieldParentArtifactIdAttribute>() != null;
				if (isPropertyAParentIdField == true)
				{
					returnGuid = propertyInfo.GetCustomAttribute<RelativityObjectFieldAttribute>().FieldGuid;
					break;
				}
			}

			return returnGuid;
		}

		public static Dictionary<PropertyInfo, RelativityObjectFieldAttribute> GetRelativityObjectFieldListInfos<T>()
		{
			var returnDictionary = new Dictionary<PropertyInfo, RelativityObjectFieldAttribute>();

			foreach (var propertyInfo in typeof(T).GetPublicProperties())
			{
				var fieldAttribute = propertyInfo.GetCustomAttribute<RelativityObjectFieldAttribute>();
				if (fieldAttribute != null)
				{
					returnDictionary.Add(propertyInfo, fieldAttribute);
				}
			}

			return returnDictionary;
		}

		public static Dictionary<PropertyInfo, Type> GetRelativityObjectChildrenPropertyInfos<T>()
		{
			Dictionary<PropertyInfo, Type> returnDictionary = new Dictionary<PropertyInfo, Type>();

			foreach (var propertyInfo in typeof(T).GetPublicProperties())
			{
				RelativityObjectChildrenListAttribute childrenAttibute = propertyInfo.GetCustomAttribute<RelativityObjectChildrenListAttribute>();
				if (childrenAttibute != null)
				{
					returnDictionary.Add(propertyInfo, childrenAttibute.ChildType);
				}
			}

			return returnDictionary;
		}

		public static Dictionary<PropertyInfo, RelativityMultipleObjectAttribute> GetRelativityMultipleObjectPropertyInfos<T>()
		{
			var returnDictionary = new Dictionary<PropertyInfo, RelativityMultipleObjectAttribute>();

			foreach (var propertyInfo in typeof(T).GetPublicProperties())
			{
				RelativityMultipleObjectAttribute childAttibute = propertyInfo.GetCustomAttribute<RelativityMultipleObjectAttribute>();
				if (childAttibute != null)
				{
					returnDictionary.Add(propertyInfo, childAttibute);
				}
			}

			return returnDictionary;
		}

		public static Dictionary<PropertyInfo, RelativitySingleObjectAttribute> GetRelativitySingleObjectPropertyInfos<T>()
		{
			var returnDictionary = new Dictionary<PropertyInfo, RelativitySingleObjectAttribute>();

			foreach (var propertyInfo in typeof(T).GetPublicProperties())
			{
				RelativitySingleObjectAttribute childAttibute = propertyInfo.GetCustomAttribute<RelativitySingleObjectAttribute>();
				if (childAttibute != null)
				{
					returnDictionary.Add(propertyInfo, childAttibute);
				}
			}

			return returnDictionary;
		}

		public static Dictionary<PropertyInfo, RelativityObjectChildrenListAttribute> GetRelativityObjectChildrenListInfos<T>()
		{
			var returnDictionary = new Dictionary<PropertyInfo, RelativityObjectChildrenListAttribute>();

			foreach (var propertyInfo in typeof(T).GetPublicProperties())
			{
				RelativityObjectChildrenListAttribute childAttibute = propertyInfo.GetCustomAttribute<RelativityObjectChildrenListAttribute>();
				if (childAttibute != null)
				{
					returnDictionary.Add(propertyInfo, childAttibute);
				}
			}

			return returnDictionary;
		}

		public object GetPropertyValue(string propertyName)
		{
			return this.GetType().GetProperty(propertyName).GetValue(this, null);
		}

		// TODO: Re-work this one to accept selector for the property, not ugly string propertyName
		public static Guid GetRelativityFieldGuidOfProperty<T>(string propertyName)
		{
			Guid returnGuid = new Guid();

			var theProperty = typeof(T).GetPublicProperties().FirstOrDefault(property => property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
			if (theProperty != null)
			{
				var fieldAttribute = theProperty.GetCustomAttribute<RelativityObjectFieldAttribute>();
				if (fieldAttribute != null)
				{
					returnGuid = fieldAttribute.FieldGuid;
				}
			}

			return returnGuid;
		}

		public PropertyInfo GetParentArtifactIdProperty()
		{
			PropertyInfo returnPropertyInfo = null;

			foreach (var propertyInfo in this.GetType().GetPublicProperties())
			{
				bool isPropertyAParentIdField = propertyInfo.GetCustomAttribute<RelativityObjectFieldParentArtifactIdAttribute>() != null;
				if (isPropertyAParentIdField == true)
				{
					returnPropertyInfo = propertyInfo;
					break;
				}
			}

			return returnPropertyInfo;
		}

		// BE CAREFULL!
		// This is the Artifact ID of the DTO within the working Workspace (where app is installed, usually)
		// If you need the global Artifact ID (for Group, User, Client, etc)
		// If you need that one, you need to use MasterArtifactID from inheriting class BaseMasterDto
		public int ArtifactId { get; set; }

		public abstract string Name { get; set; }

		protected BaseDto()
		{
		}

		public RDO ToRdo()
		{
			RelativityObjectAttribute objectTypeAttribute = this.GetType().GetCustomAttribute<RelativityObjectAttribute>(false);
			RDO rdo = new RDO(objectTypeAttribute.ObjectTypeGuid, ArtifactId);

			var parentProperty = this.GetParentArtifactIdProperty();
			if (parentProperty != null)
			{
				var parentId = parentProperty.GetValue(this, null);
				if (parentId != null)
				{
					rdo.ParentArtifact = new kCura.Relativity.Client.DTOs.Artifact((int)parentId);
				}
			}

			foreach (PropertyInfo property in this.GetType().GetPublicProperties())
			{
				object theFieldValue = null;

				RelativityObjectFieldAttribute fieldAttribute = property.GetCustomAttribute<RelativityObjectFieldAttribute>();

				if (fieldAttribute != null)
				{
					object propertyValue = property.GetValue(this);
					if (propertyValue != null && fieldAttribute.FieldType != (int)RdoFieldType.File)
					{
						switch (fieldAttribute.FieldType)
						{
							case (int)RdoFieldType.Currency:
							case (int)RdoFieldType.Date:
							case (int)RdoFieldType.Decimal:
							case (int)RdoFieldType.Empty:
							case (int)RdoFieldType.LongText:
							case (int)RdoFieldType.WholeNumber:
							case (int)RdoFieldType.YesNo:
							case (int)RdoFieldType.User:
								theFieldValue = propertyValue;
								break;
							case (int)RdoFieldType.FixedLengthText:
								int stringLenght;
								stringLenght = property.GetCustomAttribute<RelativityObjectFieldAttribute>().Length != null ?
									property.GetCustomAttribute<RelativityObjectFieldAttribute>().Length.Value :
									3000;

								string theString = propertyValue as string;
								if (string.IsNullOrEmpty(theString) == false && theString.Length > stringLenght)
								{
									theString = theString.Substring(0, (stringLenght - 3));
									theString += "...";
								}

								theFieldValue = theString;
								break;
							case (int)RdoFieldType.MultipleChoice:
								// We have IList<Enum> values here
								var multiChoiceFieldValueList = new MultiChoiceFieldValueList();

								IEnumerable enumEnumerable = propertyValue as IEnumerable;
								Type entryType = enumEnumerable.AsQueryable().ElementType;

								var enumValues = Enum.GetValues(entryType);
								foreach (var enumValueObject in enumEnumerable)
								{
									var memberInfo = entryType.GetMember(enumValueObject.ToString());
									var relativityObjectAttribute = memberInfo[0].GetCustomAttribute<RelativityObjectAttribute>();
									multiChoiceFieldValueList.Add(new kCura.Relativity.Client.DTOs.Choice(relativityObjectAttribute.ObjectTypeGuid));
								}

								theFieldValue = multiChoiceFieldValueList;
								break;
							case (int)RdoFieldType.MultipleObject:
								var listOfObjects = new FieldValueList<kCura.Relativity.Client.DTOs.Artifact>();

								foreach (int artifactId in (IList<int>)propertyValue)
								{
									listOfObjects.Add(new kCura.Relativity.Client.DTOs.Artifact(artifactId));
								}

								theFieldValue = listOfObjects;
								break;
							case (int)RdoFieldType.SingleChoice:

								bool isEnumDefined = Enum.IsDefined(propertyValue.GetType(), propertyValue);

								if (isEnumDefined == true)
								{
									var choiceGuid = propertyValue.GetType().GetMember(propertyValue.ToString())[0].GetCustomAttribute<RelativityObjectAttribute>().ObjectTypeGuid;
									theFieldValue = new kCura.Relativity.Client.DTOs.Choice(choiceGuid);
								}
								break;
							case (int)RdoFieldType.SingleObject:
								if ((int)propertyValue > 0)
								{
									theFieldValue = new kCura.Relativity.Client.DTOs.Artifact((int)propertyValue);
								}
								break;
							case SharedConstants.FieldTypeCustomListInt:
								theFieldValue = ((IList<int>)propertyValue).ToSeparatedString(SharedConstants.ListIntSeparatorChar);
								break;
							case SharedConstants.FieldTypeByteArray:
								theFieldValue = Convert.ToBase64String((byte[])propertyValue);
								break;
						}

						rdo.Fields.Add(new FieldValue(fieldAttribute.FieldGuid, theFieldValue));
					}
				}
			}

			foreach (PropertyInfo property in this.GetType().GetPublicProperties())
			{
				object theFieldValue = null;
				RelativitySingleObjectAttribute singleObjectAttribute = property.GetCustomAttribute<RelativitySingleObjectAttribute>();
				RelativityMultipleObjectAttribute multipleObjectAttribute = property.GetCustomAttribute<RelativityMultipleObjectAttribute>();

				if (singleObjectAttribute != null)
				{
					int fieldsWithSameGuid = rdo.Fields.Where(c => c.Guids.Contains(singleObjectAttribute.FieldGuid)).Count();

					if (fieldsWithSameGuid == 0)
					{
						object propertyValue = property.GetValue(this);
						if (propertyValue != null)
						{
							int artifactId = (int)propertyValue.GetType().GetProperty("ArtifactId").GetValue(propertyValue, null);
							if (artifactId != 0)
							{
								theFieldValue = new kCura.Relativity.Client.DTOs.Artifact(artifactId);
								rdo.Fields.Add(new FieldValue(singleObjectAttribute.FieldGuid, theFieldValue));
							}
							else
							{
								theFieldValue = null;
								rdo.Fields.Add(new FieldValue(singleObjectAttribute.FieldGuid, theFieldValue));
							}
						}
					}

				}

				if (multipleObjectAttribute != null)
				{
					int fieldsWithSameGuid = rdo.Fields.Where(c => c.Guids.Contains(multipleObjectAttribute.FieldGuid)).Count();

					if (fieldsWithSameGuid == 0)
					{
						object propertyValue = property.GetValue(this);
						if (propertyValue != null)
						{
							var listOfObjects = new FieldValueList<kCura.Relativity.Client.DTOs.Artifact>();

							foreach (var objectValue in propertyValue as IList)
							{
								int artifactId = (int)objectValue.GetType().GetProperty("ArtifactId").GetValue(objectValue, null);

								listOfObjects.Add(new kCura.Relativity.Client.DTOs.Artifact(artifactId));
							}

							theFieldValue = listOfObjects;
							rdo.Fields.Add(new FieldValue(multipleObjectAttribute.FieldGuid, theFieldValue));
						}
					}
				}
			}

			return rdo;
		}

		public T DeepClone<T>()
		{
			using (MemoryStream stream = new MemoryStream())
			{
				BinaryFormatter formatter = new BinaryFormatter();
				formatter.Serialize(stream, this);
				stream.Position = 0;
				return (T)formatter.Deserialize(stream);
			}
		}
	}
}
