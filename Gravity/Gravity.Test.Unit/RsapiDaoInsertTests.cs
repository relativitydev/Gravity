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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using G1 = Gravity.Test.TestClasses.GravityLevelOne;
using G2 = Gravity.Test.TestClasses.GravityLevel2;
using G3 = Gravity.Test.TestClasses.GravityLevel3;
using G2c = Gravity.Test.TestClasses.GravityLevel2Child;
using RdoCondition = System.Func<kCura.Relativity.Client.DTOs.RDO, bool>;

using static Gravity.Test.Helpers.TestObjectHelper;
using Gravity.Utils;
using Gravity.Globals;

namespace Gravity.Test.Unit
{
	public class RsapiDaoInsertTests
	{
		private const int FileFieldId = 44;
		Mock<IRsapiProvider> mockProvider;

		[SetUp]
		public void Init()
		{
			//ensure fail if method not defined
			mockProvider = new Mock<IRsapiProvider>(MockBehavior.Strict);
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

			//checks that matches inserted object
			RdoCondition matchingRdoCondition = rdo =>
					rdo[FieldGuid<G1>(nameof(G1.BoolField))].Value.Equals(true)
				&& rdo[FieldGuid<G1>(nameof(G1.CurrencyField))].Value.Equals(.5M)
				&& rdo[FieldGuid<G1>(nameof(G1.DateTimeField))].Value.Equals(new DateTime(2000, 1, 1))
				&& rdo[FieldGuid<G1>(nameof(G1.DecimalField))].Value.Equals(.6M)
				&& rdo[FieldGuid<G1>(nameof(G1.FixedTextField))].Value.Equals("FixedText")
				&& rdo[FieldGuid<G1>(nameof(G1.IntegerField))].Value.Equals(2)
				&& rdo[FieldGuid<G1>(nameof(G1.LongTextField))].Value.Equals("LongText");

			InsertObject(objectToInsert, matchingRdoCondition, ObjectFieldsDepthLevel.OnlyParentObject);
		}

		[Test]
		public void Insert_SingleChoice()
		{
			var objectToInsert = new G1
			{
				SingleChoice = SingleChoiceFieldChoices.SingleChoice2
			};

			//checks that matches inserted object
			RdoCondition matchingRdoCondition = rdo =>
				rdo[FieldGuid<G1>(nameof(G1.SingleChoice))].ValueAsSingleChoice.Guids.Single() 
					== SingleChoiceFieldChoices.SingleChoice2.GetRelativityObjectAttributeGuidValue();

			InsertObject(objectToInsert, matchingRdoCondition, ObjectFieldsDepthLevel.OnlyParentObject);
		}

		[Test]
		public void Insert_MultipleChoice()
		{
			var objectToInsert = new G1
			{
				MultipleChoiceFieldChoices = new[] { MultipleChoiceFieldChoices.MultipleChoice2, MultipleChoiceFieldChoices.MultipleChoice3 }
			};

			//checks that matches inserted object
			RdoCondition matchingRdoCondition = rdo =>
				Enumerable.SequenceEqual(
					rdo[FieldGuid<G1>(nameof(G1.MultipleChoiceFieldChoices))].ValueAsMultipleChoice.Select(x => x.Guids.Single()),				
					new[] { MultipleChoiceFieldChoices.MultipleChoice2, MultipleChoiceFieldChoices.MultipleChoice3 }
						.Select(x => x.GetRelativityObjectAttributeGuidValue()));

			InsertObject(objectToInsert, matchingRdoCondition, ObjectFieldsDepthLevel.OnlyParentObject);
		}

