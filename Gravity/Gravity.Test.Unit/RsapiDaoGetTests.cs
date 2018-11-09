using Gravity.Base;
using Gravity.DAL.RSAPI;
using Gravity.DAL.RSAPI.Tests;
using Gravity.Test.Helpers;
using Gravity.Test.TestClasses;
using kCura.Relativity.Client;
using kCura.Relativity.Client.DTOs;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Artifact = kCura.Relativity.Client.DTOs.Artifact;
using Choice = kCura.Relativity.Client.DTOs.Choice;

namespace Gravity.Test.Unit
{
	public class RsapiDaoGetTests
	{
		private const int RootArtifactID = 1111111;

		[Test]
		public void Get_BlankRDO()
		{
			var dao = new RsapiDao(GetChoiceRsapiProvider(null, null), null);
			var dto = dao.Get<GravityLevelOne>(RootArtifactID, Base.ObjectFieldsDepthLevel.FirstLevelOnly);
			Assert.AreEqual(RootArtifactID, dto.ArtifactId);
		}

		[Test]
		public void Get_MultiObject_FirstLevelOnly()
		{
			//test MultiObject fields with one level of recursion
			int[] multiObjectIds = new int[] { 1 , 2 , 3 };
			int[] singleObjectLevel3ArtifactIds = new int[] { 5 , 6 , 7 };
			var dao = new RsapiDao(GetMultipleObjectRsapiProvider(multiObjectIds, singleObjectLevel3ArtifactIds), null);
			var dto = dao.Get<GravityLevelOne>(RootArtifactID, Base.ObjectFieldsDepthLevel.FirstLevelOnly);
			for(int i = 0; i < multiObjectIds.Length; i++)
			{
				Assert.AreEqual(multiObjectIds[i], dto.GravityLevel2MultipleObjs.ElementAt(i).ArtifactId);
				Assert.IsNull(dto.GravityLevel2MultipleObjs.ElementAt(i).GravityLevel3SingleObj);
			}
		}

		[Test]
		public void Get_MultiObject_Recursive()
		{
			//test MultiObject fields with varying degrees of recursion
			int[] multiObjectIds = new int[] { 1, 2, 3 };
			int[] singleObjectLevel3ArtifactIds = new int[] { 5 , 6 , 7 };
			var dao = new RsapiDao(GetMultipleObjectRsapiProvider(multiObjectIds, singleObjectLevel3ArtifactIds), null);
			var dto = dao.Get<GravityLevelOne>(RootArtifactID, Base.ObjectFieldsDepthLevel.FullyRecursive);
			for (int i = 0; i < multiObjectIds.Length; i++)
			{
				Assert.AreEqual(multiObjectIds[i], dto.GravityLevel2MultipleObjs.ElementAt(i).ArtifactId);
				Assert.AreEqual(singleObjectLevel3ArtifactIds[i], dto.GravityLevel2MultipleObjs.ElementAt(i).GravityLevel3SingleObj.ArtifactId);
			}
		}

		[Test]
		public void Get_ChildObjectList_FirstLevelOnly()
		{
			//test ChildObject fields with varying degrees of recursion
			int level2ChildObjectId = 1;
			int singleObjectLevel3Id = 2;
			int level3ChildObjectId = 3;
			var dao = new RsapiDao(GetChildObjectRsapiProvider(level2ChildObjectId, singleObjectLevel3Id, level3ChildObjectId), null);
			var dto = dao.Get<GravityLevelOne>(RootArtifactID, Base.ObjectFieldsDepthLevel.FirstLevelOnly);
			Assert.AreEqual(level2ChildObjectId, dto.GravityLevel2Childs[0].ArtifactId);
			Assert.IsNull(dto.GravityLevel2Childs[0].GravityLevel3Obj);
			Assert.AreEqual(0,dto.GravityLevel2Childs[0].GravityLevel3Childs.Count);
		}

		[Test]
		public void Get_ChildObjectList_Recursive()
		{
			//test ChildObject fields with varying degrees of recursion
			int level2ChildObjectId = 1;
			int singleObjectLevel3Id = 2;
			int level3ChildObjectId = 3;
			var dao = new RsapiDao(GetChildObjectRsapiProvider(level2ChildObjectId, singleObjectLevel3Id, level3ChildObjectId), null);
			var dto = dao.Get<GravityLevelOne>(RootArtifactID, Base.ObjectFieldsDepthLevel.FullyRecursive);
			Assert.AreEqual(level2ChildObjectId, dto.GravityLevel2Childs[0].ArtifactId);
			Assert.AreEqual(singleObjectLevel3Id, dto.GravityLevel2Childs[0].GravityLevel3Obj.ArtifactId);
			Assert.AreEqual(level3ChildObjectId, dto.GravityLevel2Childs[0].GravityLevel3Childs[0].ArtifactId);
		}

