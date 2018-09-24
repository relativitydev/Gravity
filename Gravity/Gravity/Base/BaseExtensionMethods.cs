using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Gravity.Base
{
	public static class BaseExtensionMethods
	{
		public static IEnumerable<PropertyInfo> GetPublicProperties(this Type type)
		{
			if (!type.IsInterface)
			{
				return type.GetProperties();
			}

			return (new Type[] { type })
				   .Concat(type.GetInterfaces())
				   .SelectMany(i => i.GetProperties());
		}

		public static Expected GetAttributeValue<T, Expected>(this Enum enumeration, Func<T, Expected> expression)
		where T : Attribute
		{
			T attribute =
			  enumeration
				.GetType()
				.GetMember(enumeration.ToString())
				.FirstOrDefault(member => member.MemberType == MemberTypes.Field)
				.GetCustomAttributes(typeof(T), false)
				.Cast<T>()
				.SingleOrDefault();

			if (attribute == null)
				return default(Expected);

			return expression(attribute);
		}

		public static string GetEnumDescriptionAttributeValue(this Enum enumeration)
		{
			return enumeration.GetAttributeValue<DescriptionAttribute, String>(x => x.Description);
		}

		public static string GetPropertyName<T>(Expression<Func<T, object>> expression)
			where T : class
		{
			MemberExpression body = (MemberExpression)expression.Body;
			return body.Member.Name;
		}

		public static TAttribute GetCustomAttribute<TAttribute>(this BaseDto obj, string propertyName) where TAttribute : Attribute
		{
			TAttribute fieldAttribute = obj.GetType().GetPublicProperties()
				.SingleOrDefault(property => property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))?
				.GetCustomAttribute<TAttribute>();
			return fieldAttribute;
		}

		public static TAttribute GetObjectLevelCustomAttribute<TAttribute>(this BaseDto obj) where TAttribute : Attribute
		{
			TAttribute fieldAttribute = obj.GetType().GetCustomAttribute<TAttribute>();
			return fieldAttribute;
		}

		public static void SetValueByPropertyName(this object input, string propertyName, object value)
		{
			PropertyInfo prop = input.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
			prop.SetValue(input, value);
		}

		public static IList MakeGenericList(IEnumerable items, Type type)
		{
			var listType = typeof(List<>).MakeGenericType(type);
			IList returnList = (IList)Activator.CreateInstance(listType);
			foreach (var item in items)
			{
				returnList.Add(item);
			}
			return returnList;
		}
	}
}
