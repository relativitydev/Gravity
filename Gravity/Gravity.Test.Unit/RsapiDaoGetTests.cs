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
using Choice = kCura.Relativity.Client.DTOs.Choice;

namespace Gravity.Test.Unit
{
	public class RsapiDaoGetTests
	{
		private const int RootArtifactID = 1111111;

		[Test]
		public void Get_BlankRDO()
		{
			var dao = new RsapiDao(GetChoiceRsapiProvider(null, null));
			var dto = dao.Get<GravityLevelOne>(RootArtifactID, Base.ObjectFieldsDepthLevel.FirstLevelOnly);
			Assert.AreEqual(RootArtifactID, dto.ArtifactId);
		}

		[Test]
		[Ignore("TODO: Implement")]
		public void Get_MultiObject_Recursive()
		{
			//test MultiObject fields with varying degrees of recursion
			throw new NotImplementedException();
		}

		[Test]
		[Ignore("TODO: Implement")]
		public void Get_ChildObjectList_Recursive()
		{
			//test ChildObject fields with varying degrees of recursion
			throw new NotImplementedException();
		}

		[Test]
		[Ignore("TODO: Implement")]
		public void Get_SingleObject_Recursive()
		{
			//test single object fields with varying degrees of recursion
			throw new NotImplementedException();
		}

		[Test]
		public void Get_DownloadsFileContents()
		{
			var fileArray = new byte[] { 2 };
			var fileName = "filename.dat";
			var dao = new RsapiDao(GetFileRsapiProvider(fileName, fileArray));
			var dto = dao.Get<GravityLevelOne>(RootArtifactID, Base.ObjectFieldsDepthLevel.OnlyParentObject);
			CollectionAssert.AreEqual(fileArray, ((ByteArrayFileDto)dto.FileField).ByteArray);
			Assert.AreEqual(fileName, ((ByteArrayFileDto)dto.FileField).FileName);
		}

		[Test]
		public void Get_SkipsDownloadIfNoFile()
		{
			var dao = new RsapiDao(GetFileRsapiProvider(null, null));
			var dto = dao.Get<GravityLevelOne>(RootArtifactID, Base.ObjectFieldsDepthLevel.OnlyParentObject);
			Assert.Null(dto.FileField);
		}

		[Test]
		public void Get_SingleChoice_InEnum()
		{
			var dao = new RsapiDao(GetChoiceRsapiProvider(2, null));
			var dto = dao.Get<GravityLevelOne>(RootArtifactID, Base.ObjectFieldsDepthLevel.FirstLevelOnly);
			Assert.AreEqual(SingleChoiceFieldChoices.SingleChoice2, dto.SingleChoice);
		}

		[Test]
		public void Get_SingleChoice_NotInEnum()
		{
			var dao = new RsapiDao(GetChoiceRsapiProvider(5, null));
			Assert.Throws<InvalidOperationException>(() => dao.Get<GravityLevelOne>(RootArtifactID, Base.ObjectFieldsDepthLevel.FirstLevelOnly));
		}

		[Test]
		public void Get_MultipleChoice_AllInEnum()
		{
			var dao = new RsapiDao(GetChoiceRsapiProvider(null, new[] { 11, 13 }));
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
			var dao = new RsapiDao(GetChoiceRsapiProvider(null, new[] { 3, 13 }));
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

			providerMock.Setup(x => x.ReadSingle(RootArtifactID)).Returns(rdo);

			if (fileName != null)
			{ 
				providerMock.Setup(x => x.Read(It.Is<RDO[]>(y => y.Single().Guids.Contains(fileGuid))))
					.Returns(new[] { new RDO(fileFieldId) }.ToSuccessResultSet());
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

			providerMock.Setup(x => x.ReadSingle(RootArtifactID)).Returns(rdo);

			// setup the child object query
			providerMock.Setup(x => x.Query(It.IsAny<Query<RDO>>())).Returns(new[] { new RDO[0].ToSuccessResultSet<QueryResultSet<RDO>>() });

			// setup the choice query

			// results in ArtifactIDs 1, 2, 3
			var singleChoiceGuids = ChoiceCacheTests.GetOrderedGuids<SingleChoiceFieldChoices>();
			providerMock.Setup(ChoiceCacheTests.SetupExpr(singleChoiceGuids)).Returns(ChoiceCacheTests.GetResults(singleChoiceGuids, 1));
			// results in ArtifactIDs 11, 12, 13
			var multiChoiceGuids = ChoiceCacheTests.GetOrderedGuids<MultipleChoiceFieldChoices>();
			providerMock.Setup(ChoiceCacheTests.SetupExpr(multiChoiceGuids)).Returns(ChoiceCacheTests.GetResults(multiChoiceGuids, 11));

			return providerMock.Object;
		}
	}
}
