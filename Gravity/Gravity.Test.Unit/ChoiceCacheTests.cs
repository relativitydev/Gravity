using NUnit.Framework;
using Gravity.DAL.RSAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Gravity.Test.TestClasses;
using Gravity.Extensions;
using kCura.Relativity.Client.DTOs;
using System.Linq.Expressions;
using Gravity.Test.Helpers;

namespace Gravity.DAL.RSAPI.Tests
{
	[TestFixture]
	public class ChoiceCacheTests
	{
		Mock<IRsapiProvider> rsapiProvider;

		[SetUp]
		public void Init()
		{
			rsapiProvider = new Mock<IRsapiProvider>(MockBehavior.Strict);
		}

		[Test]
		public void GetEnum_TypeIsNotEnum()
		{
			var choiceCache = new ChoiceCache(rsapiProvider.Object);
			Assert.Throws<NotSupportedException>(() => choiceCache.GetEnum<GravityLevel2>(1));
		}

		[Test]
		public void GetEnum_NoValueFound()
		{
			var choiceGuids = GetOrderedGuids<SingleChoiceFieldChoices>();
			rsapiProvider.Setup(SetupExpr(choiceGuids)).Returns(GetResults(choiceGuids, 1));

			var choiceCache = new ChoiceCache(rsapiProvider.Object);
			Assert.Throws<InvalidOperationException>(() => choiceCache.GetEnum<SingleChoiceFieldChoices>(1000));
		}

		[Test]
		public void GetEnum_OnlyFetchesFromServerOnce()
		{
			var choiceGuids = GetOrderedGuids<SingleChoiceFieldChoices>();
			var setupExpr = SetupExpr(choiceGuids);

			rsapiProvider.SetupSequence(setupExpr).Returns(GetResults(choiceGuids, 1));

			var choiceCache = new ChoiceCache(rsapiProvider.Object);

			Assert.AreEqual(SingleChoiceFieldChoices.SingleChoice1, choiceCache.GetEnum<SingleChoiceFieldChoices>(1));
			Assert.AreEqual(SingleChoiceFieldChoices.SingleChoice1, choiceCache.GetEnum<SingleChoiceFieldChoices>(1));

			rsapiProvider.Verify(setupExpr, Times.Once);
		}

		[Test]
		public void GetEnum_RefreshesForEachCacheInstance()
		{
			var choiceGuids = GetOrderedGuids<SingleChoiceFieldChoices>();
			var setupExpr = SetupExpr(choiceGuids);

			rsapiProvider
				.SetupSequence(setupExpr)
				.Returns(GetResults(choiceGuids, 1))
				.Returns(GetResults(choiceGuids, 11));
			var choiceCache1 = new ChoiceCache(rsapiProvider.Object);
			var choiceCache2 = new ChoiceCache(rsapiProvider.Object);

			Assert.AreEqual(SingleChoiceFieldChoices.SingleChoice1, choiceCache1.GetEnum<SingleChoiceFieldChoices>(1));
			Assert.AreEqual(SingleChoiceFieldChoices.SingleChoice1, choiceCache2.GetEnum<SingleChoiceFieldChoices>(11));

			rsapiProvider.Verify(setupExpr, Times.Exactly(2));
		}

		[Test]
		public void GetEnum_RefreshesForDifferentEnumerationTypes()
		{
			var singleChoiceGuids = GetOrderedGuids<SingleChoiceFieldChoices>();
			var multiChoiceGuids = GetOrderedGuids<MultipleChoiceFieldChoices>();
			var singleSetupExpr = SetupExpr(singleChoiceGuids);
			var multiSetupExpr = SetupExpr(multiChoiceGuids);

			rsapiProvider.SetupSequence(singleSetupExpr).Returns(GetResults(singleChoiceGuids, 1));
			rsapiProvider.SetupSequence(multiSetupExpr).Returns(GetResults(multiChoiceGuids, 11));

			var choiceCache = new ChoiceCache(rsapiProvider.Object);

			Assert.AreEqual(SingleChoiceFieldChoices.SingleChoice1, choiceCache.GetEnum<SingleChoiceFieldChoices>(1));
			Assert.AreEqual(MultipleChoiceFieldChoices.MultipleChoice1, choiceCache.GetEnum<MultipleChoiceFieldChoices>(11));

			rsapiProvider.Verify(singleSetupExpr, Times.Once);
			rsapiProvider.Verify(multiSetupExpr, Times.Once);
		}

		internal static List<Guid> GetOrderedGuids<T>()
		{
			return EnumHelpers.GetAttributesForValues<T, RelativityObjectAttribute>()
						.OrderBy(x => x.Key)
						.Select(x => x.Value.ObjectTypeGuid)
						.ToList();
		}

		internal static Expression<Func<IRsapiProvider, ResultSet<RDO>>> SetupExpr(IEnumerable<Guid> guids)
		{
			return z => z.Read(It.Is<List<RDO>>(x => new HashSet<Guid>(guids).SetEquals(x.Select(y => y.Guids.Single()))));
		}

		internal static ResultSet<RDO> GetResults(List<Guid> choiceGuids, int offset)
		{
			return choiceGuids.Select((x, i) => new RDO(i + offset) { Guids = new List<Guid> { x } }).ToSuccessResultSet();
		}


	}
}