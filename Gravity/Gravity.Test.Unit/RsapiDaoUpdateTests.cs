using Gravity.DAL.RSAPI;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using G1 = Gravity.Test.TestClasses.GravityLevelOne;
using G2 = Gravity.Test.TestClasses.GravityLevel2;
using G2c = Gravity.Test.TestClasses.GravityLevel2Child;
using RdoBoolExpr = System.Linq.Expressions.Expression<System.Func<kCura.Relativity.Client.DTOs.RDO, bool>>;

using static Gravity.Test.Helpers.TestObjectHelper;
using Gravity.Base;
using System.Linq.Expressions;
using Gravity.Test.TestClasses;
using Gravity.Extensions;
using kCura.Relativity.Client.DTOs;

namespace Gravity.Test.Unit
{
	public class RsapiDaoUpdateTests
	{
		private const int G1ArtifactId = 10;
		Mock<IRsapiProvider> mockProvider;

		[SetUp]
		public void Init()
		{
			//ensure fail if method not defined
			mockProvider = new Mock<IRsapiProvider>(MockBehavior.Strict);
		}

		[SetUp]
		public void End()
		{
			//ensure any defined methods called
			mockProvider.VerifyAll();
		}

		[Test]
		public void Update_SimpleFields()
		{
			var objectToUpdate = new G1
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

			UpdateObject(objectToUpdate, matchingRdoExpression, ObjectFieldsDepthLevel.OnlyParentObject);
		}

		[Test]
		public void Update_SimpleFields_Remove()
		{
			var objectToUpdate = new G1();

			RdoBoolExpr matchingRdoExpression = rdo =>
					rdo[FieldGuid<G1>(nameof(G1.BoolField))].Value == null
				&& rdo[FieldGuid<G1>(nameof(G1.CurrencyField))].Value == null
				&& rdo[FieldGuid<G1>(nameof(G1.DateTimeField))].Value == null
				&& rdo[FieldGuid<G1>(nameof(G1.DecimalField))].Value == null
				&& rdo[FieldGuid<G1>(nameof(G1.FixedTextField))].Value == null
				&& rdo[FieldGuid<G1>(nameof(G1.IntegerField))].Value == null
				&& rdo[FieldGuid<G1>(nameof(G1.LongTextField))].Value == null;

			UpdateObject(objectToUpdate, matchingRdoExpression, ObjectFieldsDepthLevel.OnlyParentObject);
		}

		[Test]
		public void Update_SingleChoice()
		{
			var objectToUpdate = new G1
			{
				SingleChoice = SingleChoiceFieldChoices.SingleChoice2
			};

			//checks that matches Updateed object
			RdoBoolExpr matchingRdoExpression = rdo =>
				rdo[FieldGuid<G1>(nameof(G1.SingleChoice))].ValueAsSingleChoice.Guids.Single()
					== SingleChoiceFieldChoices.SingleChoice2.GetRelativityObjectAttributeGuidValue();

			UpdateObject(objectToUpdate, matchingRdoExpression, ObjectFieldsDepthLevel.OnlyParentObject);
		}

		[Test]
		public void Update_SingleChoice_Remove()
		{
			var objectToUpdate = new G1();

			RdoBoolExpr matchingRdoExpression = rdo => rdo[FieldGuid<G1>(nameof(G1.SingleChoice))].Value == null;

			UpdateObject(objectToUpdate, matchingRdoExpression, ObjectFieldsDepthLevel.OnlyParentObject);
		}


