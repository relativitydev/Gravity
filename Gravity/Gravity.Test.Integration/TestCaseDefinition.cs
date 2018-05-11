using System;
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
            }
        }
    }
}
