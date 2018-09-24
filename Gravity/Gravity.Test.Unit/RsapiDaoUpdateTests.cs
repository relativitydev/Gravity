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
using G3 = Gravity.Test.TestClasses.GravityLevel3;
using G2c = Gravity.Test.TestClasses.GravityLevel2Child;
using RdoBoolCond = System.Func<kCura.Relativity.Client.DTOs.RDO, bool>;
using Artifact = kCura.Relativity.Client.DTOs.Artifact;

using static Gravity.Test.Helpers.TestObjectHelper;
using System.IO;
using Castle.Components.DictionaryAdapter.Xml;
using Gravity.Utils;
using Gravity.Globals;

namespace Gravity.Test.Unit
{
	public class RsapiDaoUpdateTests
	{
		private const int G1ArtifactId = 10;
		private const int FileFieldId = 44;
		Mock<IRsapiProvider> mockProvider;

		[SetUp]
		public void Init()
		{
			//ensure fail if method not defined
			mockProvider = new Mock<IRsapiProvider>(MockBehavior.Strict);
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

			RdoBoolCond matchingRdoExpression = rdo =>
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

			RdoBoolCond matchingRdoExpression = rdo =>
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

			RdoBoolCond matchingRdoExpression = rdo =>
				rdo[FieldGuid<G1>(nameof(G1.SingleChoice))].ValueAsSingleChoice.Guids.Single()
					== SingleChoiceFieldChoices.SingleChoice2.GetRelativityObjectAttributeGuidValue();

			UpdateObject(objectToUpdate, matchingRdoExpression, ObjectFieldsDepthLevel.OnlyParentObject);
		}

		[Test]
		public void Update_SingleChoice_Remove()
		{
			var objectToUpdate = new G1();

			RdoBoolCond matchingRdoExpression = rdo => rdo[FieldGuid<G1>(nameof(G1.SingleChoice))].Value == null;

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

			//checks that matches Updated object				
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

			RdoBoolCond matchingRdoExpression = rdo => rdo[FieldGuid<G1>(nameof(G1.SingleChoice))].Value == null;

			UpdateObject(objectToUpdate, matchingRdoExpression, ObjectFieldsDepthLevel.OnlyParentObject);
		}

		[Test]
		public void Update_FileField_Add()
		{
			var objectToUpdate = new G1 {
				ArtifactId = 10,
				FileField = new ByteArrayFileDto {
					FileName = "ByteArrayFileDto",
					ByteArray = new byte[] {65}
				}
			};

			RdoBoolCond matchingRdoExpression = rdo => rdo.ArtifactID == 10;
			UpdateObjectWithFileField(objectToUpdate, matchingRdoExpression, ObjectFieldsDepthLevel.OnlyParentObject);
		}

		[Test]
		public void Update_FileField_Remove()
		{
			var objectToUpdate = new G1 {
				ArtifactId = 10,
				FileField = null
			};

			RdoBoolCond matchingRdoExpression = rdo => rdo.ArtifactID == 10;
			UpdateObjectWithFileField(objectToUpdate, matchingRdoExpression, ObjectFieldsDepthLevel.OnlyParentObject);
		}

