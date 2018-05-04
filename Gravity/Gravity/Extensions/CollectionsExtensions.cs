using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Gravity.Extensions
{
	public static class CollectionsExtensions
	{
		public static Type HeuristicallyDetermineType(this IList myList)
		{
			var enumerable_type =
				myList.GetType()
				.GetInterfaces()
				.Where(i => i.IsGenericType && i.GenericTypeArguments.Length == 1)
				.FirstOrDefault(i => i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

			if (enumerable_type != null)
				return enumerable_type.GenericTypeArguments[0];

			if (myList.Count == 0)
				return null;

			return myList[0].GetType();
		}
	}
}
