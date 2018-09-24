using System;
using Gravity.Base;

namespace Gravity.Test.TestClasses
{
	[Serializable]
	[RelativityObject("71244AF1-4800-471F-A3E1-9E02C1A5411F")]
	public class GravityLevel3Child : BaseDto
	{
		[RelativityObjectFieldParentArtifactId("8C306DF6-CBD4-41BC-8572-51BC88A3C8CA")]
		public int GavityLevelTwoChildArtifactId { get; set; }

		[RelativityObjectField("2105B985-583E-49FA-9D68-72F98EE3C25E", RdoFieldType.FixedLengthText, 255)]
		public string Name { get; set; }
	}
}