		//[Test, Ignore("TODO: Implement")]
		[Test]
		public void Update_FileField_Modify()
		{
			var objectToUpdate = new G1 {
				ArtifactId = 10,
				FileField = new ByteArrayFileDto {
					FileName = "NewName",
					ByteArray = new byte[] { 65 }
				}
			};

			RdoBoolCond matchingRdoExpression = rdo => rdo.ArtifactID == 10;
			UpdateObjectWithFileField(objectToUpdate, matchingRdoExpression, ObjectFieldsDepthLevel.OnlyParentObject);
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

			RdoBoolCond matchingRdoExpression = rdo =>
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

			RdoBoolCond matchingRdoExpression = rdo =>
				rdo[FieldGuid<G1>(nameof(G1.GravityLevel2Obj))].ValueAsSingleObject.ArtifactID == g2id;
			//update G2 object
			RdoBoolCond matchingG2Expression = rdo =>
				rdo.ArtifactID == g2id
				&& rdo[FieldGuid<G2>(nameof(G2.Name))].ValueAsFixedLengthText == "NewName";

			SetupChildQuery();
			SetupUpdateManyCondition(x => x.Count == 1 && matchingG2Expression(x[0]));

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

			RdoBoolCond matchingRdoExpression = rdo =>
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
			RdoBoolCond matchingRdoExpression = rdo =>
				rdo[FieldGuid<G1>(nameof(G1.GravityLevel2Obj))].ValueAsSingleObject.ArtifactID == g2id;
			RdoBoolCond matchingG2Expression = rdo =>
				rdo[FieldGuid<G2>(nameof(G2.Name))].ValueAsFixedLengthText == "NewName";

			SetupInsertManyCondition(x => x.Count == 1 && matchingG2Expression(x[0]), g2id);
			SetupChildQuery();

			UpdateObject(objectToUpdate, matchingRdoExpression, ObjectFieldsDepthLevel.FirstLevelOnly);
		}

		[Test]
		public void Update_SingleObject_InsertNewWithUpdatePropertyRecursion()
		{
			const int g2id = 20;
			const int g3id = 30;

			var objectToUpdate = new G1 {
				GravityLevel2Obj = new G2 {
					Name = "G2",
					ArtifactId = g2id,
					GravityLevel3SingleObj = new G3 {
						Name = "NewName",
						ArtifactId = g3id
					}
				}
			};

			RdoBoolCond matchingRdoExpression = rdo =>
				rdo[FieldGuid<G1>(nameof(G1.GravityLevel2Obj))].ValueAsSingleObject.ArtifactID == g2id;
			RdoBoolCond matchingG2Expression = rdo =>
				rdo[FieldGuid<G2>(nameof(G2.Name))].ValueAsFixedLengthText == "G2";
			RdoBoolCond matchingG3Expression = rdo =>
				rdo[FieldGuid<G3>(nameof(G3.Name))].ValueAsFixedLengthText == "G3";

			SetupUpdateManyCondition(x => x.Count == 1 && matchingG2Expression(x[0]));
			SetupInsertManyCondition(x => x.Count == 1 && matchingG2Expression(x[0]), g3id);
			SetupChildQuery(g2id);
			SetupMultipleLevelChildQuery(g3id);
			SetupDeleteChild();

			UpdateObject(objectToUpdate, matchingRdoExpression, ObjectFieldsDepthLevel.FullyRecursive);
		}

		[Test]
		public void Update_SingleObject_Remove()
		{
			var objectToUpdate = new G1();

			RdoBoolCond matchingRdoExpression = rdo => rdo[FieldGuid<G1>(nameof(G1.GravityLevel2Obj))].Value == null;

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

			//non-recursive, so don't create G2A and only assign G2B to G1
			RdoBoolCond matchingRdoExpression = rdo =>
				rdo[FieldGuid<G1>(nameof(G1.GravityLevel2MultipleObjs))].GetValueAsMultipleObject<Artifact>().Single().ArtifactID == g2aId;
			RdoBoolCond matchingG2Expression = rdo =>
				rdo.ArtifactID == g2aId
				&& rdo[FieldGuid<G2>(nameof(G2.Name))].ValueAsFixedLengthText == "G2A";

			SetupUpdateManyCondition(x => x.Count == 1 && matchingG2Expression(x[0]));

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

			//create G2B, update G2A, assign both to G1
			RdoBoolCond matchingRdoExpression = rdo =>
				rdo[FieldGuid<G1>(nameof(G1.GravityLevel2MultipleObjs))].GetValueAsMultipleObject<Artifact>()
					.Select(x => x.ArtifactID)
					.SequenceEqual(new[] { g2aId, g2bId });
			RdoBoolCond matchingG2aExpression = rdo =>
				rdo.ArtifactID == g2aId	&& rdo[FieldGuid<G2>(nameof(G2.Name))].ValueAsFixedLengthText == "G2A";
			RdoBoolCond matchingG2bExpression = rdo =>
				rdo.ArtifactID == 0     && rdo[FieldGuid<G2>(nameof(G2.Name))].ValueAsFixedLengthText == "G2B";

			SetupUpdateManyCondition(x => x.Count == 1 && matchingG2aExpression(x[0]));
			SetupInsertManyCondition(x => x.Count == 1 && matchingG2bExpression(x[0]), g2bId);
			SetupChildQuery();

			UpdateObject(objectToUpdate, matchingRdoExpression, ObjectFieldsDepthLevel.FirstLevelOnly);
			CollectionAssert.AreEqual(new[] { g2aId, g2bId }, objectToUpdate.GravityLevel2MultipleObjs.Select(x => x.ArtifactId));
		}

