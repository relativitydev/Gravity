using Gravity.DAL.RSAPI;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gravity.Base;
using System.Linq.Expressions;
using Gravity.Test.TestClasses;
using Gravity.Extensions;
using kCura.Relativity.Client.DTOs;
using kCura.Relativity.Client;
using Gravity.Test.Helpers;
using LinqKit;

using G1 = Gravity.Test.TestClasses.GravityLevelOne;
using G2 = Gravity.Test.TestClasses.GravityLevel2;
using G2c = Gravity.Test.TestClasses.GravityLevel2Child;
using RdoBoolExpr = System.Linq.Expressions.Expression<System.Func<kCura.Relativity.Client.DTOs.RDO, bool>>;
using Artifact = kCura.Relativity.Client.DTOs.Artifact;

using static Gravity.Test.Helpers.TestObjectHelper;

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

				//Gravity overwrites choices
				if (value.UpdateBehavior == MultiChoiceUpdateBehavior.Merge)
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
				MultipleChoiceFieldChoices = emptyList ? new List<MultipleChoiceFieldChoices>() : null
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
		public void Update_SingleObject_UpdateInsertExisting()
		{
			const int g2id = 20;

			var objectToUpdate = new G1
			{
				GravityLevel2Obj = new G2
				{
					ArtifactId = g2id,
					Name = "NewName"
				}
			};

			RdoBoolExpr matchingRdoExpression = rdo =>
				rdo[FieldGuid<G1>(nameof(G1.GravityLevel2Obj))].ValueAsSingleObject.ArtifactID == g2id;
			//since recursion off, don't update G2 obj

			UpdateObject(objectToUpdate, matchingRdoExpression, ObjectFieldsDepthLevel.OnlyParentObject);
		}

		[Test]
		public void Update_SingleObject_UpdateInsertExisting_WithRecursion()
		{
			const int g2id = 20;

			var objectToUpdate = new G1
			{
				GravityLevel2Obj = new G2
				{
					ArtifactId = g2id,
					Name = "NewName"
				}
			};

			RdoBoolExpr matchingRdoExpression = rdo =>
				rdo[FieldGuid<G1>(nameof(G1.GravityLevel2Obj))].ValueAsSingleObject.ArtifactID == g2id;
			//update G2 object
			RdoBoolExpr matchingG2Expression = rdo =>
				rdo.ArtifactID == g2id
				&& rdo[FieldGuid<G2>(nameof(G2.Name))].ValueAsFixedLengthText == "NewName";

			SetupChildQuery();
			mockProvider.Setup(x => x.UpdateSingle(It.Is(matchingG2Expression)));

			UpdateObject(objectToUpdate, matchingRdoExpression, ObjectFieldsDepthLevel.FirstLevelOnly);
		}

		[Test]
		public void Update_SingleObject_InsertNew()
		{
			var objectToUpdate = new G1
			{
				GravityLevel2Obj = new G2
				{
					Name = "NewName"
				}
			};

			RdoBoolExpr matchingRdoExpression = rdo =>
				rdo[FieldGuid<G1>(nameof(G1.GravityLevel2Obj))].ValueAsSingleObject == null;
			//since recursion off, don't create G2 obj
			//since doesn't exist, don't assign to G1 object

			UpdateObject(objectToUpdate, matchingRdoExpression, ObjectFieldsDepthLevel.OnlyParentObject);
		}

		[Test]
		public void Update_SingleObject_InsertNew_WithRecursion()
		{
			const int g2id = 20;

			var objectToUpdate = new G1
			{
				GravityLevel2Obj = new G2
				{
					Name = "NewName"
				}
			};

			//create and assign G2 object
			RdoBoolExpr matchingRdoExpression = rdo =>
				rdo[FieldGuid<G1>(nameof(G1.GravityLevel2Obj))].ValueAsSingleObject.ArtifactID == g2id;
			RdoBoolExpr matchingG2Expression = rdo =>
				rdo[FieldGuid<G2>(nameof(G2.Name))].ValueAsFixedLengthText == "NewName";

			mockProvider.Setup(x => x.CreateSingle(It.Is(matchingG2Expression))).Returns(g2id);
			SetupChildQuery();

			UpdateObject(objectToUpdate, matchingRdoExpression, ObjectFieldsDepthLevel.FirstLevelOnly);
		}

		[Test, Ignore("No third-level objects")]
		public void Update_SingleObject_InsertNewWithUpdatePropertyRecursion()
		{
		}

		[Test]
		public void Update_SingleObject_Remove()
		{
			var objectToUpdate = new G1();

			RdoBoolExpr matchingRdoExpression = rdo => rdo[FieldGuid<G1>(nameof(G1.GravityLevel2Obj))].Value == null;

			UpdateObject(objectToUpdate, matchingRdoExpression, ObjectFieldsDepthLevel.OnlyParentObject);
		}

		[Test]
		public void Update_MultipleObject_InsertNewAndUpdateInsertExisting()
		{
			const int g2aId = 20;

			var objectToUpdate = new G1
			{
				GravityLevel2MultipleObjs = new[] {
					new G2 { ArtifactId = g2aId, Name = "G2A" }, //exists
					new G2 { Name = "G2B" } //new
				}
			};

			//we don't create new RDOs on non-recursive operations
			RdoBoolExpr matchingRdoExpression = rdo =>
				rdo[FieldGuid<G1>(nameof(G1.GravityLevel2MultipleObjs))].GetValueAsMultipleObject<Artifact>().Single().ArtifactID == g2aId;
			RdoBoolExpr matchingG2Expression = rdo =>
				rdo.ArtifactID == g2aId
				&& rdo[FieldGuid<G2>(nameof(G2.Name))].ValueAsFixedLengthText == "G2A";

			mockProvider.Setup(x => x.UpdateSingle(It.Is(matchingG2Expression)));

			UpdateObject(objectToUpdate, matchingRdoExpression, ObjectFieldsDepthLevel.OnlyParentObject);
			CollectionAssert.AreEqual(new[] { g2aId, 0 }, objectToUpdate.GravityLevel2MultipleObjs.Select(x => x.ArtifactId));
		}

		[Test]
		public void Update_MultipleObject_InsertNewAndUpdateInsertExisting_WithRecursion()
		{
			const int g2aId = 20;
			const int g2bId = 30;

			//update/create children as necessary
			var objectToUpdate = new G1
			{
				GravityLevel2MultipleObjs = new[] {
					new G2 { ArtifactId = g2aId, Name = "G2A" }, //exists
					new G2 { ArtifactId = 0, Name = "G2B" } //new
				}
			};

			//we don't create new RDOs on recursive operations
			RdoBoolExpr matchingRdoExpression = rdo =>
				rdo[FieldGuid<G1>(nameof(G1.GravityLevel2MultipleObjs))].GetValueAsMultipleObject<Artifact>()
					.Select(x => x.ArtifactID)
					.SequenceEqual(new[] { g2aId, g2bId });
			RdoBoolExpr matchingG2aExpression = rdo =>
				rdo.ArtifactID == g2aId	&& rdo[FieldGuid<G2>(nameof(G2.Name))].ValueAsFixedLengthText == "G2A";
			RdoBoolExpr matchingG2bExpression = rdo =>
				rdo.ArtifactID == 0     && rdo[FieldGuid<G2>(nameof(G2.Name))].ValueAsFixedLengthText == "G2B";

			mockProvider.Setup(x => x.UpdateSingle(It.Is(matchingG2aExpression)));
			mockProvider.Setup(x => x.CreateSingle(It.Is(matchingG2bExpression))).Returns(g2bId);
			SetupChildQuery();

			UpdateObject(objectToUpdate, matchingRdoExpression, ObjectFieldsDepthLevel.FirstLevelOnly);
			CollectionAssert.AreEqual(new[] { g2aId, g2bId }, objectToUpdate.GravityLevel2MultipleObjs.Select(x => x.ArtifactId));
		}


		[Test, Ignore("No third-level objects")]
		public void Update_MultipleObject_InsertNewWithUpdatePropertyRecursion()
		{
		}

		[TestCase(true)]
		[TestCase(false)]
		public void Update_MultipleObject_Remove(bool emptyList)
		{
			var objectToUpdate = new G1
			{
				GravityLevel2MultipleObjs = emptyList ? new List<G2>() : null
			};

			RdoBoolExpr matchingRdoExpression = rdo => rdo[FieldGuid<G1>(nameof(G1.GravityLevel2MultipleObjs))].Value == null;

			UpdateObject(objectToUpdate, matchingRdoExpression, ObjectFieldsDepthLevel.OnlyParentObject);
		}

		[Test]
		public void Update_ChildObject_Update()
		{
		}

		[Test]
		public void Update_ChildObject_Update_WithRecursion()
		{
		}

		[Test]
		public void Update_ChildObject_InsertNew()
		{
			//without recursion, should throw an error
		}

		[Test]
		public void Update_ChildObject_InsertNew_WithRecursion()
		{
		}

		[Test]
		public void Update_ChildObject_Remove()
		{
			//delete if not in the collection. Annoying that have to query, but <shrug>
		}

		void UpdateObject(G1 objectToInsert, RdoBoolExpr rootExpression, ObjectFieldsDepthLevel depthLevel)
		{
			objectToInsert.ArtifactId = G1ArtifactId;
			mockProvider.Setup(x => x.UpdateSingle(It.Is(
				PredicateBuilder.New<RDO>(true)
					.And(y => y.ArtifactID == G1ArtifactId)
					.And(rootExpression)
				)));
			new RsapiDao(mockProvider.Object).Update(objectToInsert, depthLevel);
		}

		//this is needed whenever recursion is turned on
		private void SetupChildQuery(params int[] resultArtifactIds)
		{
			mockProvider.Setup(x =>
				x.Query(It.Is<Query<RDO>>(
					y => y.ArtifactTypeGuid == BaseDto.GetObjectTypeGuid<G2c>()
						&& ((WholeNumberCondition)y.Condition).Value.Single() == G1ArtifactId)))
				.Returns(new[] { resultArtifactIds.Select(y => new RDO(y)).ToSuccessResultSet<QueryResultSet<RDO>>() });
		}
	}
}
