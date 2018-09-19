using System.Configuration;
using Gravity.Base;
using Gravity.DAL.SQL;
using Gravity.Test.Helpers;
using Gravity.Test.TestClasses;
using NUnit.Framework;

namespace Gravity.Test.Unit
{
	public class SqlDaoGetTests
	{
		const int gravityLvlOneId = 1039932;
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
			GravityLevelOne gravityLvlOne = sqlDao.Get<GravityLevelOne>(gravityLvlOneId, ObjectFieldsDepthLevel.OnlyParentObject);
			Assert.IsNull(gravityLvlOne.GravityLevel2Obj);
		}

		[Test]
		public void GetRelativityObjectFirstLvl()
		{
			GravityLevelOne gravityLvlOne = sqlDao.Get<GravityLevelOne>(gravityLvlOneId, ObjectFieldsDepthLevel.FirstLevelOnly);
			Assert.IsNotNull(gravityLvlOne.GravityLevel2Obj);
		}

		[Test]
		public void GetRelativityObject()
		{
			GravityLevelOne gravityLvlOne = sqlDao.Get<GravityLevelOne>(gravityLvlOneId, ObjectFieldsDepthLevel.FullyRecursive);
			Assert.AreEqual(gravityLvlOneId, gravityLvlOne.ArtifactId);
		}

		[Test]
		public void GetRelativityObjectFile()
		{
			string fileName = "test123.PNG";
			GravityLevelOne gravityLvlOne = sqlDao.Get<GravityLevelOne>(gravityLvlOneId, ObjectFieldsDepthLevel.FullyRecursive);

			var gravityFile = (ByteArrayFileDto)gravityLvlOne.FileField;

			Assert.AreEqual(fileName, gravityFile.FileName);
			Assert.IsNotEmpty(gravityFile.ByteArray);
		}
	}
}