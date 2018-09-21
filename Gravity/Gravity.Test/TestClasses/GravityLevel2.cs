using System;
using Gravity.Base;

namespace Gravity.Test.TestClasses
{
	[Serializable]
	[RelativityObject("B997FAB7-FDEF-42E9-BF57-C173F8D32CD6")]
	public class GravityLevel2 : BaseDto
	{
		[RelativityObjectField("97E0BA08-9291-416A-9CE1-8D36EBDF9113", RdoFieldType.FixedLengthText, 255)]
		public string Name { get; set; }

		[RelativityObjectField("CE1E19FF-5603-4241-847E-72D2BD790984", RdoFieldType.SingleObject)]
		public GravityLevel3 GravityLevel3SingleObj { get; set; }
	}
}
