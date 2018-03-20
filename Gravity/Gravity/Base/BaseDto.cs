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

		public static Dictionary<PropertyInfo, RelativityObjectChildrenListAttribute> GetRelativityObjectChildrenListInfos<T>()
		{
			return GetPropertyAttributes<T, RelativityObjectChildrenListAttribute>()
				.ToDictionary(x => x.Item1, x => x.Item2);
		}

		public object GetPropertyValue(string propertyName)
		{
			return this.GetType().GetProperty(propertyName).GetValue(this, null);
		}

        public PropertyInfo GetParentArtifactIdProperty()
		{
			return GetPropertyAttributes<RelativityObjectFieldParentArtifactIdAttribute>(this.GetType())
				.FirstOrDefault()?
				.Item1;
		}

		// BE CAREFUL!
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


				object propertyValue = property.GetValue(this);
				if (propertyValue == null)
				{
					continue;
				}

				if (TryAddSimplePropertyValue(rdo, property, propertyValue)) { continue; }
				if (TryAddObjectPropertyValue(rdo, property, propertyValue)) { continue; }
				if (TryAddMultipleObjectPropertyValue(rdo, property, propertyValue)) { continue; }
			}

			return rdo;
		}

		#region TryAddPropertyValue methods

		private bool TryAddSimplePropertyValue(RDO rdo, PropertyInfo property, object propertyValue)
		{
			RelativityObjectFieldAttribute fieldAttribute = property.GetCustomAttribute<RelativityObjectFieldAttribute>();

			if (fieldAttribute == null || fieldAttribute.FieldType == (int)RdoFieldType.File)
			{
				return false;
			}

			var relativityValue = ConvertPropertyValue(property, fieldAttribute.FieldType, propertyValue);

			rdo.Fields.Add(new FieldValue(fieldAttribute.FieldGuid, relativityValue));
			return true;
		}

		private bool TryAddObjectPropertyValue(RDO rdo, PropertyInfo property, object propertyValue)
		{
			var singleObjectAttributeGuid = property.GetCustomAttribute<RelativitySingleObjectAttribute>()?.FieldGuid;

			if (singleObjectAttributeGuid == null)
			{
				return false;
			}

			// skip if field already exists
			if (rdo.Fields.Any(c => c.Guids.Contains(singleObjectAttributeGuid.Value)))
			{
				return false;
			}

			//Note that this isn't recursive (only ArtifactIDs are set), because recursive inserts, etc. are handled separately anyways.
			int artifactId = (int)propertyValue.GetType().GetProperty(nameof(ArtifactId)).GetValue(propertyValue, null);
			var relativityValue = artifactId == 0 ? null : new Artifact(artifactId);

			rdo.Fields.Add(new FieldValue(singleObjectAttributeGuid.Value, relativityValue));
			return true;
		}

		private bool TryAddMultipleObjectPropertyValue(RDO rdo, PropertyInfo property, object propertyValue)
		{
			var multipleObjectAttributeGuid = property.GetCustomAttribute<RelativityMultipleObjectAttribute>()?.FieldGuid;

			if (multipleObjectAttributeGuid == null)
			{
				return false;
			}

			// skip if field already exists
			if (rdo.Fields.Any(c => c.Guids.Contains(multipleObjectAttributeGuid.Value)))
			{
				return false;
			}

			var enumerableOfObjects = ((IList)propertyValue)
				.Cast<object>()
				.Select(objectValue => (int)objectValue.GetType().GetProperty(nameof(ArtifactId)).GetValue(objectValue, null))
				.Select(artifactId => new Artifact(artifactId));
			var relativityValue = new FieldValueList<Artifact>(enumerableOfObjects);

			rdo.Fields.Add(new FieldValue(multipleObjectAttributeGuid.Value, relativityValue));
			return true;
		}

		private static object ConvertPropertyValue(PropertyInfo property, int fieldType, object propertyValue)
		{
			switch (fieldType)
			{
				case (int)RdoFieldType.Currency:
				case (int)RdoFieldType.Date:
				case (int)RdoFieldType.Decimal:
				case (int)RdoFieldType.Empty:
				case (int)RdoFieldType.LongText:
				case (int)RdoFieldType.WholeNumber:
				case (int)RdoFieldType.YesNo:
				case (int)RdoFieldType.User:
					{
						return propertyValue;
					}

				//truncate fixed-length text
				case (int)RdoFieldType.FixedLengthText:
					{
						int stringLength = property.GetCustomAttribute<RelativityObjectFieldAttribute>().Length ?? 3000;

						string theString = propertyValue as string;
						if (string.IsNullOrEmpty(theString) == false && theString.Length > stringLength)
						{
							theString = theString.Substring(0, (stringLength - 3)) + "...";
						}

						return theString;
					}

				case (int)RdoFieldType.MultipleChoice:
					{
						var multiChoiceFieldValueEnumerable = ((IEnumerable)propertyValue)
							.Cast<Enum>()
							.Select(x => x.GetRelativityObjectAttributeGuidValue())
							.Select(choiceGuid => new Choice(choiceGuid));

						return new MultiChoiceFieldValueList(multiChoiceFieldValueEnumerable);
					}

				case (int)RdoFieldType.MultipleObject:
					{
						return new FieldValueList<Artifact>(
							((IList<int>)propertyValue).Select(x => new Artifact(x)));
					}

				case (int)RdoFieldType.SingleChoice:
					{
						if (Enum.IsDefined(propertyValue.GetType(), propertyValue))
						{
							var choiceGuid = ((Enum)propertyValue).GetRelativityObjectAttributeGuidValue();
							return new Choice(choiceGuid);
						}
						break;
					}

				case (int)RdoFieldType.SingleObject:
					{
						if ((int)propertyValue > 0)
						{
							return new Artifact((int)propertyValue);
						}
						break;
					}

				case SharedConstants.FieldTypeCustomListInt:
					{
						return ((IList<int>)propertyValue).ToSeparatedString(SharedConstants.ListIntSeparatorChar);
					}

				case SharedConstants.FieldTypeByteArray:
					{
						return Convert.ToBase64String((byte[])propertyValue);
					}
			}

			return null;
		}

		#endregion

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
