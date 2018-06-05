using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Gravity.Base;
using Gravity.Extensions;
using Gravity.Test.Helpers;
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

		[TestCase("File.doc")]
		[TestCase(null, TestName = "{m}_NoFile")]
		public void ToHydratedDto_File(string fileName)
		{
			//generates file
			var propertyGuid = typeof(GravityLevelOne)
				.GetProperty(nameof(GravityLevelOne.FileField))
				.GetCustomAttribute<RelativityObjectFieldAttribute>()
				.FieldGuid;

			var fieldId = 2;

			var fieldValue = new FieldValue(propertyGuid, fileName) { ArtifactID = fieldId };

			var dto = GetRdoWithField(propertyGuid, fieldValue);

			var expectedValue = fileName == null ? (int?)null : fieldId;
			Assert.AreEqual(expectedValue, dto.FileField?.ArtifactTypeId);
		}

		public static GravityLevelOne GetRdoWithField(Guid propertyGuid, FieldValue fieldValue)
		{
			fieldValue.Guids = new List<Guid> { propertyGuid };

			var rdo = TestObjectHelper.GetStubRDO<GravityLevelOne>(1);
			rdo[propertyGuid] = fieldValue;

			return rdo.ToHydratedDto<GravityLevelOne>();
		}
	}
}