		[Test]
		public void Get_SingleObject_FirstLevelOnly()
		{
			//test single object fields with one level of recursion
			int singleObjectLevel2ArtifactId = 1;
			int singleObjectLevel3ArtifactId = 2;
			var dao = new RsapiDao(GetObjectRsapiProvider(singleObjectLevel2ArtifactId, singleObjectLevel3ArtifactId), null);
			var dto = dao.Get<GravityLevelOne>(RootArtifactID, Base.ObjectFieldsDepthLevel.FirstLevelOnly);
			Assert.AreEqual(singleObjectLevel2ArtifactId, dto.GravityLevel2Obj.ArtifactId);
			Assert.IsNull(dto.GravityLevel2Obj.GravityLevel3SingleObj);
		}

		[Test]
		public void Get_SingleObject_Recursive()
		{
			//test single object fields with varying degrees of recursion
			int singleObjectLevel2ArtifactId = 25;
			int singleObjectLevel3ArtifactId = 26;
			var dao = new RsapiDao(GetObjectRsapiProvider(singleObjectLevel2ArtifactId, singleObjectLevel3ArtifactId), null);
			var dto = dao.Get<GravityLevelOne>(RootArtifactID, Base.ObjectFieldsDepthLevel.FullyRecursive);
			Assert.AreEqual(singleObjectLevel2ArtifactId, dto.GravityLevel2Obj.ArtifactId);
			Assert.AreEqual(singleObjectLevel3ArtifactId, dto.GravityLevel2Obj.GravityLevel3SingleObj.ArtifactId);
		}

		[Test]
		public void Get_DownloadsFileContents()
		{
			var fileArray = new byte[] { 2 };
			var fileName = "filename.dat";
			var dao = new RsapiDao(GetFileRsapiProvider(fileName, fileArray), null);
			var dto = dao.Get<GravityLevelOne>(RootArtifactID, Base.ObjectFieldsDepthLevel.OnlyParentObject);
			CollectionAssert.AreEqual(fileArray, ((ByteArrayFileDto)dto.FileField).ByteArray);
			Assert.AreEqual(fileName, ((ByteArrayFileDto)dto.FileField).FileName);
		}

		[Test]
		public void Get_SkipsDownloadIfNoFile()
		{
			var dao = new RsapiDao(GetFileRsapiProvider(null, null), null);
			var dto = dao.Get<GravityLevelOne>(RootArtifactID, Base.ObjectFieldsDepthLevel.OnlyParentObject);
			Assert.Null(dto.FileField);
		}

		[Test]
		public void Get_SingleChoice_InEnum()
		{
			var dao = new RsapiDao(GetChoiceRsapiProvider(2, null), null);
			var dto = dao.Get<GravityLevelOne>(RootArtifactID, Base.ObjectFieldsDepthLevel.FirstLevelOnly);
			Assert.AreEqual(SingleChoiceFieldChoices.SingleChoice2, dto.SingleChoice);
		}

		[Test]
		public void Get_SingleChoice_NotInEnum()
		{
			var dao = new RsapiDao(GetChoiceRsapiProvider(5, null), null);
			Assert.Throws<InvalidOperationException>(() => dao.Get<GravityLevelOne>(RootArtifactID, Base.ObjectFieldsDepthLevel.FirstLevelOnly));
		}

		[Test]
		public void Get_MultipleChoice_AllInEnum()
		{
			var dao = new RsapiDao(GetChoiceRsapiProvider(null, new[] { 11, 13 }), null);
			var dto = dao.Get<GravityLevelOne>(RootArtifactID, Base.ObjectFieldsDepthLevel.FirstLevelOnly);
			CollectionAssert.AreEquivalent(
				new[] { MultipleChoiceFieldChoices.MultipleChoice1, MultipleChoiceFieldChoices.MultipleChoice3 },
				dto.MultipleChoiceFieldChoices
			);
		}

		[Test]
		public void Get_MultipleChoice_NotAllInEnum()
		{
			//first item is in an enum, but not in our enum
			var dao = new RsapiDao(GetChoiceRsapiProvider(null, new[] { 3, 13 }), null);
			Assert.Throws<InvalidOperationException>(() => dao.Get<GravityLevelOne>(RootArtifactID, Base.ObjectFieldsDepthLevel.FirstLevelOnly));

		}

