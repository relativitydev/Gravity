using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gravity.Base;

namespace Gravity.Test.TestClasses
{
	[Serializable]
	[RelativityObject("BAF1304D-7B0B-4E9F-BE7A-634ED2DF8EB6")]
	public class GravityLevel3Child : BaseDto
	{
		[RelativityObjectFieldParentArtifactId("EDE1FE54-CA19-4CCD-BD26-F458814B373B")]
		public int GavityLevelTwoArtifactId { get; set; }

		[RelativityObjectField("02E68AB7-DF37-4287-9F24-49688A4E13B3", RdoFieldType.FixedLengthText, 255)]
		public string Name { get; set; }
	}
}
