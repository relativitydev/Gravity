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
					case RdoFieldType.File:
						if (theFieldValue.Value != null) // value is file name string
						{
							newValueObject = new RelativityFile(theFieldValue.ArtifactID);
						}
						break;
					case RdoFieldType.FixedLengthText:
						newValueObject = theFieldValue.ValueAsFixedLengthText;
						break;
					case RdoFieldType.LongText:
						newValueObject = theFieldValue.ValueAsLongText;
						break;
					case RdoFieldType.MultipleChoice:
						{
							var valueAsMultipleChoice = theFieldValue.ValueAsMultipleChoice;
							if (valueAsMultipleChoice == null)
								break;

							var enumType = property.PropertyType.GetEnumerableInnerType();

							//get a List<target_enum_type> to hold your converted values
							var genericListType = typeof(List<>).MakeGenericType(enumType);
							var listOfEnumValuesInstance = (IList)Activator.CreateInstance(genericListType);

							//get choice names
							var choiceNames = new HashSet<string>(
								valueAsMultipleChoice.Select(c => c.Name.ChoiceTrim()),
								StringComparer.InvariantCultureIgnoreCase);

							//get enum values of type that correspond to those names
							var enumValues = Enum.GetValues(enumType).Cast<Enum>()
								.Where(x => choiceNames.Contains(x.ToString()));

							//add to list
							foreach (var theValueObject in enumValues)
							{
								listOfEnumValuesInstance.Add(theValueObject);
							}

							//set to new object
							newValueObject = listOfEnumValuesInstance;
						}
						break;
					case RdoFieldType.SingleChoice:
						{

							string choiceNameTrimmed = theFieldValue?.ValueAsSingleChoice?.Name.ChoiceTrim();

							if (choiceNameTrimmed == null)
								break;

							newValueObject = Enum.GetValues(property.PropertyType)
								.Cast<object>()
								.SingleOrDefault(x => x.ToString().Equals(choiceNameTrimmed, StringComparison.OrdinalIgnoreCase));
						}
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
