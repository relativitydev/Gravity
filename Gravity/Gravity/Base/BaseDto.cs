﻿using kCura.Relativity.Client.DTOs;
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

		public static List<PropertyInfo> GetRelativityObjectChildrenListProperties<T>()
		{
			return GetPropertyAttributes<T, RelativityObjectChildrenListAttribute>().Select(x => x.Item1).ToList();
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
			}

			return rdo;
		}

		#region TryAddPropertyValue methods

		private bool TryAddSimplePropertyValue(RDO rdo, PropertyInfo property, object propertyValue)
		{
			RelativityObjectFieldAttribute fieldAttribute = property.GetCustomAttribute<RelativityObjectFieldAttribute>();

			if (fieldAttribute == null || fieldAttribute.FieldType == RdoFieldType.File)
			{
				return false;
			}

			var relativityValue = ConvertPropertyValue(property, fieldAttribute.FieldType, propertyValue);

			rdo.Fields.Add(new FieldValue(fieldAttribute.FieldGuid, relativityValue));
			return true;
		}

		private static object ConvertPropertyValue(PropertyInfo property, RdoFieldType fieldType, object propertyValue)
		{
			switch (fieldType)
			{
				case RdoFieldType.Currency:
				case RdoFieldType.Date:
				case RdoFieldType.Decimal:
				case RdoFieldType.Empty:
				case RdoFieldType.LongText:
				case RdoFieldType.WholeNumber:
				case RdoFieldType.YesNo:
				case RdoFieldType.User:
					{
						return propertyValue;
					}

				//truncate fixed-length text
				case RdoFieldType.FixedLengthText:
					{
						int stringLength = property.GetCustomAttribute<RelativityObjectFieldAttribute>().Length ?? 3000;

						string theString = propertyValue as string;
						if (string.IsNullOrEmpty(theString) == false && theString.Length > stringLength)
						{
							theString = theString.Substring(0, (stringLength - 3)) + "...";
						}

						return theString;
					}

				case RdoFieldType.MultipleChoice:
					{
						var multiChoiceFieldValueEnumerable = ((IEnumerable)propertyValue)
							.Cast<Enum>()
							.Select(x => x.GetRelativityObjectAttributeGuidValue())
							.Select(choiceGuid => new Choice(choiceGuid));

						return new MultiChoiceFieldValueList(multiChoiceFieldValueEnumerable);
					}

				case RdoFieldType.MultipleObject:
					{
						return new FieldValueList<Artifact>(
							(
								(IEnumerable<object>) propertyValue).Select(x =>
								new Artifact(
									(int) x.GetType().GetProperty("ArtifactId").GetValue(x, null)
									)
								)
							);


					}

				case RdoFieldType.SingleChoice:
					{
						if (Enum.IsDefined(propertyValue.GetType(), propertyValue))
						{
							var choiceGuid = ((Enum)propertyValue).GetRelativityObjectAttributeGuidValue();
							return new Choice(choiceGuid);
						}
						break;
					}

				case RdoFieldType.SingleObject:
					{
						int artifactId = (int)propertyValue.GetType().GetProperty("ArtifactId")?.GetValue(propertyValue, null);
					
						if (artifactId > 0)
						{
							return new Artifact(artifactId);
						}
						break;
					}
			}

			return null;
		}

		#endregion

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
