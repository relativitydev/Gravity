using Gravity.Base;
using Gravity.DAL.RSAPI;
using Gravity.Extensions;
using Gravity.Test.Helpers;
using Gravity.Test.TestClasses;
using kCura.Relativity.Client.DTOs;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using G1 = Gravity.Test.TestClasses.GravityLevelOne;
using G2 = Gravity.Test.TestClasses.GravityLevel2;
using G2c = Gravity.Test.TestClasses.GravityLevel2Child;
using RdoBoolExpr = System.Linq.Expressions.Expression<System.Func<kCura.Relativity.Client.DTOs.RDO, bool>>;

namespace Gravity.Test.Unit
{
	public class RsapiDaoInsertTests
	{
		Mock<IRsapiProvider> mockProvider;

		[SetUp]
		public void Init()
		{
			mockProvider = new Mock<IRsapiProvider>(MockBehavior.Strict);
		}

		[SetUp]
		public void End()
		{
			mockProvider.VerifyAll();
		}

		[Test]
		public void Insert_SimpleFields()
		{
			var objectToInsert = new G1
			{
				BoolField = true,
				CurrencyField = .5M,
				DateTimeField = new DateTime(2000, 1, 1),
				DecimalField = .6M,
				FixedTextField = "FixedText",
				IntegerField = 2,
				LongTextField = "LongText",
			};

			RdoBoolExpr matchingRdoExpression = rdo =>
					rdo[FieldGuid<G1>(nameof(G1.BoolField))].Value.Equals(true)
				&& rdo[FieldGuid<G1>(nameof(G1.CurrencyField))].Value.Equals(.5M)
				&& rdo[FieldGuid<G1>(nameof(G1.DateTimeField))].Value.Equals(new DateTime(2000, 1, 1))
				&& rdo[FieldGuid<G1>(nameof(G1.DecimalField))].Value.Equals(.6M)
				&& rdo[FieldGuid<G1>(nameof(G1.FixedTextField))].Value.Equals("FixedText")
				&& rdo[FieldGuid<G1>(nameof(G1.IntegerField))].Value.Equals(2)
				&& rdo[FieldGuid<G1>(nameof(G1.LongTextField))].Value.Equals("LongText");

			mockProvider.Setup(x => x.CreateSingle(It.Is(matchingRdoExpression))).Returns(10);
			var insertedId = new RsapiDao(mockProvider.Object).Insert(objectToInsert, ObjectFieldsDepthLevel.OnlyParentObject);
			Assert.AreEqual(10, insertedId);
			Assert.AreEqual(10, objectToInsert.ArtifactId);



		}

		[Test]
		public void Insert_SingleChoice()
		{
			var objectToInsert = new G1
			{
				SingleChoice = SingleChoiceFieldChoices.SingleChoice2
			};

			RdoBoolExpr matchingRdoExpression = rdo =>
				rdo[FieldGuid<G1>(nameof(G1.SingleChoice))].ValueAsSingleChoice.Guids.Single() 
					== SingleChoiceFieldChoices.SingleChoice2.GetRelativityObjectAttributeGuidValue();

			mockProvider.Setup(x => x.CreateSingle(It.Is(matchingRdoExpression))).Returns(10);
			var insertedId = new RsapiDao(mockProvider.Object).Insert(objectToInsert, ObjectFieldsDepthLevel.OnlyParentObject);
			Assert.AreEqual(10, insertedId);
			Assert.AreEqual(10, objectToInsert.ArtifactId);
		}

		[Test]
		public void Insert_MultipleChoice()
		{
			var objectToInsert = new G1
			{
				MultipleChoiceFieldChoices = new[] { MultipleChoiceFieldChoices.MultipleChoice2, MultipleChoiceFieldChoices.MultipleChoice3 }
			};

			RdoBoolExpr matchingRdoExpression = rdo =>
				Enumerable.SequenceEqual(
					rdo[FieldGuid<G1>(nameof(G1.MultipleChoiceFieldChoices))].ValueAsMultipleChoice.Select(x => x.Guids.Single()),				
					new[] { MultipleChoiceFieldChoices.MultipleChoice2, MultipleChoiceFieldChoices.MultipleChoice3 }
						.Select(x => x.GetRelativityObjectAttributeGuidValue()));

			mockProvider.Setup(x => x.CreateSingle(It.Is(matchingRdoExpression))).Returns(10);
			var insertedId = new RsapiDao(mockProvider.Object).Insert(objectToInsert, ObjectFieldsDepthLevel.OnlyParentObject);
			Assert.AreEqual(10, insertedId);
			Assert.AreEqual(10, objectToInsert.ArtifactId);
		}

