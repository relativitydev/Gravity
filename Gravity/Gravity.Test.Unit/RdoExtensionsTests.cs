using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Gravity.Base;
using Gravity.Extensions;
using Gravity.Test.TestClasses;
using kCura.Relativity.Client.DTOs;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace Gravity.Test.Unit
{
	public class RdoExtensionsTests
	{
		[Test]
		[TestCaseSource(nameof(ToHydratedDto_SimpleTypesFields_TestCases))]
		public void ToHydratedDto_SimpleTypesFields(string fieldName, object value)
		{
			var property = typeof(GravityLevelOne).GetProperty(fieldName);
			var propertyGuid = property.GetCustomAttribute<RelativityObjectFieldAttribute>().FieldGuid;

			//make sure what you put into the RDO gets copied to the DTO
			var dto = GetRdoWithField(propertyGuid, new FieldValue() { Value = value });

			Assert.AreEqual(value, property.GetValue(dto));
		}

		public static IEnumerable<TestCaseData> ToHydratedDto_SimpleTypesFields_TestCases()
		{
			return new(string, object)[]
			{
				(nameof(GravityLevelOne.BoolField), true),
				(nameof(GravityLevelOne.CurrencyField), 10.5m),
				(nameof(GravityLevelOne.DateTimeField), new DateTime(2008, 1, 1)),
				(nameof(GravityLevelOne.DecimalField), 10.5m),
				(nameof(GravityLevelOne.LongTextField), "Test"),
				(nameof(GravityLevelOne.FixedTextField), "Test"),
				(nameof(GravityLevelOne.IntegerField), 1),
				(nameof(GravityLevelOne.UserField), new User(1)),
			}.Select(x => new TestCaseData(x.Item1, x.Item2).SetName("{m}(" + x.Item1 + ")"));
		}

		[Test]
		[TestCaseSource(nameof(ToHydratedDto_TypeMismatch_TestCases))]
		public void ToHydratedDto_TypeMismatch(string fieldName, object value)
		{
			Exception AssertThrowsAny(TestDelegate code)
			{
				return Assert.Throws<Exception>(() =>
				{
					try { code(); }
					catch { throw new Exception(); }
				});
			}

			var property = typeof(GravityLevelOne).GetProperty(fieldName);
			var propertyGuid = property.GetFieldGuidValueFromAttribute();

			AssertThrowsAny(() => GetRdoWithField(propertyGuid, new FieldValue() { Value = value }));
		}

		public static IEnumerable<TestCaseData> ToHydratedDto_TypeMismatch_TestCases()
		{
			// Ignore the tests for now, for the given reason.
			return new[]
			{
				new TestCaseData(null, null)
					.Ignore("Not worth investing in checking failure behavior before working on success behavior ")
			};

			// To implement tests, remove the ignore block above and uncomment below
			//
			//return typeof(GravityLevelOne)
			//	.GetPublicProperties()
			//	.Where(x => x.GetFieldGuidValueFromAttribute() != new Guid())
			//	.Select(x => new TestCaseData(x.Name, new { Foo = "Bar" }).SetName("{m}(" + x.Name + ")"));
		}


		[Test]
		[TestCase("single choice2  ")] //test cases ensure ignore whitespace and mixed case
		[TestCase("SingleChoice2")]
		public void ToHydratedDto_SingleChoice_InEnum(string choiceName)
		{
			var propertyGuid = typeof(GravityLevelOne)
				.GetProperty(nameof(GravityLevelOne.SingleChoice))
				.GetFieldGuidValueFromAttribute();

			var fieldValue = new FieldValue()
			{
				ValueAsSingleChoice = new Choice() { Name = choiceName }
			};

			var dto = GetRdoWithField(propertyGuid, fieldValue);

			Assert.AreEqual(SingleChoiceFieldChoices.SingleChoice2, dto.SingleChoice);
		}

		[Test]
		[Ignore("No agreed-on behavior, see issue #80")]
		public void ToHydratedDto_SingleChoice_NotInEnum()
		{
			//What do we do here?
			throw new NotImplementedException();
		}

		[Test]
		public void ToHydratedDto_MultipleChoice_AllInEnum()
		{
			var propertyGuid = typeof(GravityLevelOne)
				.GetProperty(nameof(GravityLevelOne.MultipleChoiceFieldChoices))
				.GetFieldGuidValueFromAttribute();

			var fieldValue = new FieldValue(propertyGuid)
			{
				ValueAsMultipleChoice = new[] { " multiple choice 2", "MultipleChoice3" } //ignore case, whitespace
					.Select(x => new Choice { Name = x })
					.ToList()
			};

			var dto = GetRdoWithField(propertyGuid, fieldValue);

			CollectionAssert.AreEquivalent(
				new[] { MultipleChoiceFieldChoices.MultipleChoice2, MultipleChoiceFieldChoices.MultipleChoice3 }, 
				dto.MultipleChoiceFieldChoices);
		}

		[Test]
		[Ignore("see issue #69")]
		public void ToHydratedDto_MultipleChoice_MultipleEnumSameValue()
		{
			//Note that we are stuck matching by name because of RSAPI limitations. If required, we need to make
			//another query.

			//So for example, matching two different Choices with names "(Other)" and "Other" will both try to map to the same client-side type.
			//This needs to throw an error until it is fixed.
			throw new NotImplementedException();
		}

		[Test]
		[Ignore("No agreed-on behavior, see issue #80")]
		public void ToHydratedDto_MultipleChoice_ChoiceNotInAvailableChoices()
		{
			//What do we want to do when some choices are not enumerated in the set of valid choices? Throw an exception?
			throw new NotImplementedException();
		}

		[TestCase("File.doc")]
		[TestCase(null, TestName = "{m}_NoFile")]
		public void ToHydratedDto_File(string fileName)
		{
			//generates file
			var propertyGuid = typeof(GravityLevelOne)
				.GetProperty(nameof(GravityLevelOne.FileField))
				.GetFieldGuidValueFromAttribute();

			var fieldId = 2;

			var fieldValue = new FieldValue(propertyGuid, fileName) { ArtifactID = fieldId };

			var dto = GetRdoWithField(propertyGuid, fieldValue);

			var expectedValue = fileName == null ? (int?)null : fieldId;
			Assert.AreEqual(expectedValue, dto.FileField?.ArtifactTypeId);
		}

		public static GravityLevelOne GetRdoWithField(Guid propertyGuid, FieldValue fieldValue)
		{
			RDO GetStub<T>(int artifactId) where T : BaseDto, new()
			{
				RelativityObjectAttribute objectTypeAttribute = typeof(T).GetCustomAttribute<RelativityObjectAttribute>(false);
				RDO stubRdo = new RDO(objectTypeAttribute.ObjectTypeGuid, artifactId);

				var fieldValues = typeof(T)
					.GetPublicProperties()
					.Select(x => x.GetFieldGuidValueFromAttribute())
					.Where(x => x != new Guid())
					.Distinct()
					.Select(x => new FieldValue(x, null));
				stubRdo.Fields.AddRange(fieldValues);

				return stubRdo;
			}

			fieldValue.Guids = new List<Guid> { propertyGuid };

			var rdo = GetStub<GravityLevelOne>(1);
			rdo[propertyGuid] = fieldValue;

			return rdo.ToHydratedDto<GravityLevelOne>();
		}
	}
}
