using Gravity.Base;
using Gravity.Extensions;
using kCura.Relativity.Client.DTOs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Gravity.DAL.RSAPI
{
	public partial class RsapiDao
	{
		
		[Obsolete("This method will be replaced by a version that gets the field via lambda")]
		public void UpdateField<T>(int rdoID, Guid fieldGuid, object value)
				where T : BaseDto
		{
			PropertyInfo fieldProperty = typeof(T).GetProperties()
				.SingleOrDefault(p => p.GetCustomAttribute<RelativityObjectFieldAttribute>()?.FieldGuid == fieldGuid);
			if (fieldProperty == null)
				throw new InvalidOperationException($"Field not on type {typeof(T)}");


			object rdoValue;
			if (!TryGetRelativityFieldValue<T>(fieldProperty, value, out rdoValue))
				return;

			if (rdoValue is FileDto rdoValueFile)
			{
				InsertUpdateFileField(fieldGuid, rdoID, rdoValueFile);
				return;
			}

			RDO theRdo = new RDO(rdoID);
			theRdo.ArtifactTypeGuids.Add(BaseDto.GetObjectTypeGuid<T>());
			theRdo.Fields.Add(new FieldValue(fieldGuid, rdoValue));
			rsapiProvider.UpdateSingle(theRdo);
		}

		private static bool TryGetRelativityFieldValue<T>(PropertyInfo fieldProperty, object value, out object rdoValue)
			where T : BaseDto
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
							.Select(x => new Artifact((int)x.GetType().GetProperty(nameof(BaseDto.ArtifactId)).GetValue(x)))
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
				&& value.GetType().BaseType?.IsAssignableFrom(typeof(FileDto)) == true)
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
