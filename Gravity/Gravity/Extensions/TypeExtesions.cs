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
	}
}
