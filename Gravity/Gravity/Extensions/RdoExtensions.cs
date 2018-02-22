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

		// TODO: Scope a US to replace the usage of kCura.Relativity.Client.FieldType with our own enum for only our usages
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
						case (int)RdoFieldType.Currency:
							newValueObject = theFieldValue.ValueAsCurrency;
							break;
						case (int)RdoFieldType.Date:
							newValueObject = theFieldValue.ValueAsDate;
							break;
						case (int)RdoFieldType.Decimal:
							newValueObject = theFieldValue.ValueAsDecimal;
							break;
						case (int)RdoFieldType.Empty:
							newValueObject = null;
							break;
						case (int)RdoFieldType.File:
							if (theFieldValue.Value != null)
							{
								newValueObject = new RelativityFile(theFieldValue.ArtifactID);
							}
							break;
						case (int)RdoFieldType.FixedLengthText:
							newValueObject = theFieldValue.ValueAsFixedLengthText;
							break;
						case (int)RdoFieldType.LongText:
							newValueObject = theFieldValue.ValueAsLongText;
							break;
						case (int)RdoFieldType.MultipleChoice:
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
						case (int)RdoFieldType.MultipleObject:
							newValueObject = theFieldValue.GetValueAsMultipleObject<Artifact>()
								.Select(artifact => artifact.ArtifactID).ToList();
							break;
						case (int)RdoFieldType.SingleChoice:
							{

								string choiceNameTrimmed = theFieldValue.ValueAsSingleChoice.Name.ChoiceTrim();

								if (choiceNameTrimmed == null)
									break;

								newValueObject = Enum.GetValues(fieldAttribute.ObjectFieldDTOType)
									.Cast<object>()
									.FirstOrDefault(x => x.ToString().Equals(choiceNameTrimmed, StringComparison.OrdinalIgnoreCase));
							}
							break;
						case (int)RdoFieldType.SingleObject:
							if (theFieldValue?.ValueAsSingleObject?.ArtifactID > 0)
							{
								newValueObject = theFieldValue.ValueAsSingleObject.ArtifactID;
							}
							break;
						case (int)RdoFieldType.User:
							if (theFieldValue.Value != null && property.PropertyType == typeof(User))
							{
								newValueObject = theFieldValue.Value as User;
							}
							break;
						case (int)RdoFieldType.WholeNumber:
							newValueObject = theFieldValue.ValueAsWholeNumber;
							break;
						case (int)RdoFieldType.YesNo:
							newValueObject = theFieldValue.ValueAsYesNo;
							break;
						case SharedConstants.FieldTypeCustomListInt:
							newValueObject = theFieldValue.ValueAsLongText.ToListInt(SharedConstants.ListIntSeparatorChar);
							break;
						case SharedConstants.FieldTypeByteArray:
							if (theFieldValue.ValueAsLongText != null)
							{
								newValueObject = Convert.FromBase64String(theFieldValue.ValueAsLongText);
							}
							break;
					}

					property.SetValue(returnDto, newValueObject);
				}
			}

			return returnDto;
		}
	}
}
