using Gravity.DAL.SQL;
using Gravity.Test.Helpers;
using Gravity.Test.TestClasses;
using NUnit.Framework;
using System.Configuration;

namespace Gravity.Test.Unit
{
	public class SqlDaoGetTests
	{
		const int gravityLvlOneId = 1040359;
		SqlDao sqlDao;

		[SetUp]
		public void SqlDaoSetup()
		{
			int gravityWsId = int.Parse(ConfigurationManager.AppSettings["GravityWorkspaceID"]);
			sqlDao = new SqlDao(new AppConfigConnectionHelper(), gravityWsId);
		}

		[Test]
		public void GetRelativityObjectSlim()
		{
			GravityLevelOne gravityLvlOne = sqlDao.Get<GravityLevelOne>(gravityLvlOneId, Base.ObjectFieldsDepthLevel.OnlyParentObject);
			Assert.IsNull(gravityLvlOne.GravityLevel2Obj);
		}

		[Test]
		public void GetRelativityObjectFirstLvl()
		{
			GravityLevelOne gravityLvlOne = sqlDao.Get<GravityLevelOne>(gravityLvlOneId, Base.ObjectFieldsDepthLevel.FirstLevelOnly);
			Assert.IsNotNull(gravityLvlOne.GravityLevel2Obj);
		}

		[Test]
		public void GetRelativityObject()
		{
			GravityLevelOne gravityLvlOne = sqlDao.Get<GravityLevelOne>(gravityLvlOneId, Base.ObjectFieldsDepthLevel.FullyRecursive);
			Assert.AreEqual(gravityLvlOneId, gravityLvlOne.ArtifactId);
		}
	}
}