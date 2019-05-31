using System;
using System.Linq;
using System.Reflection;

namespace ModelGenerationTool.Extensions
{
	internal static class TypeExtensions
	{
		internal static A GetPropertyAttribute<A>(this Type type, string propertyName)
			where A : Attribute => type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).First(p => p.Name == propertyName).GetCustomAttribute<A>();
	}
}