		[Test]
		public void Update_MultipleChoice()
		{
			var objectToUpdate = new G1
			{
				MultipleChoiceFieldChoices = new[] { MultipleChoiceFieldChoices.MultipleChoice2, MultipleChoiceFieldChoices.MultipleChoice3 }
			};

			Func<RDO, bool> matchingRdoExpressionInner = rdo => {
				var value = rdo[FieldGuid<G1>(nameof(G1.MultipleChoiceFieldChoices))].ValueAsMultipleChoice;

				if (value.UpdateBehavior == kCura.Relativity.Client.MultiChoiceUpdateBehavior.Merge)
					return false;

				return Enumerable.SequenceEqual(
						value.Select(x => x.Guids.Single()),
						new[] { MultipleChoiceFieldChoices.MultipleChoice2, MultipleChoiceFieldChoices.MultipleChoice3 }
							.Select(x => x.GetRelativityObjectAttributeGuidValue())
						);
			};

			//checks that matches Updateed object				
			UpdateObject(objectToUpdate, rdo => matchingRdoExpressionInner(rdo), ObjectFieldsDepthLevel.OnlyParentObject);
		}

		[TestCase(true)]
		[TestCase(false)]
		public void Update_MultipleChoice_Remove(bool emptyList)
		{
			var objectToUpdate = new G1
			{
				GravityLevel2Childs = emptyList ? new List<G2c>() : null
			};

			RdoBoolExpr matchingRdoExpression = rdo => rdo[FieldGuid<G1>(nameof(G1.SingleChoice))].Value == null;

			UpdateObject(objectToUpdate, matchingRdoExpression, ObjectFieldsDepthLevel.OnlyParentObject);
		}

		[Test, Ignore("File behavior not defined yet")]
		public void Update_FileField_Add()
		{
		}

		[Test, Ignore("File behavior not defined yet")]
		public void Update_FileField_Remove()
		{
		}

		[Test, Ignore("File behavior not defined yet")]
		public void Update_FileField_Modify()
		{
		}
		
		[Test]
		public void Update_ChildObject_Update()
		{
		}

		[Test]
		public void Update_ChildObject_UpdateWithRecursion()
		{
		}

		[Test]
		public void Update_ChildObject_InsertNew()
		{
			//without recursion, should throw an error
		}

		[Test]
		public void Update_ChildObject_InsertNewWithRecursion()
		{
		}

		[Test]
		public void Update_ChildObject_Remove()
		{
			//delete if not in the collection. Annoying that have to query, but <shrug>
		}

		[Test]
		public void Update_SingleObject_Update()
		{
		}

		[Test]
		public void Update_SingleObject_UpdateWithRecursion()
		{
		}

		[Test]
		public void Update_SingleObject_InsertExisting()
		{
		}

		[Test]
		public void Update_SingleObject_InsertExistingWithRecursion()
		{
		}

		[Test]
		public void Update_SingleObject_InsertNew()
		{
			//without recursion, should throw an error
		}

		[Test]
		public void Update_SingleObject_InsertNewWithRecursion()
		{
		}

		[Test, Ignore("No third-level objects")]
		public void Update_SingleObject_InsertNewWithUpdatePropertyRecursion()
		{
		}

		[Test]
		public void Update_SingleObject_Remove()
		{
		}

		[Test]
		public void Update_MultipleObject_Update()
		{
		}

		[Test]
		public void Update_MultipleObject_UpdateWithRecursion()
		{
		}

		[Test]
		public void Update_MultipleObject_InsertExisting()
		{
		}

		[Test]
		public void Update_MultipleObject_InsertExistingWithRecursion()
		{
		}

		[Test]
		public void Update_MultipleObject_InsertNew()
		{
			//without recursion, should throw an error
		}

		[Test]
		public void Update_MultipleObject_InsertNewWithRecursion()
		{
		}


		[Test, Ignore("No third-level objects")]
		public void Update_MultipleObject_InsertNewWithUpdatePropertyRecursion()
		{
		}

		[Test]
		public void Update_MultipleObject_Remove()
		{
		}

		void UpdateObject(G1 objectToInsert, RdoBoolExpr rootExpression, ObjectFieldsDepthLevel depthLevel)
		{
			objectToInsert.ArtifactId = G1ArtifactId;
			mockProvider.Setup(x => x.UpdateSingle(It.Is(rootExpression.And(y => y.ArtifactID == G1ArtifactId))));
			new RsapiDao(mockProvider.Object).Update(objectToInsert, depthLevel);
		}
	}
}
