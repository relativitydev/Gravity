using Gravity.DAL.RSAPI;
using Gravity.DAL.RSAPI.Tests;
using Gravity.Test.Helpers;
using Gravity.Test.TestClasses;
using kCura.Relativity.Client.DTOs;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Gravity.Test.Unit
{
	public class RsapiDaoGetTests
	{
		private const int RootArtifactID = 1111111;

		[Test]
		public void GetHydratedDTO_BlankRDO()
		{
			var dao = new RsapiDao(GetChoiceRsapiProvider(null, null));
			var dto = dao.GetRelativityObject<GravityLevelOne>(RootArtifactID, Base.ObjectFieldsDepthLevel.FirstLevelOnly);
			Assert.AreEqual(RootArtifactID, dto.ArtifactId);
		}

		[Test]
		[Ignore("TODO: Implement")]
		public void GetHydratedDTO_MultiObject_Recursive()
		{
			//test MultiObject fields with varying degrees of recursion
			throw new NotImplementedException();
		}

		[Test]
		[Ignore("TODO: Implement")]
		public void GetHydratedDTO_ChildObjectList_Recursive()
		{
			//test ChildObject fields with varying degrees of recursion
			throw new NotImplementedException();
		}

		[Test]
		[Ignore("TODO: Implement")]
		public void GetHydratedDTO_SingleObject_Recursive()
		{
			//test single object fields with varying degrees of recursion
			throw new NotImplementedException();
		}

		[Test]
		[Ignore("TODO: Implement")]
		public void GetHydratedDTO_DownloadsFileContents()
		{
			//if possible, test whether Hydrated DTO can download properly
			throw new NotImplementedException();
		}

		[Test]
		public void GetHydratedDTO_SingleChoice_InEnum()
		{
			var dao = new RsapiDao(GetChoiceRsapiProvider(2, null));
			var dto = dao.GetRelativityObject<GravityLevelOne>(RootArtifactID, Base.ObjectFieldsDepthLevel.FirstLevelOnly);
			Assert.AreEqual(SingleChoiceFieldChoices.SingleChoice2, dto.SingleChoice);
		}

		[Test]
		public void GetHydratedDTO_SingleChoice_NotInEnum()
		{
			var dao = new RsapiDao(GetChoiceRsapiProvider(5, null));
			Assert.Throws<InvalidOperationException>(() => dao.GetRelativityObject<GravityLevelOne>(RootArtifactID, Base.ObjectFieldsDepthLevel.FirstLevelOnly));
		}

		[Test]
		public void GetHydratedDTO_MultipleChoice_AllInEnum()
		{
			var dao = new RsapiDao(GetChoiceRsapiProvider(null, new[] { 11, 13 }));
			var dto = dao.GetRelativityObject<GravityLevelOne>(RootArtifactID, Base.ObjectFieldsDepthLevel.FirstLevelOnly);
			CollectionAssert.AreEquivalent(
				new[] { MultipleChoiceFieldChoices.MultipleChoice1, MultipleChoiceFieldChoices.MultipleChoice3 },
				dto.MultipleChoiceFieldChoices
			);
		}

		[Test]
		public void GetHydratedDTO_MultipleChoice_NotAllInEnum()
		{
			//first item is in an enum, but not in our enum
			var dao = new RsapiDao(GetChoiceRsapiProvider(null, new[] { 3, 13 }));
			Assert.Throws<InvalidOperationException>(() => dao.GetRelativityObject<GravityLevelOne>(RootArtifactID, Base.ObjectFieldsDepthLevel.FirstLevelOnly));

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
