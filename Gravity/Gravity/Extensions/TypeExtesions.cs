using System;
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
				if (parentAttribute != null)
				{
					returnValue = propertyInfo.GetCustomAttribute<RelativityObjectFieldAttribute>().FieldGuid;
				}
			}

			return returnValue;
		}

		public static Guid GetFieldGuidValueFromAttribute(this PropertyInfo propertyInfo)
		{
			Guid returnValue = new Guid();

			if (propertyInfo.GetCustomAttribute<RelativityObjectFieldAttribute>() != null)
			{
				returnValue = propertyInfo.GetCustomAttribute<RelativityObjectFieldAttribute>().FieldGuid;
			}

			if (propertyInfo.GetCustomAttribute<RelativityMultipleObjectAttribute>() != null)
			{
				returnValue = propertyInfo.GetCustomAttribute<RelativityMultipleObjectAttribute>().FieldGuid;
			}

			if (propertyInfo.GetCustomAttribute<RelativitySingleObjectAttribute>() != null)
			{
				returnValue = propertyInfo.GetCustomAttribute<RelativitySingleObjectAttribute>().FieldGuid;
			}

			return returnValue;
		}

		public static object InvokeGenericMethod(this object obj, Type typeArgument, string methodName, params object[] args)
		{
			MethodInfo method = obj.GetType()
				.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
				.MakeGenericMethod(new Type[] { typeArgument });

			return method.Invoke(obj, args);
		}
	}
}