		[Test]
		public void Update_MultipleObject_InsertNewWithUpdatePropertyRecursion()
		{
			const int g2aId = 20;
			const int g2bId = 30;
			const int g3aId = 40;
			const int g3bId = 50;

			var objectToUpdate = new G1 
			{
				GravityLevel2MultipleObjs = new[] {
					new G2 {
						ArtifactId = g2aId,
						Name = "G2A",
						GravityLevel3SingleObj = new G3 {
							ArtifactId = g3aId,
							Name = "G3A"
						}
					},
					new G2 {
						ArtifactId = g2bId,
						Name = "G2B",
						GravityLevel3SingleObj = new GravityLevel3() 
						{
							ArtifactId = g3bId,
							Name = "G3B"
						}
					}
				}
			};

			RdoBoolCond matchingRdoExpression = rdo =>
				rdo[FieldGuid<G1>(nameof(G1.GravityLevel2MultipleObjs))].GetValueAsMultipleObject<Artifact>()
					.Select(x => x.ArtifactID)
					.SequenceEqual(new[] {g2aId, g2bId});
			RdoBoolCond matchingG2aExpression = rdo =>
				rdo[FieldGuid<G2>(nameof(G2.Name))].ValueAsFixedLengthText == "G2A";
			RdoBoolCond matchingG2bExpression = rdo =>
				rdo[FieldGuid<G2>(nameof(G2.Name))].ValueAsFixedLengthText == "G2B";

			SetupUpdateManyCondition(x => x.Count == 2 && (matchingG2aExpression(x[0]) || matchingG2bExpression(x[0])));
			SetupInsertManyCondition(x => x.Count == 1 && matchingG2bExpression(x[0]), g3bId);
			SetupChildQuery(g2aId,g2bId);
			SetupMultipleLevelChildQuery(g3aId);
			SetupDeleteChild();

			UpdateObject(objectToUpdate, matchingRdoExpression, ObjectFieldsDepthLevel.FullyRecursive);
			CollectionAssert.AreEqual(new int[] { g2aId, g2bId }, objectToUpdate.GravityLevel2MultipleObjs.Select(x => x.ArtifactId));
			CollectionAssert.AreEqual(new int[] { g3aId, g3bId },objectToUpdate.GravityLevel2MultipleObjs.Select(x => x.GravityLevel3SingleObj.ArtifactId));
		}

		[TestCase(true)]
		[TestCase(false)]
		public void Update_MultipleObject_Remove(bool emptyList)
		{
			var objectToUpdate = new G1
			{
				GravityLevel2MultipleObjs = emptyList ? new List<G2>() : null
			};

			RdoBoolCond matchingRdoExpression = rdo => rdo[FieldGuid<G1>(nameof(G1.GravityLevel2MultipleObjs))].Value == null;

			UpdateObject(objectToUpdate, matchingRdoExpression, ObjectFieldsDepthLevel.OnlyParentObject);
		}