		[Test, Ignore("File behavior not defined yet")]
		public void Insert_FileField()
		{
		}

		[Test]
		public void Insert_NewSingleObject_Recursive()
		{
			const int g2Id = 20;

			var objectToInsert = new G1
			{
				GravityLevel2Obj = new G2 { Name = "G2" }
			};

			//G2 object created with its fields, not just as a stub
			RdoBoolExpr matchingG2Expression = rdo => 
				rdo.Fields.Any(f => f.Guids.Contains(FieldGuid<G2>(nameof(G2.Name))) && f.ValueAsFixedLengthText == "G2");
			RdoBoolExpr matchingG1Expression = rdo =>
				rdo.Fields.Any(f => f.Guids.Contains(FieldGuid<G1>(nameof(G1.GravityLevel2Obj))) && f.ValueAsSingleObject.ArtifactID == g2Id);

			mockProvider.Setup(x => x.CreateSingle(It.Is(matchingG2Expression))).Returns(g2Id);
			mockProvider.Setup(x => x.CreateSingle(It.Is(matchingG1Expression))).Returns(10);
			var insertedId = new RsapiDao(mockProvider.Object).Insert(objectToInsert, ObjectFieldsDepthLevel.FirstLevelOnly);

			Assert.AreEqual(10, insertedId);
			Assert.AreEqual(10, objectToInsert.ArtifactId);
			Assert.AreEqual(g2Id, objectToInsert.GravityLevel2Obj.ArtifactId);
		}

		[Test]
		public void Insert_NewSingleObject_NonRecursive()
		{
			var objectToInsert = new G1
			{
				GravityLevel2Obj = new G2 { Name = "G2" }
			};

			RdoBoolExpr matchingG1Expression = rdo => rdo[FieldGuid<G1>(nameof(G1.GravityLevel2Obj))].Value == null;

			mockProvider.Setup(x => x.CreateSingle(It.Is(matchingG1Expression))).Returns(10);
			var insertedId = new RsapiDao(mockProvider.Object).Insert(objectToInsert, ObjectFieldsDepthLevel.OnlyParentObject);
			Assert.AreEqual(10, insertedId);
			Assert.AreEqual(10, objectToInsert.ArtifactId);
			//since only parent object, G2 object never inserted
			Assert.AreEqual(0, objectToInsert.GravityLevel2Obj.ArtifactId);
		}

		[Test]
		public void Insert_ExistingSingleObject_DontUpdateFields()
		{
			const int g2Id = 20;

			var objectToInsert = new G1
			{
				GravityLevel2Obj = new G2 { ArtifactId = g2Id }
			};

			RdoBoolExpr matchingG1Expression = rdo => rdo[FieldGuid<G1>(nameof(G1.GravityLevel2Obj))].ValueAsSingleObject.ArtifactID == g2Id;

			mockProvider.Setup(x => x.CreateSingle(It.Is(matchingG1Expression))).Returns(10);

			//even though first level, will be no update of G2 object because already exists
			var insertedId = new RsapiDao(mockProvider.Object).Insert(objectToInsert, ObjectFieldsDepthLevel.FirstLevelOnly);
			Assert.AreEqual(10, insertedId);
			Assert.AreEqual(10, objectToInsert.ArtifactId);
			Assert.AreEqual(g2Id, objectToInsert.GravityLevel2Obj.ArtifactId);
		}

		[Test, Ignore("No Level-3 object for testing")]
		public void Insert_ExistingSingleObject_DontInsertChildren()
		{
			var objectToInsert = new G1
			{
			};
		}

	

		private Guid FieldGuid<T>(string fieldName)
			=> typeof(T).GetProperty(fieldName).GetCustomAttribute<RelativityObjectFieldAttribute>().FieldGuid;
	}
}
