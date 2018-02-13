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
			var propertyInfo = GetPropertyAttributes<T, RelativityObjectFieldParentArtifactIdAttribute>()
				.FirstOrDefault()?
				.Item1;

			return propertyInfo?.GetCustomAttribute<RelativityObjectFieldAttribute>().FieldGuid ?? new Guid();
		}

		public static Dictionary<PropertyInfo, RelativityObjectFieldAttribute> GetRelativityObjectFieldListInfos<T>()
		{
			return GetPropertyAttributes<T, RelativityObjectFieldAttribute>()
				.ToDictionary(x => x.Item1, x => x.Item2);
		}


		public static Dictionary<PropertyInfo, Type> GetRelativityObjectChildrenPropertyInfos<T>()
		{
			return GetPropertyAttributes<T, RelativityObjectChildrenListAttribute>()
				.ToDictionary(x => x.Item1, x => x.Item2.ChildType);
		}

		public static Dictionary<PropertyInfo, RelativityMultipleObjectAttribute> GetRelativityMultipleObjectPropertyInfos<T>()
		{
			return GetPropertyAttributes<T, RelativityMultipleObjectAttribute>()
				.ToDictionary(x => x.Item1, x => x.Item2);
		}

		public static Dictionary<PropertyInfo, RelativitySingleObjectAttribute> GetRelativitySingleObjectPropertyInfos<T>()
		{
			return GetPropertyAttributes<T, RelativitySingleObjectAttribute>()
				.ToDictionary(x => x.Item1, x => x.Item2);
		}

		public static Dictionary<PropertyInfo, RelativityObjectChildrenListAttribute> GetRelativityObjectChildrenListInfos<T>()
		{
			return GetPropertyAttributes<T, RelativityObjectChildrenListAttribute>()
				.ToDictionary(x => x.Item1, x => x.Item2);
		}

		public object GetPropertyValue(string propertyName)
		{
			return this.GetType().GetProperty(propertyName).GetValue(this, null);
		}

		// TODO: Re-work this one to accept selector for the property, not ugly string propertyName
		public static Guid GetRelativityFieldGuidOfProperty<T>(string propertyName)
		{

			var fieldAttribute = typeof(T).GetPublicProperties()
				.FirstOrDefault(property => property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))?
				.GetCustomAttribute<RelativityObjectFieldAttribute>();
			

			return fieldAttribute?.FieldGuid ?? new Guid();

		}

		public PropertyInfo GetParentArtifactIdProperty()
		{
			return GetPropertyAttributes<RelativityObjectFieldParentArtifactIdAttribute>(this.GetType())
				.FirstOrDefault()?
				.Item1;
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

			var parentId = this.GetParentArtifactIdProperty()?.GetValue(this, null);
			if (parentId != null)
			{
				rdo.ParentArtifact = new Artifact((int)parentId);
			}

			foreach (PropertyInfo property in this.GetType().GetPublicProperties())
			{
				object theFieldValue = null;

				RelativityObjectFieldAttribute fieldAttribute = property.GetCustomAttribute<RelativityObjectFieldAttribute>();

				if (fieldAttribute == null)
				{
					continue;
				}

				if (fieldAttribute.FieldType == (int)RdoFieldType.File)
				{
					continue;
				}

				object propertyValue = property.GetValue(this);
				if (propertyValue == null)
				{
					continue;
				}

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
							multiChoiceFieldValueList.Add(new Choice(relativityObjectAttribute.ObjectTypeGuid));
						}

						theFieldValue = multiChoiceFieldValueList;
						break;
					case (int)RdoFieldType.MultipleObject:
						var listOfObjects = new FieldValueList<Artifact>();

						foreach (int artifactId in (IList<int>)propertyValue)
						{
							listOfObjects.Add(new Artifact(artifactId));
						}

						theFieldValue = listOfObjects;
						break;
					case (int)RdoFieldType.SingleChoice:

						bool isEnumDefined = Enum.IsDefined(propertyValue.GetType(), propertyValue);

						if (isEnumDefined == true)
						{
							var choiceGuid = propertyValue.GetType().GetMember(propertyValue.ToString())[0].GetCustomAttribute<RelativityObjectAttribute>().ObjectTypeGuid;
							theFieldValue = new Choice(choiceGuid);
						}
						break;
					case (int)RdoFieldType.SingleObject:
						if ((int)propertyValue > 0)
						{
							theFieldValue = new Artifact((int)propertyValue);
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
							theFieldValue = artifactId == 0 ? null : new Artifact(artifactId);
							rdo.Fields.Add(new FieldValue(singleObjectAttribute.FieldGuid, theFieldValue));
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
							var listOfObjects = new FieldValueList<Artifact>();

							foreach (var objectValue in propertyValue as IList)
							{
								int artifactId = (int)objectValue.GetType().GetProperty("ArtifactId").GetValue(objectValue, null);

								listOfObjects.Add(new Artifact(artifactId));
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

		private static IEnumerable<Tuple<PropertyInfo, A>> GetPropertyAttributes<T, A>() where A : Attribute
	=> GetPropertyAttributes<A>(typeof(T));

		private static IEnumerable<Tuple<PropertyInfo, A>> GetPropertyAttributes<A>(Type type) where A : Attribute
		{
			return type.GetPublicProperties()
				.Select(p => new Tuple<PropertyInfo, A>(p, p.GetCustomAttribute<A>()))
				.Where(kvp => kvp.Item2 != null);
		}
	}
}