		[Test]
		public void Update_ChildObject_UpdateInsertNew_IgnoredWithNoRecursion()
		{
			const int g2caId = 20;

			var objectToUpdate = new G1
			{
				GravityLevel2Childs = new[]
				{
					new G2c { ArtifactId = g2caId, Name = "G2cA" }, //exists
					new G2c { ArtifactId = 0, Name = "G2cB" } //new			
				}
			};
			//no actions done on child objects, because recursion off
			UpdateObject(objectToUpdate, rdo => true, ObjectFieldsDepthLevel.OnlyParentObject);
			CollectionAssert.AreEqual(new[] { g2caId, 0 }, objectToUpdate.GravityLevel2Childs.Select(x => x.ArtifactId));
		}

		[Test]
		public void Update_ChildObject_UpdateInsertNewRemove_WithRecursion()
		{
			const int g2caId = 20;
			const int g2cbId = 30;
			const int g2ccId = 40;

			var objectToUpdate = new G1
			{
				GravityLevel2Childs = new[]
				{
					new G2c { ArtifactId = g2caId, Name = "G2cA" }, //exists
					new G2c { ArtifactId = 0, Name = "G2cB" } //new			
				}
			};

			RdoBoolCond matchingG2caExpression = rdo =>
				rdo.ArtifactID == g2caId 
					&& rdo[FieldGuid<G2c>(nameof(G2c.Name))].ValueAsFixedLengthText == "G2cA"
					&& rdo.ParentArtifact.ArtifactID == G1ArtifactId;
			RdoBoolCond matchingG2cbExpression = rdo =>
				rdo.ArtifactID == 0 
					&& rdo[FieldGuid<G2c>(nameof(G2c.Name))].ValueAsFixedLengthText == "G2cB"
					&& rdo.ParentArtifact.ArtifactID == G1ArtifactId;

			SetupChildQuery(g2caId, g2cbId);
			SetupDeleteChild();
			SetupMultipleLevelChildQuery(g2caId,g2cbId);
			SetupUpdateManyCondition(x => x.Count == 1 && matchingG2caExpression(x[0]));
			SetupInsertManyCondition(x => x.Count == 1 && matchingG2cbExpression(x[0]), g2cbId);
			mockProvider.Setup(x => x.ReadSingle(g2ccId)).Returns(GetStubRDO<G2c>(40)); //object is read to check for any children to delete
			mockProvider.Setup(x => x.Delete(It.Is<List<int>>(y => y.Single() == g2ccId)))
				.Returns(new RDO[0].ToSuccessResultSet<WriteResultSet<RDO>>());

			UpdateObject(objectToUpdate, rdo => true, ObjectFieldsDepthLevel.FirstLevelOnly);
			CollectionAssert.AreEqual(
				new[] { g2caId, g2cbId }, 
				objectToUpdate.GravityLevel2Childs.Select(x => x.ArtifactId));

		}
		
		void UpdateObject(G1 objectToUpdate, RdoBoolCond rootExpression, ObjectFieldsDepthLevel depthLevel)
		{
			objectToUpdate.ArtifactId = G1ArtifactId;
			SetupUpdateManyCondition(x => x.Count == 1 && x[0].ArtifactID == G1ArtifactId && rootExpression(x[0]));

			//setup clearing the non-present file
			mockProvider.Setup(x => x.Read(It.Is<RDO[]>(y => y.Single().Guids.Single() == FieldGuid<G1>(nameof(G1.FileField)))))
				.Returns(new[] { new RDO(FileFieldId) }.ToSuccessResultSet<WriteResultSet<RDO>>());
			mockProvider.Setup(x => x.ClearFile(FileFieldId, G1ArtifactId));

			new RsapiDao(mockProvider.Object, null).Update(objectToUpdate, depthLevel);
		}

