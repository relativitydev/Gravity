using System;
using System.Collections.Generic;
using System.ComponentModel;
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
	}
}
