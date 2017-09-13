using System;

namespace Gravity.Attributes
{
	[AttributeUsage(AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
	public class IncludeAllPropertiesInMappingAttribute : Attribute
	{
	}
}
