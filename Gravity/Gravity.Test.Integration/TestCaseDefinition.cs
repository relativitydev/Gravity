﻿using System;
using System.Collections.Generic;
using Gravity.Test.Helpers;
using Gravity.Test.TestClasses;
using NUnit.Framework;

namespace Gravity.Test.Integration
{
	public static class TestCaseDefinition
	{
		public static IEnumerable<TestCaseData> SimpleFieldReadWriteTestCases
		{
			get
			{
				yield return new TestCaseData("LongTextField", TestValues.LongTextFieldValue);
				yield return new TestCaseData("FixedTextField", TestValues.String100Length);
				yield return new TestCaseData("IntegerField", -1);
				yield return new TestCaseData("BoolField", true);
				yield return new TestCaseData("DecimalField", 123.45);
				yield return new TestCaseData("CurrencyField", 5648.54);
				yield return new TestCaseData("SingleChoice", SingleChoiceFieldChoices.SingleChoice2);
				yield return new TestCaseData("GravityLevel2Obj", new GravityLevel2() { Name = "Test_" + Guid.NewGuid() });
				yield return new TestCaseData("GravityLevel2MultipleObjs", new List<GravityLevel2>(){
					new GravityLevel2()
					{
						Name = "Test_" + Guid.NewGuid()
					},
					new GravityLevel2()
					{
						Name = "Test_" + Guid.NewGuid()
					},
					new GravityLevel2()
					{
						Name = "Test_" + Guid.NewGuid()
					}
				});
			}
		}

	}
}
