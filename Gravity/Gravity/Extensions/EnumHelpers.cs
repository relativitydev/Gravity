using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Gravity.Extensions
{
	public static class EnumHelpers
	{
		public static Guid GetRelativityObjectAttributeGuidValue(this Enum enumValue)
		{
			RelativityObjectAttribute attr = enumValue
				.GetType()
				.GetTypeInfo()
				.GetDeclaredField(enumValue.ToString())
				.GetCustomAttribute<RelativityObjectAttribute>();

			return attr.ObjectTypeGuid;
		}

		public static Dictionary<T1, T2> GetAttributesForValues<T1, T2>() where T2: Attribute
		{
			if (!typeof(T1).IsEnum)
			{
				throw new NotSupportedException($"{typeof(T1).Name} does not represent an enumeration");
			}

			var type = typeof(T1);
			return Enum.GetNames(type).ToDictionary(
				x => (T1)Enum.Parse(type, x),
				x => type.GetField(x).GetCustomAttribute<T2>()
			);
		}
	}
}