		[Test]
		public void Insert_FileField()
		{
			var objectToInsert = new G1 
			{
				ArtifactId = 10,
				FileField = new ByteArrayFileDto {
					FileName = "ByteArrayFileDto",
					ByteArray = new byte[] { 65 }
				}
			};

			//checks that matches inserted object
			RdoCondition matchingRdoCondition = rdo => rdo.ArtifactID == 10;
			InsertObjectContainingFileField(objectToInsert, matchingRdoCondition, ObjectFieldsDepthLevel.FirstLevelOnly);
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
			RdoCondition matchingG2Condition = rdo => 
				rdo.Fields.Any(f => f.Guids.Contains(FieldGuid<G2>(nameof(G2.Name))) && f.ValueAsFixedLengthText == "G2");

			//G1 contains G2 object
			RdoCondition matchingG1Condition = rdo =>
				rdo.Fields.Any(f => f.Guids.Contains(FieldGuid<G1>(nameof(G1.GravityLevel2Obj))) && f.ValueAsSingleObject.ArtifactID == g2Id);

			SetupInsertManyCondition(x => matchingG2Condition(x.Single()), g2Id);

			InsertObject(objectToInsert, matchingG1Condition, ObjectFieldsDepthLevel.FirstLevelOnly);

			Assert.AreEqual(g2Id, objectToInsert.GravityLevel2Obj.ArtifactId);
		}

		[Test]
		public void Insert_NewSingleObject_NonRecursive()
		{
			var objectToInsert = new G1
			{
				GravityLevel2Obj = new G2 { Name = "G2" }
			};

			//since only parent object, G2 object never inserted
			RdoCondition matchingG1Condition = rdo => rdo[FieldGuid<G1>(nameof(G1.GravityLevel2Obj))].Value == null;

			InsertObject(objectToInsert, matchingG1Condition, ObjectFieldsDepthLevel.OnlyParentObject);

			Assert.AreEqual(0, objectToInsert.GravityLevel2Obj.ArtifactId);
		}

		[Test]
		public void Insert_ExistingSingleObject_DontUpdateFields()
		{
			const int g2Id = 20;

			var objectToInsert = new G1
			{
				GravityLevel2Obj = new G2 { ArtifactId = g2Id, Name = "G2A" }
			};

			//even though recursive, will be no update of G2 object because already exists
			RdoCondition matchingG1Condition = rdo => rdo[FieldGuid<G1>(nameof(G1.GravityLevel2Obj))].ValueAsSingleObject.ArtifactID == g2Id;
			InsertObject(objectToInsert, matchingG1Condition, ObjectFieldsDepthLevel.FirstLevelOnly);

			Assert.AreEqual(g2Id, objectToInsert.GravityLevel2Obj.ArtifactId);
		}

		[Test]
		public void Insert_ExistingSingleObject_DontInsertChildren()
		{
			const int g2Id = 20;
			const int g3Id = 30;

			var objectToInsert = new G1 
			{
				GravityLevel2Obj = new G2 
				{
					ArtifactId = g2Id,
					Name = "G2A",
					GravityLevel3SingleObj = new G3 
					{
						ArtifactId = g3Id,
						Name = "G3A"
					}
				}
			};

			RdoCondition matchingG1Condition = rdo => rdo[FieldGuid<G1>(nameof(G1.GravityLevel2Obj))].ValueAsSingleObject.ArtifactID == g2Id;
			RdoCondition rdoCondition = rdo =>
				rdo[FieldGuid<G1>(nameof(G1.GravityLevel2Obj))]
					.ValueAsSingleObject[FieldGuid<G2>(nameof(G2.GravityLevel3SingleObj))].ArtifactID == g3Id;
			SetupInsertManyCondition(x => x.Count == 1 && rdoCondition(x[0]));
			InsertObject(objectToInsert, matchingG1Condition, ObjectFieldsDepthLevel.FirstLevelOnly);
		}

		[Test]
		public void Insert_NewMultipleObject_Recursive()
		{
			const int g2aId = 20;
			const int g2bId = 30;

			var objectToInsert = new G1
			{
				GravityLevel2MultipleObjs = new[] { new G2 { Name = "G2A" }, new G2 { Name = "G2B" } }
			};

			//G2a,b objects created with their fields
			Func<RDO, string, bool> g2Func = (rdo, name) =>
			   rdo.Fields.Any(f => f.Guids.Contains(FieldGuid<G2>(nameof(G2.Name))) && f.ValueAsFixedLengthText == name);

			//parent RDO contains both items
			RdoCondition matchingG1Condition = rdo =>
				rdo.Fields.Any(f => f.Guids.Contains(FieldGuid<G1>(nameof(G1.GravityLevel2MultipleObjs)))
					&& f.GetValueAsMultipleObject<Artifact>().Select(x => x.ArtifactID).SequenceEqual(new[] { g2aId, g2bId }));

			SetupInsertManyCondition(
				rdos => g2Func(rdos[0], "G2A") && g2Func(rdos[1], "G2B") && rdos.Count == 2,
				g2aId, g2bId
			);
			
			InsertObject(objectToInsert, matchingG1Condition, ObjectFieldsDepthLevel.FirstLevelOnly);

			CollectionAssert.AreEqual(new[] { g2aId, g2bId }, objectToInsert.GravityLevel2MultipleObjs.Select(x => x.ArtifactId));
		}

