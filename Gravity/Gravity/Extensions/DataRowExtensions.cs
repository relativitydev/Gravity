using Gravity.Base;
using kCura.Relativity.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Gravity.Extensions
{
	public static class DataRowExtensions
	{
		public static T ToHydratedDto<T>(this DataRow objRow, Dictionary<Guid, string> fieldsGuidsToColumnNameMappings, int? parentArtifactId)
			where T : BaseDto, new()
		{
			T returnDto = new T();
			returnDto.ArtifactId = Convert.ToInt32(objRow["ArtifactID"]);
			returnDto.GetParentArtifactIdProperty()?.SetValue(returnDto, parentArtifactId);
			
			string columnName;

			foreach ((PropertyInfo property, RelativityObjectFieldAttribute fieldAttribute)
				in typeof(T).GetPropertyAttributeTuples<RelativityObjectFieldAttribute>())
			{
				object newValue = null;
				columnName = fieldsGuidsToColumnNameMappings.FirstOrDefault(x => x.Key == fieldAttribute.FieldGuid).Value;
				
				switch (fieldAttribute.FieldType)
				{
					case RdoFieldType.Currency:
						if (objRow.IsNull(columnName) == false)
						{
							newValue = Convert.ToDecimal(objRow[columnName]);
						}
						break;

					case RdoFieldType.Decimal:
						newValue = objRow.IsNull(columnName) ? Decimal.Zero : Convert.ToDecimal(objRow[columnName]);
						break;

					case RdoFieldType.Date:
						newValue = objRow.IsNull(columnName) ? DateTime.MinValue : Convert.ToDateTime(objRow[columnName]);
						break;

					case RdoFieldType.File:
						int fileId = objRow.IsNull(columnName.Substring(0, columnName.Length - 4)) ? 0 : (int)objRow[columnName.Substring(0, columnName.Length - 4)];
						newValue = new RelativityFile(fileId);
						break;

					case RdoFieldType.FixedLengthText:
					case RdoFieldType.LongText:
						if (objRow.IsNull(columnName) == false)
						{
							newValue = Convert.ToString(objRow[columnName]);
						}
						break;

					case RdoFieldType.MultipleChoice:
					case RdoFieldType.MultipleObject:
					case RdoFieldType.SingleChoice:
					case RdoFieldType.SingleObject:
						break;

					case RdoFieldType.User:
						if (property.PropertyType == typeof(User))
						{
							int userArtifactId = objRow.IsNull(columnName) ? 0 : Convert.ToInt32(objRow[columnName]);

							if (userArtifactId > 0)
							{
								newValue = new User() { ArtifactID = userArtifactId };
							}
						}
						break;

					case RdoFieldType.WholeNumber:
						newValue = objRow[columnName] != DBNull.Value ? Convert.ToInt32(objRow[columnName]) : 0;
						break;

					case RdoFieldType.YesNo:
						if (objRow.IsNull(columnName) == false)
						{
							newValue = Convert.ToBoolean(objRow[columnName]);
						}
						break;
				}

				property.SetValue(returnDto, newValue);
			}

			return returnDto;
		}
	}
}
