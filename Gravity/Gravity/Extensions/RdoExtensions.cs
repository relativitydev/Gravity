﻿using kCura.Relativity.Client.DTOs;
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

			foreach (PropertyInfo property in typeof(T).GetPublicProperties())
			{
				RelativityObjectFieldAttribute fieldAttribute = property.GetCustomAttribute<RelativityObjectFieldAttribute>();
				object newValueObject = null;
				if (fieldAttribute != null)
				{
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

								//get a List<target_enum_type> to hold your converted values
								var genericListType = typeof(List<>).MakeGenericType(fieldAttribute.ObjectFieldDTOType);
								var listOfEnumValuesInstance = (IList)Activator.CreateInstance(genericListType);

								//get choice names
								var choiceNames = new HashSet<string>(
									valueAsMultipleChoice.Select(c => c.Name.ChoiceTrim()),
									StringComparer.InvariantCultureIgnoreCase);

								//get enum values of type that correspond to those names
								var enumValues = Enum.GetValues(fieldAttribute.ObjectFieldDTOType).Cast<Enum>()
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

								newValueObject = Enum.GetValues(fieldAttribute.ObjectFieldDTOType)
									.Cast<object>()
									.SingleOrDefault(x => x.ToString().Equals(choiceNameTrimmed, StringComparison.OrdinalIgnoreCase));
							}
							break;
						case RdoFieldType.User:
							if (theFieldValue.Value != null && property.PropertyType == typeof(User))
							{
								newValueObject = theFieldValue.Value as User;
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
			}

			return returnDto;
		}
	}
}