		[Test]
		public void Insert_NewMultipleObject_NonRecursive()
		{
			var objectToInsert = new G1
			{
				GravityLevel2MultipleObjs = new[] { new G2 { Name = "G2A" }, new G2 { Name = "G2B" } }
			};

			//parent RDO doesn't insert these objects
			RdoCondition matchingG1Condition = rdo =>
				rdo.Fields.Any(f => !f.Guids.Contains(FieldGuid<G1>(nameof(G1.GravityLevel2MultipleObjs))));

			InsertObject(objectToInsert, matchingG1Condition, ObjectFieldsDepthLevel.OnlyParentObject);

			CollectionAssert.AreEqual(new[] { 0, 0 }, objectToInsert.GravityLevel2MultipleObjs.Select(x => x.ArtifactId));
		}

		[Test]
		public void Insert_ExistingMultipleObject_DontUpdateFields()
		{
			const int g2aId = 20;
			const int g2bId = 30;

			var objectToInsert = new G1
			{
				GravityLevel2MultipleObjs = new[] 
				{
					new G2 { ArtifactId = g2aId, Name = "G2A" },
					new G2 { ArtifactId = g2bId, Name = "G2B" }
				}
			};

			//only create parent object, not children
			RdoCondition matchingG1Condition = rdo =>
				rdo.Fields.Any(f => f.Guids.Contains(FieldGuid<G1>(nameof(G1.GravityLevel2MultipleObjs)))
					&& f.GetValueAsMultipleObject<Artifact>().Select(x => x.ArtifactID).SequenceEqual(new[] { g2aId, g2bId }));

			InsertObject(objectToInsert, matchingG1Condition, ObjectFieldsDepthLevel.FullyRecursive);
		}

		[Test]
		public void Insert_ExistingMultipleObject_DontInsertChildren()
		{
			const int g2aId = 20;
			const int g2bId = 30;
			const int g3aId = 40;
			const int g3bId = 50;
			var objectToInsert = new G1
			{
				GravityLevel2MultipleObjs = new[]
				{
					new G2 
					{
						ArtifactId = g2aId,
						Name = "G2A",
						GravityLevel3SingleObj = new G3 
						{
							ArtifactId = g3aId,
							Name = "G3A"
						}
					},
					new G2 {
						ArtifactId = g2bId,
						Name = "G2B",
						GravityLevel3SingleObj = new G3
						{
							ArtifactId = g3bId,
							Name = "G3B"
						}
					}
				}
			};

			RdoCondition matchingG1Condition = rdo =>
				rdo.Fields.Any(f => f.Guids.Contains(FieldGuid<G1>(nameof(G1.GravityLevel2MultipleObjs)))
				    && f.GetValueAsMultipleObject<Artifact>().Select(x => x.ArtifactID).SequenceEqual(new[] { g2aId, g2bId }));
			RdoCondition rdoCondition1 = rdo =>
				rdo[FieldGuid<G1>(nameof(G1.GravityLevel2MultipleObjs))].GetValueAsMultipleObject<Artifact>()
					.Select(x => x[FieldGuid<G2>(nameof(G2.GravityLevel3SingleObj))].ArtifactID)
					.SequenceEqual(new[] {g3aId, g3bId});
			SetupInsertManyCondition(x => x.Count == 1 && rdoCondition1(x[0]));
			InsertObject(objectToInsert, matchingG1Condition, ObjectFieldsDepthLevel.FullyRecursive);
		}

