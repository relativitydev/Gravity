using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gravity.Base;

namespace Gravity.Extensions
{
	public static class TypeExtensions
	{
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
