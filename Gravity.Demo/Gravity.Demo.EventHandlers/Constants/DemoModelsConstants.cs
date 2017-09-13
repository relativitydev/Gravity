using System;
using System.Collections.Generic;
using Gravity.Demo.EventHandlers.Models;

namespace Gravity.Demo.EventHandler.Constants
{
	public static class DemoModelsConstants
	{
		private static GravityLevel2Child level2ChildObjectA = new GravityLevel2Child()
		{
			Name = "Level 2 Child Demo A"
		};

		private static GravityLevel2Child level2ChildObjectB = new GravityLevel2Child()
		{
			Name = "Level 2 Child Demo B"
		};

		public static GravityLevelOne LevelOneObject = new GravityLevelOne()
		{
			Name = "Level One Demo",
			GravityLevel2Childs = new List<GravityLevel2Child> { level2ChildObjectA, level2ChildObjectB },
			BoolField = true,
			CurrencyField = 12.5M,
			DateTimeField = DateTime.Now,
			MultipleChoiceFieldChoices = new List<MultipleChoiceFieldChoices> { MultipleChoiceFieldChoices.MultipleChoice1, MultipleChoiceFieldChoices.MultipleChoice3 },
			SingleChoiceFiledChoices = SingleChoiceFiledChoices.SingleChoice2,
			FixedTextField = "Fixed text demo value",
			IntegerField = 2,
			DecimalField = 3.14M,
			LongTextField = "Long text field demo value"
		};
	}
}