		[Test]
		public void Insert_ChildObject_Recursive()
		{
			const int g2caId = 20;
			const int g2cbId = 30;

			var objectToInsert = new G1
			{
				GravityLevel2Childs = new[] { new G2c { Name = "G2cA" }, new G2c { Name = "G2cB" } }
			};

			//G2ca,b objects created with their fields
			Func <RDO, string, bool> g2cFunc = (rdo, name) =>
				rdo.ParentArtifact.ArtifactID == 10
					&& rdo.Fields.Any(f => f.Guids.Contains(FieldGuid<G2c>(nameof(G2c.Name))) && f.ValueAsFixedLengthText == name);

			//parent RDO contains both items
			RdoCondition matchingG1Condition = rdo => rdo.ArtifactTypeGuids.Contains(BaseDto.GetObjectTypeGuid<G1>());

			SetupInsertManyCondition(
				rdos => g2cFunc(rdos[0], "G2cA") && g2cFunc(rdos[1], "G2cB") && rdos.Count == 2,
				g2caId, g2cbId
			);

			InsertObject(objectToInsert, matchingG1Condition, ObjectFieldsDepthLevel.FirstLevelOnly);

			CollectionAssert.AreEqual(new[] { g2caId, g2cbId }, objectToInsert.GravityLevel2Childs.Select(x => x.ArtifactId));
		}

		[Test]
		public void Insert_ChildObject_NonRecursive()
		{
			const int g2caId = 20;
			const int g2cbId = 30;

			var objectToInsert = new G1
			{
				GravityLevel2Childs = new[]
				{
					new G2c { ArtifactId = g2caId, Name = "G2cA" },
					new G2c { ArtifactId = g2cbId, Name = "G2cB" }
				}
			};

			//dont create children since non-recursive
			RdoCondition matchingG1Condition = rdo => rdo.ArtifactTypeGuids.Contains(BaseDto.GetObjectTypeGuid<G1>());

			InsertObject(objectToInsert, matchingG1Condition, ObjectFieldsDepthLevel.OnlyParentObject);
		}

		void InsertObject(G1 objectToInsert, RdoCondition rootCondition, ObjectFieldsDepthLevel depthLevel)
		{
			SetupInsertManyCondition(x => x.Count == 1 && rootCondition(x.Single()), 10);
			var insertedId = new RsapiDao(mockProvider.Object, null).Insert(objectToInsert, depthLevel);
			Assert.AreEqual(10, insertedId);
			Assert.AreEqual(10, objectToInsert.ArtifactId);
		}

		public void SetupInsertManyCondition(Func<List<RDO>, bool> condition, params int[] resultIds)
		{
			mockProvider
				.Setup(x => x.Create(It.Is<List<RDO>>(y => condition(y))))
				.Returns(resultIds.Select(x => new RDO(x)).ToSuccessResultSet<WriteResultSet<RDO>>());
		}

		void InsertObjectContainingFileField(G1 objectToInsert, RdoCondition rootCondition, ObjectFieldsDepthLevel depthLevel)
		{
			SetupInsertManyCondition(x => x.Count == 1 && rootCondition(x.Single()), 10);

			mockProvider
				.Setup(x => x.Read(It.Is<RDO[]>(y => y.Single().Guids.Single() == FieldGuid<G1>(nameof(G1.FileField)))))
				.Returns(new[] { new RDO(FileFieldId) }.ToSuccessResultSet<WriteResultSet<RDO>>());

			mockProvider
				.Setup(x => x.UploadFile(FileFieldId, 10,
					Path.Combine(Path.GetTempPath(), "ByteArrayFileDto")));

			InvokeWithRetrySettings invokeWithRetrySettings = new InvokeWithRetrySettings(SharedConstants.retryAttempts,
				SharedConstants.sleepTimeInMiliseconds);
			var insertedId = new RsapiDao(mockProvider.Object, new InvokeWithRetryService(invokeWithRetrySettings))
				.Insert(objectToInsert, depthLevel);
			Assert.AreEqual(10, insertedId);
			Assert.AreEqual(10, objectToInsert.ArtifactId);
		}
	}
}