		private IRsapiProvider GetFileRsapiProvider(string fileName, byte[] result)
		{
			const int fileFieldId = 20;

			var providerMock = new Mock<IRsapiProvider>(MockBehavior.Strict);
			var fileGuid = typeof(GravityLevelOne)
				.GetProperty(nameof(GravityLevelOne.FileField))
				.GetCustomAttribute<RelativityObjectFieldAttribute>()
				.FieldGuid;

			var rdo = TestObjectHelper.GetStubRDO<GravityLevelOne>(RootArtifactID);
			rdo[fileGuid].Value = fileName;

			providerMock.Setup(x => x.Read(RootArtifactID)).ReturnsResultSet(rdo);

			if (fileName != null)
			{ 
				providerMock.Setup(x => x.Read(It.Is<RDO[]>(y => y.Single().Guids.Contains(fileGuid))))
					.ReturnsResultSet(new RDO(fileFieldId));
				providerMock.Setup(x => x.DownloadFile(fileFieldId, RootArtifactID))
					.Returns(Tuple.Create(
						new FileMetadata { FileName = fileName },
						new MemoryStream(result)));
			}
			return providerMock.Object;
		}

		private IRsapiProvider GetChoiceRsapiProvider(int? singleChoiceId, int[] multipleChoiceIds)
		{
			var providerMock = new Mock<IRsapiProvider>(MockBehavior.Strict);

			// setup the RDO Read

			var multipleGuid = typeof(GravityLevelOne)
				.GetProperty(nameof(GravityLevelOne.MultipleChoiceFieldChoices))
				.GetCustomAttribute<RelativityObjectFieldAttribute>()
				.FieldGuid;

			var singleGuid = typeof(GravityLevelOne)
				.GetProperty(nameof(GravityLevelOne.SingleChoice))
				.GetCustomAttribute<RelativityObjectFieldAttribute>()
				.FieldGuid;

			var rdo = TestObjectHelper.GetStubRDO<GravityLevelOne>(RootArtifactID);
			rdo[singleGuid].ValueAsSingleChoice = singleChoiceId == null ? null : new Choice(singleChoiceId.Value);
			rdo[multipleGuid].ValueAsMultipleChoice = multipleChoiceIds?.Select(x => new Choice(x)).ToList() ?? new List<Choice>();

			providerMock.Setup(x => x.Read(RootArtifactID)).ReturnsResultSet(rdo);

			// setup the child object query
			providerMock.Setup(x => x.Query(It.IsAny<Query<RDO>>())).ReturnsResultSet();

			// setup the choice query

			// results in ArtifactIDs 1, 2, 3
			var singleChoiceGuids = ChoiceCacheTests.GetOrderedGuids<SingleChoiceFieldChoices>();
			providerMock.Setup(ChoiceCacheTests.SetupExpr(singleChoiceGuids)).ReturnsResultSet(ChoiceCacheTests.GetResults(singleChoiceGuids, 1));
			// results in ArtifactIDs 11, 12, 13
			var multiChoiceGuids = ChoiceCacheTests.GetOrderedGuids<MultipleChoiceFieldChoices>();
			providerMock.Setup(ChoiceCacheTests.SetupExpr(multiChoiceGuids)).ReturnsResultSet(ChoiceCacheTests.GetResults(multiChoiceGuids, 11));

			return providerMock.Object;
		}

		private IRsapiProvider GetObjectRsapiProvider(int singleLevel2ArtifactId, int singleLevel3ArtifactId)
		{
			var providerMock = new Mock<IRsapiProvider>(MockBehavior.Strict);

			// setup the RDO Read
			var singleLevel1Guid = typeof(GravityLevelOne)
				.GetProperty(nameof(GravityLevelOne.GravityLevel2Obj))
				.GetCustomAttribute<RelativityObjectFieldAttribute>()
				.FieldGuid;

			var singleLevel2Guid = typeof(GravityLevel2)
				.GetProperty(nameof(GravityLevel2.GravityLevel3SingleObj))
				.GetCustomAttribute<RelativityObjectFieldAttribute>()
				.FieldGuid;

			var level1Rdo = TestObjectHelper.GetStubRDO<GravityLevelOne>(RootArtifactID);
			var level2Rdo = TestObjectHelper.GetStubRDO<GravityLevel2>(singleLevel2ArtifactId);
			var level3Rdo = TestObjectHelper.GetStubRDO<GravityLevel3>(singleLevel3ArtifactId);

			level1Rdo[singleLevel1Guid].ValueAsSingleObject = level2Rdo;
			level2Rdo[singleLevel2Guid].ValueAsSingleObject = level3Rdo;
			providerMock.Setup(x => x.Read(RootArtifactID)).ReturnsResultSet(level1Rdo);
			providerMock.Setup(x => x.Read(singleLevel2ArtifactId)).ReturnsResultSet(level2Rdo);
			providerMock.Setup(x => x.Read(singleLevel3ArtifactId)).ReturnsResultSet(level3Rdo);

			// setup the child object query
			providerMock.Setup(x => x.Query(It.IsAny<Query<RDO>>())).ReturnsResultSet();
		
			return providerMock.Object;
		}

