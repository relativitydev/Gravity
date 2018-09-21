using Gravity.Base;
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
						newValue = objRow.IsNull(columnName) ? (decimal?)null : Convert.ToDecimal(objRow[columnName]);
						break;
					case RdoFieldType.Decimal:
						newValue = objRow.IsNull(columnName) ? (decimal?)null : Convert.ToDecimal(objRow[columnName]);
						break;
					case RdoFieldType.Empty:
						newValue = null;
						break;
					case RdoFieldType.Date:
						newValue = objRow.IsNull(columnName) ? (DateTime?)null : Convert.ToDateTime(objRow[columnName]);
						break;
					case RdoFieldType.FixedLengthText:
					case RdoFieldType.LongText:
						newValue = objRow.IsNull(columnName) ? null : Convert.ToString(objRow[columnName]);
						break;
					case RdoFieldType.MultipleChoice:
					case RdoFieldType.MultipleObject:
					case RdoFieldType.SingleChoice:
					case RdoFieldType.SingleObject:
					case RdoFieldType.User:
					case RdoFieldType.File:
						break;
					case RdoFieldType.WholeNumber:
						newValue = objRow.IsNull(columnName) ? (int?)null : Convert.ToInt32(objRow[columnName]);
						break;
					case RdoFieldType.YesNo:
						newValue = objRow.IsNull(columnName) ? (bool?)null : Convert.ToBoolean(objRow[columnName]);
						break;
				}

				property.SetValue(returnDto, newValue);
			}

			return returnDto;
		}
	}
}
