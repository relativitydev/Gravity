using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
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

			try
			{ 
				return method.Invoke(obj, args);
			}
			catch(TargetInvocationException ex)
			{
				// rethrow actual exception https://stackoverflow.com/a/17091351/1180926
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				throw;
			}
		}

		// performance boost option: cache results of these
		public static Type GetEnumerableInnerType(this Type type)
		{
			return 
				type.GetInterfaces()
				.First(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
				.GetGenericArguments()[0];
		}

		public static IEnumerable<Tuple<PropertyInfo, A>> GetPropertyAttributeTuples<A>(this Type type) where A : Attribute
		{
			return type.GetPublicProperties()
				.Select(p => new Tuple<PropertyInfo, A>(p, p.GetCustomAttribute<A>()))
				.Where(kvp => kvp.Item2 != null);
		}
	}
}
