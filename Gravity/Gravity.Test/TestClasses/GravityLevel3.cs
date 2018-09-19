using System;
using Gravity.Base;

namespace Gravity.Test.TestClasses
{
	[Serializable]
	[RelativityObject("59662482-5789-4905-9012-63839E6D2C11")]
	public class GravityLevel3 : BaseDto
	{
		[RelativityObjectField("F943BD19-D2F6-41C5-9EE6-8ECB7A90C131", RdoFieldType.FixedLengthText, 255)]
		public string Name { get; set; }
	}
}