		void UpdateObjectWithFileField(G1 objectToUpdate, RdoBoolCond rootExpression, ObjectFieldsDepthLevel depthLevel)
		{
			objectToUpdate.ArtifactId = G1ArtifactId;
			SetupUpdateManyCondition(x => x.Count == 1 && x[0].ArtifactID == G1ArtifactId && rootExpression(x[0]));

			//setup clearing the non-present file
			mockProvider
				.Setup(x => x.Read(It.Is<RDO[]>(y => y.Single().Guids.Single() == FieldGuid<G1>(nameof(G1.FileField)))))
				.Returns(new[] { new RDO(FileFieldId) }.ToSuccessResultSet<WriteResultSet<RDO>>());
			mockProvider
				.Setup(x => x.ClearFile(FileFieldId, G1ArtifactId));
			mockProvider
				.Setup(x => x.UploadFile(FileFieldId, 10, Path.Combine(Path.GetTempPath(), "ByteArrayFileDto")));
			mockProvider
				.Setup(x => x.UploadFile(FileFieldId, 10, Path.Combine(Path.GetTempPath(), "NewName")));
			InvokeWithRetrySettings invokeWithRetrySettings = new InvokeWithRetrySettings(SharedConstants.retryAttempts,
				SharedConstants.sleepTimeInMiliseconds);
			new RsapiDao(mockProvider.Object, new InvokeWithRetryService(invokeWithRetrySettings)).Update(objectToUpdate, depthLevel);
		}
		
		private void SetupSingleObjectQuery(params int[] resultArtifactIds)
		{
			mockProvider.Setup(x =>
					x.Query(It.Is<Query<RDO>>(
						y => y.ArtifactTypeGuid == BaseDto.GetObjectTypeGuid<GravityLevel3Child>()
						     && ((WholeNumberCondition)y.Condition).Value.Single() == G1ArtifactId)))
				.Returns(new[] { resultArtifactIds.Select(y => new RDO(y)).ToSuccessResultSet<QueryResultSet<RDO>>() });
		}
		
		//this is needed whenever recursion is turned on
		private void SetupChildQuery(params int[] resultArtifactIds)
		{
			mockProvider.Setup(x =>
					x.Query(It.Is<Query<RDO>>(
						y => y.ArtifactTypeGuid == BaseDto.GetObjectTypeGuid<G2c>()
						     && ((WholeNumberCondition)y.Condition).Value.Single() == G1ArtifactId)))
				.Returns(new[] {resultArtifactIds.Select(y => new RDO(y)).ToSuccessResultSet<QueryResultSet<RDO>>()});
		}

		private void SetupMultipleLevelChildQuery(params int[] level3ArtifactIds)
		{
			mockProvider.Setup(x =>
					x.Query(It.Is<Query<RDO>>(
						y => y.ArtifactTypeGuid == BaseDto.GetObjectTypeGuid<GravityLevel3Child>())))
				.Returns(new[] { level3ArtifactIds.Select(y => new RDO(y)).ToSuccessResultSet<QueryResultSet<RDO>>() });
		}

		private void SetupDeleteChild()
		{
			mockProvider.Setup(x => x.Delete(It.IsAny<List<int>>()))
				.Returns(new RDO[0].ToSuccessResultSet<WriteResultSet<RDO>>());
		}
		
		public void SetupInsertManyCondition(Func<List<RDO>, bool> condition, params int[] resultIds)
		{
			mockProvider
				.Setup(x => x.Create(It.Is<List<RDO>>(y => condition(y))))
				.Returns(resultIds.Select(x => new RDO(x)).ToSuccessResultSet<WriteResultSet<RDO>>());
		}
		
		public void SetupUpdateManyCondition(Func<List<RDO>, bool> condition)
		{
			mockProvider
				.Setup(x => x.Update(It.Is<List<RDO>>(y => condition(y))))
				.Returns(new RDO[0].ToSuccessResultSet<WriteResultSet<RDO>>());
		}
	}
}
