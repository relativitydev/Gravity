using kCura.Relativity.Client.DTOs;
using System;
using System.Collections.Generic;
using Gravity.Base;

namespace Gravity.Test.TestClasses
{
	[Serializable]
	[RelativityObject("0B5C62E0-2AFA-4408-B7FF-789351C9BEDC")]
	public class GravityLevelOne : BaseDto
	{
		[RelativityObjectField("E1FA93B9-C2DB-442A-9978-84EEB6B61A3F", RdoFieldType.FixedLengthText, 255)]
		public string Name { get; set; }

		[RelativityObjectField("1A7A55A7-C22C-421A-BFE9-CDC1107ABEA3", RdoFieldType.FixedLengthText, 100)]
		public string FixedTextField { get; set; }

		[RelativityObjectField("31E466CF-750C-49E5-A922-184C5FE525F2", RdoFieldType.LongText)]
		public string LongTextField { get; set; }

		[RelativityObjectField("4D673C39-F397-4EC2-B2A0-55FCB20938D5", RdoFieldType.WholeNumber)]
		public int? IntegerField { get; set; }

		[RelativityObjectField("50B690DA-02A1-4543-B5CB-DE76C5D8D33D", RdoFieldType.YesNo)]
		public bool? BoolField { get; set; }

		[RelativityObjectField("17E5D2F1-95D8-4956-B99B-3EA080B02A0E", RdoFieldType.Decimal)]
		public decimal? DecimalField { get; set; }

		[RelativityObjectField("FA00F2A1-2581-492C-823C-A2F89B0155F1", RdoFieldType.Date)]
		public DateTime? DateTimeField { get; set; }

		[RelativityObjectField("06575EC1-7DE4-47C0-A763-AA135C1F29BA", RdoFieldType.File)]
		public FileDto FileField { get; set; }

		[RelativityObjectField("D0770889-8A4D-436A-9647-33419B96E37E", RdoFieldType.MultipleObject)]
		public IList<GravityLevel2> GravityLevel2MultipleObjs { get; set; } = new List<GravityLevel2>();

		[RelativityObjectField("C3336C2C-5A97-4EB1-A3A4-929D79658B8D", RdoFieldType.SingleObject)]
		public GravityLevel2 GravityLevel2Obj { get; set; }

		[RelativityObjectField("C3B2943D-C9C2-4C92-A88D-115B3F9ED64D", RdoFieldType.MultipleChoice)]
		public IList<MultipleChoiceFieldChoices> MultipleChoiceFieldChoices { get; set; } = new List<MultipleChoiceFieldChoices>();

		[RelativityObjectField("CEDB347B-679D-44ED-93D3-0B3027C7E6F5", RdoFieldType.SingleChoice)]
		public SingleChoiceFieldChoices? SingleChoice { get; set; }

		[RelativityObjectField("48404771-DEA2-41AA-8FE6-217E6A04C8DC", RdoFieldType.User)]
		public User UserField { get; set; }

		[RelativityObjectField("AB5D53FB-9217-4633-A4F1-EF3536EDC8EC", RdoFieldType.Currency)]
		public decimal? CurrencyField { get; set; }

		[RelativityObjectChildrenList]
		public IList<GravityLevel2Child> GravityLevel2Childs { get; set; } = new List<GravityLevel2Child>();
	}
}
