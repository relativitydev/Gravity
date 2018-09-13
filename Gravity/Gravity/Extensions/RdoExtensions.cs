using kCura.Relativity.Client.DTOs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gravity.Base;
using Gravity.Globals;

namespace Gravity.Extensions
{
	public static class RdoExtensions
	{
		private static string ChoiceTrim(this string str)
			=> new[] { " ", "-", "(", ")" }.Aggregate(str, (s, c) => s.Replace(c, ""));

		public static T ToHydratedDto<T>(this RDO rdo)
			where T : BaseDto, new()
		{
			T returnDto = new T();
			returnDto.ArtifactId = rdo.ArtifactID;
			returnDto.GetParentArtifactIdProperty()?.SetValue(returnDto, rdo.ParentArtifact?.ArtifactID);

			foreach ((PropertyInfo property, RelativityObjectFieldAttribute fieldAttribute) 
				in typeof(T).GetPropertyAttributeTuples<RelativityObjectFieldAttribute>())
			{
				object newValueObject = null;
				FieldValue theFieldValue = rdo[fieldAttribute.FieldGuid];

				switch (fieldAttribute.FieldType)
				{
					case RdoFieldType.SingleObject:
					case RdoFieldType.MultipleObject:
						break;
					case RdoFieldType.Currency:
						newValueObject = theFieldValue.ValueAsCurrency;
						break;
					case RdoFieldType.Date:
						newValueObject = theFieldValue.ValueAsDate;
						break;
					case RdoFieldType.Decimal:
						newValueObject = theFieldValue.ValueAsDecimal;
						break;
					case RdoFieldType.Empty:
						newValueObject = null;
						break;
					case RdoFieldType.FixedLengthText:
						newValueObject = theFieldValue.ValueAsFixedLengthText;
						break;
					case RdoFieldType.LongText:
						newValueObject = theFieldValue.ValueAsLongText;
						break;
					case RdoFieldType.User:
						if (theFieldValue.Value is User user && property.PropertyType == typeof(User))
						{
							newValueObject = user;
						}
						break;
					case RdoFieldType.WholeNumber:
						newValueObject = theFieldValue.ValueAsWholeNumber;
						break;
					case RdoFieldType.YesNo:
						newValueObject = theFieldValue.ValueAsYesNo;
						break;
				}

				property.SetValue(returnDto, newValueObject);
			}

			return returnDto;
		}
	}
}