		private IRsapiProvider GetMultipleObjectRsapiProvider(int[] multipleObjectIds, int[] singleLevel3ArtifactIds)
		{
			var providerMock = new Mock<IRsapiProvider>(MockBehavior.Strict);

			// setup the RDO Read
			var multipleLevel1Guid = typeof(GravityLevelOne)
				.GetProperty(nameof(GravityLevelOne.GravityLevel2MultipleObjs))
				.GetCustomAttribute<RelativityObjectFieldAttribute>()
				.FieldGuid;

			var singleLevel2Guid = typeof(GravityLevel2)
				.GetProperty(nameof(GravityLevel2.GravityLevel3SingleObj))
				.GetCustomAttribute<RelativityObjectFieldAttribute>()
				.FieldGuid;

			var level1Rdo = TestObjectHelper.GetStubRDO<GravityLevelOne>(RootArtifactID);
			providerMock.Setup(x => x.Read(RootArtifactID)).ReturnsResultSet(level1Rdo);

			var results = new List<RDO>();
			var level3StubRdos = singleLevel3ArtifactIds.Select(TestObjectHelper.GetStubRDO<GravityLevel3>).ToList();

			FieldValueList<Artifact> fieldValueList = new FieldValueList<Artifact>();
			for (int i = 0; i < multipleObjectIds.Length; i++)
			{
				var level2MultipleObjectRdo = TestObjectHelper.GetStubRDO<GravityLevel2>(multipleObjectIds[i]);
				fieldValueList.Add(level2MultipleObjectRdo);
				results.Add(level2MultipleObjectRdo);

				var level3SingleRdo = level3StubRdos[i];
				level2MultipleObjectRdo[singleLevel2Guid].ValueAsSingleObject = level3SingleRdo;
				providerMock.Setup(x => x.Read(singleLevel3ArtifactIds[i])).ReturnsResultSet(level3SingleRdo);
			}

			level1Rdo[multipleLevel1Guid].SetValueAsMultipleObject<Artifact>(fieldValueList);

			providerMock.Setup(x => x.Read(singleLevel3ArtifactIds)).ReturnsResultSet(level3StubRdos);
			providerMock.Setup(x => x.Read(multipleObjectIds)).ReturnsResultSet(results);

			// setup the child object query
			providerMock.Setup(x => x.Query(It.IsAny<Query<RDO>>())).ReturnsResultSet();

			return providerMock.Object;
		}

		private IRsapiProvider GetChildObjectRsapiProvider(int level2ChildObjectId, int singleObjectLevel3Id, int level3ChildObjectId)
		{
			var providerMock = new Mock<IRsapiProvider>(MockBehavior.Strict);

			// setup the RDO Read
			var singleLevel2Guid = typeof(GravityLevel2Child)
				.GetProperty(nameof(GravityLevel2Child.GravityLevel3Obj))
				.GetCustomAttribute<RelativityObjectFieldAttribute>()
				.FieldGuid;

			var level1Rdo = TestObjectHelper.GetStubRDO<GravityLevelOne>(RootArtifactID);
			
			providerMock.Setup(x => x.Read(RootArtifactID)).ReturnsResultSet(level1Rdo);
			var level2Rdo = TestObjectHelper.GetStubRDO<GravityLevel2Child>(level2ChildObjectId);
			var level3Rdo = TestObjectHelper.GetStubRDO<GravityLevel3>(singleObjectLevel3Id);
			var level3childRdo = TestObjectHelper.GetStubRDO<GravityLevel3Child>(level3ChildObjectId);
			level2Rdo.ParentArtifact = new Artifact(RootArtifactID);
			level2Rdo[singleLevel2Guid].ValueAsSingleObject = level3Rdo;
			level3childRdo.ParentArtifact = new Artifact(level2ChildObjectId);

			var iList = new List<RDO> {	level2Rdo };
			var iList2 = new List<RDO> { level3childRdo	};
			providerMock.Setup(x => x.Read(level2ChildObjectId)).ReturnsResultSet(level2Rdo);
			providerMock.Setup(x => x.Read(singleObjectLevel3Id)).ReturnsResultSet(level3Rdo);
			providerMock.Setup(x => x.Read(level3ChildObjectId)).ReturnsResultSet(level3childRdo);

			// setup the child object query
			providerMock.SetupSequence(x => x.Query(It.IsAny<Query<RDO>>()))
				.ReturnsResultSet(iList)
				.ReturnsResultSet(iList2);

			return providerMock.Object;
		}
	}
}
