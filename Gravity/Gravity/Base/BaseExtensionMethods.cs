using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Gravity.Attributes;

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

		// Get all classes and interfaces that this type implements
		// And take the ones that have IncludeAllPropertiesInBaseObjectComparsionAttribute
		// Then return all the public properties for those classes and interfaces
		public static IEnumerable<PropertyInfo> GetAllPropertiesForMapping(this Type type)
		{
			List<PropertyInfo> allPropertiesForMapping = new List<PropertyInfo>();

			List<Type> allImplementedInterfacesIncludingSelf = new List<Type>();
			allImplementedInterfacesIncludingSelf.AddRange(type.GetInterfaces());

			if (type.IsInterface == true)
			{
				allImplementedInterfacesIncludingSelf.Add(type);
			}

			foreach (Type theInterfaceThaContainsPropertiesForMapping in allImplementedInterfacesIncludingSelf)
			{
				if (theInterfaceThaContainsPropertiesForMapping.GetCustomAttribute<IncludeAllPropertiesInMappingAttribute>(true) != null)
				{
					allPropertiesForMapping.AddRange(theInterfaceThaContainsPropertiesForMapping.GetProperties());
				}
			}

			return allPropertiesForMapping;
		}

		public static Expected GetAttributeValue<T, Expected>(this Enum enumeration, Func<T, Expected> expression)
		where T : Attribute
		{
			T attribute =
			  enumeration
				.GetType()
				.GetMember(enumeration.ToString())
				.Where(member => member.MemberType == MemberTypes.Field)
				.FirstOrDefault()
				.GetCustomAttributes(typeof(T), false)
				.Cast<T>()
				.SingleOrDefault();

			if (attribute == null)
				return default(Expected);

			return expression(attribute);
		}

		public static String GetEnumDescriptionAttributeValue(this Enum enumeration) 
		{ 
			return enumeration.GetAttributeValue<DescriptionAttribute, String>(x => x.Description); 
		}

		public static string GetPropertyName<T, TReturn>(this T obj, Expression<Func<T, TReturn>> expression)
			where T : class
		{
			MemberExpression body = (MemberExpression)expression.Body;
			return body.Member.Name;
		}

		public static void SetValueByPropertyName(this object input, string propertyName, object value)
		{
			PropertyInfo prop = input.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
			prop.SetValue(input, value);
		}
	}
}
