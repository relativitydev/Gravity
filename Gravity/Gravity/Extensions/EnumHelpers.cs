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

		public static T GetAttributeOfTypeFromEnum<T>(this Enum enumVal) where T : System.Attribute
		{
			var type = enumVal.GetType();
			var memInfo = type.GetMember(enumVal.ToString());
			var attributes = memInfo[0].GetCustomAttributes(typeof(T), false);
			return (attributes.Length > 0) ? (T)attributes[0] : null;
		}

		public static string GetDescriptionAttributeValue(this Enum enumVal)
		{
			var type = enumVal.GetType();
			var memInfo = type.GetMember(enumVal.ToString());
			var attribute = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), true)[0];
			var description = (DescriptionAttribute)attribute;

			return description.Description;
		}
	}
}
