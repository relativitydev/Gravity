using System.Configuration;
using Gravity.Base;
using Gravity.DAL.SQL;
using Relativity.Test.Helpers.SharedTestHelpers;
using Gravity.Test.TestClasses;
using NUnit.Framework;
using DbContextHelper;
using System;
using kCura.Relativity.Client.DTOs;
using System.Collections.Generic;
using System.Linq;
using Gravity.Extensions;

namespace Gravity.Test.Integration
{
	public partial class SQL_IntegrationTest : Base
		{
		const int gravityLvlOneId = 1039932;

		SqlDao sqlDao;

		[SetUp]
		public void SqlDaoSetup()
		{
			sqlDao = new SqlDao(_dbContext, _eddsDbContext, null);
		}

		[Test, Description("SqlDao: Verify RelativityObject field read correctly using Gravity"),
		TestCaseSource(typeof(TestCaseDefinition), nameof(TestCaseDefinition.SimpleFieldReadWriteTestCases))]
		public void Valid_Gravity_RelativityObject_Read_Field_Type<T>(string objectPropertyName, T sampleData)
		{
				void Inner()
				{
						//Arrange
						LogStart($"Arrangement for property {objectPropertyName}");

								GravityLevelOne testObject = new GravityLevelOne() { Name = $"TestObjectRead_{objectPropertyName}{Guid.NewGuid()}" };

								var testFieldAttribute = testObject.GetCustomAttribute<RelativityObjectFieldAttribute>(objectPropertyName);
								Guid testFieldGuid = testFieldAttribute.FieldGuid;
								RdoFieldType fieldType = testFieldAttribute.FieldType;
								Guid nameFieldGuid = testObject.GetCustomAttribute<RelativityObjectFieldAttribute>("Name").FieldGuid;
								Guid testObjectTypeGuid = testObject.GetObjectLevelCustomAttribute<RelativityObjectAttribute>().ObjectTypeGuid;

								_client.APIOptions.WorkspaceID = _workspaceId;
								object expectedData = sampleData;
								int newArtifactId = RSAPI_IntegrationTestHelper.CreateRDOFromValue(testObject, _client, _testObjectHelper,
										testObjectTypeGuid, nameFieldGuid, testFieldGuid, sampleData, fieldType, ref expectedData);


								LogEnd("Arrangement");

						//Act
						LogStart("Act");

						object gravityFieldValue = null;

						if (newArtifactId > 0)
						{
								GravityLevelOne testGravityObject = _testObjectHelper.GetSqlDao().Get<GravityLevelOne>(newArtifactId, ObjectFieldsDepthLevel.FirstLevelOnly);
								gravityFieldValue = testGravityObject.GetPropertyValue(objectPropertyName);
								if (gravityFieldValue != null)
								{
										switch (fieldType)
										{
												case RdoFieldType.SingleObject:
														gravityFieldValue = ((GravityLevel2)gravityFieldValue).Name;
														break;
												case RdoFieldType.MultipleObject:
														gravityFieldValue = ((List<GravityLevel2>)gravityFieldValue).ToDictionary(x => x.ArtifactId, x => x.Name);
														break;
										}
								}
						}

						LogEnd("Act");

						//Assert
						LogStart("Assertion");

						if (newArtifactId > 0)
						{
								Assert.AreEqual(expectedData, gravityFieldValue);
						}
						else
						{
								Assert.Fail("Could not create object to test with through RSAPI. This is not a Gravity failure.");
						}

						LogEnd("Assertion");
				}
				TestWrapper(Inner);
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

		private int CreateRelativityObjectForTest()
				{
						int newArtifactId = 0;
						return newArtifactId;
				}
	}
}