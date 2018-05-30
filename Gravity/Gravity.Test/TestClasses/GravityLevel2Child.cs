using System;
using Gravity.Base;

namespace Gravity.Test.TestClasses
{
	[Serializable]
	[RelativityObject("BB6E2528-212E-4FB0-BC85-82E8905B1D42")]
	public class GravityLevel2Child : BaseDto
	{
		[RelativityObjectFieldParentArtifactId("2EB737C1-564B-4E2B-9C0A-CB34F3FB998C")]
		public int GavityLevelOneArtifactId { get; set; }

		[RelativityObjectField("DAD68D2A-F740-4473-8047-E89C5C58D987", RdoFieldType.FixedLengthText, 255)]
		public override string Name { get; set; }
	}
}
