using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Configuration;
using Gravity.Base;
using Gravity.DAL.RSAPI;
using Gravity.Test.TestClasses;

namespace Gravity.Test
{
	[TestClass]
	public class RsapiDaoTest : BaseTest
	{
		private RsapiDao gravityRsapiDao;

		[TestInitialize]
		public override void Initialize()
		{
			base.Initialize();

			int workspaceId = Convert.ToInt32(ConfigurationManager.AppSettings["WorkspaceID"]);

			gravityRsapiDao = new RsapiDao(base.Helper, workspaceId);
		}

		[TestMethod]
		public void GetDTO()
		{
			int rdoArtifactID = 0;

			var testDto = gravityRsapiDao.GetRelativityObject<GravityLevelOne>(rdoArtifactID, ObjectFieldsDepthLevel.FullyRecursive);
			Assert.IsNotNull(testDto);
		}

		[TestMethod]
		public void DeleteDTO()
		{
			int rdoArtifactID = 0;

			gravityRsapiDao.DeleteRelativityObjectRecusively<GravityLevelOne>(rdoArtifactID);
		}

		[TestMethod]
		public void InsertDTO()
		{
			int rdoArtifactID = 0;

			var testDto = gravityRsapiDao.GetRelativityObject<GravityLevelOne>(rdoArtifactID, ObjectFieldsDepthLevel.FullyRecursive);
			testDto.Name += " Insrted From UT";
			int testDtoId = gravityRsapiDao.InsertRelativityObject<GravityLevelOne>(testDto);

			Assert.AreNotEqual(0, testDtoId);
		}

		[TestMethod]
		public void UpdateDTO()
		{
			int rdoArtifactID = 0;

			var testDto = gravityRsapiDao.GetRelativityObject<GravityLevelOne>(rdoArtifactID, ObjectFieldsDepthLevel.FullyRecursive);
			testDto.Name += " Updated";
			gravityRsapiDao.UpdateRelativityObject<GravityLevelOne>(testDto);
		}

		[TestMethod]
		public void UpdateField()
		{
			int rdoArtifactID = 0;

			gravityRsapiDao.UpdateField<GravityLevelOne>(rdoArtifactID, new Guid("1A7A55A7-C22C-421A-BFE9-CDC1107ABEA3"), "Updated from UT");
		}
	}
}
