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

namespace Gravity.Test.Unit
{
	public class RdoExtensionsTests
	{
		[Test]
		[TestCaseSource(nameof(ToHydratedDto_SimpleTypesFields_TestCases))]
		public void ToHydratedDto_SimpleTypesFields(string fieldName, object value)
		{
			var rdo = GetStub<GravityLevelOne>(1);
			var property = typeof(GravityLevelOne).GetProperty(fieldName);
			var propertyGuid = property.GetCustomAttribute<RelativityObjectFieldAttribute>().FieldGuid;

			//make sure what you put into the RDO gets copied to the DTO

			rdo[propertyGuid] = new FieldValue(propertyGuid, value);

			var dto = rdo.ToHydratedDto<GravityLevelOne>();

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
		[Ignore("Not worth investing in checking failure behavior before working on success behavior ")]
		// [TestCaseSource(nameof(ToHydratedDto_TypeMismatch_TestCases))]
		public void ToHydratedDto_TypeMismatch(string fieldName, object value)
		{
			var rdo = GetStub<GravityLevelOne>(1);
			var property = typeof(GravityLevelOne).GetProperty(fieldName);
			var propertyGuid = property.GetFieldGuidValueFromAttribute();

			rdo[propertyGuid] = new FieldValue(propertyGuid, value);
			AssertThrowsAny(() => rdo.ToHydratedDto<GravityLevelOne>());
		}

		public static IEnumerable<TestCaseData> ToHydratedDto_TypeMismatch_TestCases()
		{
			return typeof(GravityLevelOne)
				.GetPublicProperties()
				.Where(x => x.GetFieldGuidValueFromAttribute() != new Guid())
				.Select(x => new TestCaseData(x.Name, new { Foo = "Bar" }).SetName("{m}(" + x.Name + ")"));
		}


		[Test]
		[TestCase("single choice2  ")] //test cases ensure ignore whitespace and mixed case
		[TestCase("SingleChoice2")]
		public void ToHydratedDto_SingleChoice_InEnum(string choiceName)
		{
			var rdo = GetStub<GravityLevelOne>(1);
			var propertyGuid = typeof(GravityLevelOne)
				.GetProperty(nameof(GravityLevelOne.SingleChoice))
				.GetCustomAttribute<RelativityObjectFieldAttribute>()
				.FieldGuid;


			rdo[propertyGuid] = new FieldValue(propertyGuid)
			{
				ValueAsSingleChoice = new Choice() { Name = choiceName }
			};

			Assert.AreEqual(SingleChoiceFieldChoices.SingleChoice2, rdo.ToHydratedDto<GravityLevelOne>().SingleChoice);
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
			var rdo = GetStub<GravityLevelOne>(1);
			var propertyGuid = typeof(GravityLevelOne)
				.GetProperty(nameof(GravityLevelOne.MultipleChoiceFieldChoices))
				.GetCustomAttribute<RelativityObjectFieldAttribute>()
				.FieldGuid;

			rdo[propertyGuid] = new FieldValue(propertyGuid)
			{
				ValueAsMultipleChoice = new[] { " multiple choice 2", "MultipleChoice3" } //ignore case, whitespace
					.Select(x => new Choice { Name = x })
					.ToList()
			};

			CollectionAssert.AreEquivalent(
				new[] { MultipleChoiceFieldChoices.MultipleChoice2, MultipleChoiceFieldChoices.MultipleChoice3 }, 
				rdo.ToHydratedDto<GravityLevelOne>().MultipleChoiceFieldChoices);
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

		[Test]
		public void ToHydratedDto_MultipleObject_PopulatesIntegerListField()
		{
			throw new NotImplementedException();
		}

		[Test]
		public void ToHydratedDto_SingleObject_PopulatesIntegerField()
		{
			throw new NotImplementedException();
		}

		[Test]
		public void ToHydratedDto_User()
		{
			//this is going to be a problem in the long run as we will not always be using RSAPI types
			throw new NotImplementedException();
		}

		[Test]
		public void ToHydratedDto_File()
		{
			//generates file
			throw new NotImplementedException();
		}

		private static RDO GetStub<T>(int artifactId) where T : BaseDto, new()
		{
			RelativityObjectAttribute objectTypeAttribute = typeof(T).GetCustomAttribute<RelativityObjectAttribute>(false);
			RDO rdo = new RDO(objectTypeAttribute.ObjectTypeGuid, artifactId);

			var fieldValues = typeof(T)
				.GetPublicProperties()
				.Select(x => x.GetFieldGuidValueFromAttribute())
				.Where(x => x != new Guid())
				.Distinct()
				.Select(x => new FieldValue(x, null));
			rdo.Fields.AddRange(fieldValues);

			return rdo;
		}

		private static Exception AssertThrowsAny(TestDelegate code)
		{
			return Assert.Throws<Exception>(() =>
			{
				try { code(); }
				catch { throw new Exception(); }
			});
		}
	}
}
