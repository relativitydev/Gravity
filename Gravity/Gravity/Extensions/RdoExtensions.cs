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

					switch ((int)fieldAttribute.FieldType)
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
								RelativityFile fileData = new RelativityFile(theFieldValue.ArtifactID);
								newValueObject = fileData;
							}
							break;
						case (int)RdoFieldType.FixedLengthText:
							newValueObject = theFieldValue.ValueAsFixedLengthText;
							break;
						case (int)RdoFieldType.LongText:
							newValueObject = theFieldValue.ValueAsLongText;
							break;
						case (int)RdoFieldType.MultipleChoice:
							// Means we have IList<some_enum> here, in fieldAttribute.ObjectDTOType
							var valueAsMultipleChoice = theFieldValue.ValueAsMultipleChoice;
							if (valueAsMultipleChoice != null)
							{
								var listOfEnumValuesDoNotUse = typeof(List<>).MakeGenericType(fieldAttribute.ObjectFieldDTOType);
								var listOfEnumValuesInstance = (IList)Activator.CreateInstance(listOfEnumValuesDoNotUse);
								foreach (var choice in valueAsMultipleChoice)
								{
									string choiceNameTrimmed = choice.Name.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
									System.Array enumValues = System.Enum.GetValues(fieldAttribute.ObjectFieldDTOType);
									for (int i = 0; i < enumValues.Length; i++)
									{
										object theValueObject = enumValues.GetValue(i);
										if (theValueObject.ToString().Equals(choiceNameTrimmed, StringComparison.OrdinalIgnoreCase) == true)
										{
											listOfEnumValuesInstance.Add(theValueObject);
										}
									}
								}

								// Now we have a List<object> and we need it to be List<fieldAttribute.ObjectFieldDTOType>
								newValueObject = listOfEnumValuesInstance;
							}
							break;
						case (int)RdoFieldType.MultipleObject:
							newValueObject = theFieldValue.GetValueAsMultipleObject<kCura.Relativity.Client.DTOs.Artifact>()
								.Select<kCura.Relativity.Client.DTOs.Artifact, int>(artifact => artifact.ArtifactID).ToList();
							break;
						case (int)RdoFieldType.SingleChoice:
							kCura.Relativity.Client.DTOs.Choice theChoice = theFieldValue.ValueAsSingleChoice;
							if (theChoice != null)
							{
								string choiceNameTrimmed = theChoice.Name.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
								System.Array enumValues = System.Enum.GetValues(fieldAttribute.ObjectFieldDTOType);
								for (int i = 0; i < enumValues.Length; i++)
								{
									object theValueObject = enumValues.GetValue(i);
									if (theValueObject.ToString().Equals(choiceNameTrimmed, StringComparison.OrdinalIgnoreCase) == true)
									{
										newValueObject = theValueObject;
										break;
									}
								}
							}
							break;
						case (int)RdoFieldType.SingleObject:
							if (theFieldValue != null && theFieldValue.ValueAsSingleObject != null && theFieldValue.ValueAsSingleObject.ArtifactID > 0)
							{
								newValueObject = theFieldValue.ValueAsSingleObject.ArtifactID;
							}
							break;
						case (int)RdoFieldType.User:
							if (theFieldValue.Value != null)
							{
								if (property.PropertyType == typeof(User))
								{
									newValueObject = theFieldValue.Value as User;
								}
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
