using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gravity.Base;

namespace Gravity.Extensions
{
	public static class TypeExtesions
	{
		public static Guid GetRelativityObjectGuidForParentField(this Type type)
		{
			Guid returnValue = new Guid();

			foreach (var propertyInfo in type.GetPublicProperties())
			{
				RelativityObjectFieldParentArtifactIdAttribute parentAttribute = propertyInfo.GetCustomAttribute<RelativityObjectFieldParentArtifactIdAttribute>();
				return propertyInfo.GetCustomAttribute<RelativityObjectFieldAttribute>()?.FieldGuid ?? new Guid();
			}

			return returnValue;
		}

		public static Guid GetFieldGuidValueFromAttribute(this PropertyInfo propertyInfo)
		{
			return propertyInfo.GetCustomAttribute<RelativityObjectFieldAttribute>()?.FieldGuid
				?? propertyInfo.GetCustomAttribute<RelativityMultipleObjectAttribute>()?.FieldGuid
				?? propertyInfo.GetCustomAttribute<RelativityMultipleObjectAttribute>()?.FieldGuid
				?? new Guid();
		}

		public static object InvokeGenericMethod(this object obj, Type typeArgument, string methodName, params object[] args)
		{
			MethodInfo method = obj.GetType()
				.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
				.MakeGenericMethod(new Type[] { typeArgument });

			return method.Invoke(obj, args);
		}

		// performance boost option: cache results of these
		public static Type GetEnumerableInnerType(this Type type)
		{
			return 
				type.GetInterfaces()
				.First(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
				.GetGenericArguments()[0];
		}
	}
}
