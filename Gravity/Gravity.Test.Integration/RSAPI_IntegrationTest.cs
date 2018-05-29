using System;
using System.Collections.Generic;
using System.Linq;
using kCura.Relativity.Client;
using NUnit.Framework;
using Relativity.API;
using Relativity.Test.Helpers;
using Relativity.Test.Helpers.ServiceFactory.Extentions;
using Relativity.Test.Helpers.SharedTestHelpers;
using Relativity.Test.Helpers.WorkspaceHelpers;
using System.Configuration;
using Gravity.Base;
using Gravity.Test.TestClasses;
using kCura.Relativity.Client.DTOs;
using System.Reflection;
using Gravity.Extensions;
using Gravity.Test.Helpers;

namespace Gravity.Test.Integration
{
	[TestFixture]
	public class RSAPI_IntegrationTest
	{
		#region Variables

		//this is set to true and should skip the creation of the database, installation of the testing application, and the deletion of the workspace.
		//must point _workspaceId = valid workspace with application already installed in the Setup
		private bool _debug = Convert.ToBoolean(ConfigurationManager.AppSettings["Debug"]);
		private int _debugWorkspaceId = Convert.ToInt32(ConfigurationManager.AppSettings["DebugWorkspaceId"]);

		private IRSAPIClient _client;
		private readonly string _workspaceName = $"GravityTest_{Guid.NewGuid()}";
		private int _workspaceId;
		private IServicesMgr _servicesManager;
		private IDBContext _eddsDbContext;
		private IDBContext _dbContext;
		//public string FilepathApplication = TestHelpers.Constants.Agent.DEFAULT_RAP_FILE_LOCATION + TestHelpers.Constants.Application.General.APPLICATION_NAME;
		public string _applicationFilePath = ConfigurationManager.AppSettings["TestApplicationLocation"];
		public string _applicationName = ConfigurationManager.AppSettings["TestApplicationName"];
		private Test.Helpers.TestObjectHelper _testObjectHelper;
		#endregion

		#region Setup
		[TestFixtureSetUp]
		public void Execute_TestFixtureSetup()
		{
			try
			{
				//Start of test and setup
				Console.WriteLine("Test START.....");
				Console.WriteLine("Enter Test Fixture Setup.....");
				var helper = new TestHelper();

				//Setup for testing		
				Console.WriteLine("Creating Test Helper Services Manager based on App.Config file settings.");
				_servicesManager = helper.GetServicesManager();
				Console.WriteLine("Services Manager Created.");

				Console.WriteLine("Creating workspace.....");
				if (!_debug)
				{
					_workspaceId =
							Relativity.Test.Helpers.WorkspaceHelpers.CreateWorkspace.CreateWorkspaceAsync(_workspaceName,
									ConfigurationHelper.TEST_WORKSPACE_TEMPLATE_NAME, _servicesManager, ConfigurationHelper.ADMIN_USERNAME,
									ConfigurationHelper.DEFAULT_PASSWORD).Result;
					Console.WriteLine($"Workspace created [WorkspaceArtifactId= {_workspaceId}].....");
				}
				else
				{
					//must point _workspaceId = valid workspace with application already installed
					_workspaceId = _debugWorkspaceId;
					Console.WriteLine($"Using existing workspace [WorkspaceArtifactId= {_workspaceId}].....");
				}


				Console.WriteLine("Creating RSAPI and Service Factory.....");
				try
				{

					_eddsDbContext = helper.GetDBContext(-1);
					_dbContext = helper.GetDBContext(_workspaceId);

					//create client
					_client = _servicesManager.GetProxy<IRSAPIClient>(ConfigurationHelper.ADMIN_USERNAME, ConfigurationHelper.DEFAULT_PASSWORD);

					Console.WriteLine("Importing Application.");
					if (!_debug)
					{
						//Import Application
						Relativity.Test.Helpers.Application.ApplicationHelpers.ImportApplication(_client, _workspaceId, true, _applicationFilePath, _applicationName);
						Console.WriteLine("Application import Complete.");
					}
					else
					{
						Console.WriteLine($"Using existing application");
					}



					_client.APIOptions.WorkspaceID = _workspaceId;

				}
				catch (Exception ex)
				{
					throw new Exception("Error encountered while creating new RSAPI Client and/or Service Proxy.", ex);
				}
				finally
				{
					Console.WriteLine("Created RSAPI and Service Factory.....");
				}

				Console.WriteLine("Creating TestObject Helper.");
				//1 retry setting because I want to know if it fails quickly while debugging, may bump up for production
				_testObjectHelper = new Test.Helpers.TestObjectHelper(_servicesManager, _workspaceId, 1);
				Console.WriteLine("TestObject Helper Created.");

			}
			catch (Exception ex)
			{
				throw new Exception("Error encountered in Test Setup.", ex);
			}
			finally
			{
				Console.WriteLine("Exit Test Fixture Setup.....");
			}
		}
		#endregion

		#region "Teardown"
		[TestFixtureTearDown]
		public void Execute_TestFixtureTeardown()
		{
			try
			{
				Console.WriteLine("Test Teardown START.....");

				//Delete workspace
				Console.WriteLine("Deleting workspace.....");
				if (!_debug)
				{
					DeleteWorkspace.DeleteTestWorkspace(_workspaceId, _servicesManager, ConfigurationHelper.ADMIN_USERNAME, ConfigurationHelper.DEFAULT_PASSWORD);
					Console.WriteLine("Workspace deleted.....");
				}
				else
				{
					Console.WriteLine("Not deleting workspace because tests are using existing");
				}
			}
			catch (Exception ex)
			{
				throw new Exception("Error encountered in Test Teardown.", ex);
			}
			finally
			{
				Console.WriteLine("Test Teardown END.....");
				Console.WriteLine("Test END.....");
			}
		}
		#endregion

		#region "Tests"

		[Test, Description("Verify Test Object is Created")]
		public void Valid_Gravity_Object_Created()
		{
			Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name + " Created");

			try
			{
				//Arrange
				Console.WriteLine("Starting Arrangement....");

				GravityLevelOne testObject = new GravityLevelOne();
				testObject.Name = $"TestObject_{Guid.NewGuid()}";


				Console.WriteLine("Arrangement Complete....");

				//Act
				Console.WriteLine("Starting Act....");

				var newRdoArtifactId = _testObjectHelper.CreateTestObjectWithGravity<GravityLevelOne>(testObject);

				Console.WriteLine("Act Complete....");

				//Assert
				Console.WriteLine("Starting Assertion....");

				//Assert object returned valid Artifact ID
				Console.WriteLine("Starting Artifact ID > 0 Assertion....");
				Assert.Greater(newRdoArtifactId, 0);
				Console.WriteLine("Artifact ID > 0 Assertion Complete...." + newRdoArtifactId.ToString());

				Console.WriteLine("Assertion Complete....");
			}
			catch (Exception ex)
			{
				throw new Exception("Error encountered in " + System.Reflection.MethodBase.GetCurrentMethod().Name, ex);
			}
			finally
			{
				Console.WriteLine("Ending Test case " + System.Reflection.MethodBase.GetCurrentMethod().Name);
			}
		}

		[Test, Description("Verify RelativityObject field created correctly using Gravity"),
		 TestCaseSource(typeof(TestCaseDefinition), "SimpleFieldReadWriteTestCases")]
		//need object fields, could get a little more difficult
		public void Valid_Gravity_RelativityObject_Create_Field_Type<T>(string objectPropertyName, T sampleData)
		{
			Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name + " Created");

			try
			{
				//Arrange
				Console.WriteLine("Starting Arrangement for property...." + objectPropertyName);

				GravityLevelOne testObject = new GravityLevelOne();
				testObject.Name = "TestObjectCreate_" + objectPropertyName + Guid.NewGuid().ToString();

				Guid testFieldGuid = testObject.GetCustomAttribute<RelativityObjectFieldAttribute>(objectPropertyName).FieldGuid;
				//can get rid of cast once FieldType is created as RdoFieldType and not int
				RdoFieldType fieldType = testObject.GetCustomAttribute<RelativityObjectFieldAttribute>(objectPropertyName).FieldType;

				object expectedData = sampleData;
				
				//need this mess because when passing in tests for decimal and currency System wants to use double and causes problems
				switch (fieldType)
				{
					case RdoFieldType.Currency:
					case RdoFieldType.Decimal:
						testObject.SetValueByPropertyName(objectPropertyName, Convert.ToDecimal(sampleData));
						break;
					default:
						testObject.SetValueByPropertyName(objectPropertyName, sampleData);
						break;
				}

				_client.APIOptions.WorkspaceID = _workspaceId;

				Console.WriteLine("Arrangement Complete....");

				//Act
				Console.WriteLine("Starting Act....");

				var newRdoArtifactId = _testObjectHelper.CreateTestObjectWithGravity<GravityLevelOne>(testObject);

				//read artifactID from RSAPI
				RDO newObject = _client.Repositories.RDO.ReadSingle(newRdoArtifactId);

				FieldValue field = newObject.Fields.Get(testFieldGuid);

				object newObjectValue = null;

				switch (fieldType)
				{
					case RdoFieldType.LongText:
						newObjectValue = field.ValueAsLongText;
						break;
					case RdoFieldType.FixedLengthText:
						newObjectValue = field.ValueAsFixedLengthText;
						break;
					case RdoFieldType.WholeNumber:
						newObjectValue = field.ValueAsWholeNumber;
						break;
					case RdoFieldType.YesNo:
						newObjectValue = field.ValueAsYesNo;
						break;
					case RdoFieldType.Currency:
						newObjectValue = field.ValueAsCurrency;
						break;
					case RdoFieldType.Decimal:
						newObjectValue = field.ValueAsDecimal;
						break;
					case RdoFieldType.SingleChoice:
						int choiceArtifactId = field.ValueAsSingleChoice.ArtifactID;
						if (choiceArtifactId > 0)
						{
							kCura.Relativity.Client.DTOs.Choice choice = _client.Repositories.Choice.ReadSingle(choiceArtifactId);
							Enum singleChoice = (Enum)Enum.ToObject(sampleData.GetType(), sampleData);
							Guid singleChoiceGuid = singleChoice.GetRelativityObjectAttributeGuidValue();
							newObjectValue = choice.Guids.SingleOrDefault(x => x.Equals(singleChoiceGuid));
							expectedData = singleChoiceGuid;
						}
						break;
					case RdoFieldType.SingleObject:
						newObjectValue = field.ValueAsSingleObject.ArtifactID;
						if (testObject.GravityLevel2Obj.ArtifactId > 0)
						{
							expectedData = testObject.GravityLevel2Obj.ArtifactId;
						}
						else
						{
							expectedData = null;
						}

						break;
					case RdoFieldType.MultipleObject:
						newObjectValue = field.GetValueAsMultipleObject<kCura.Relativity.Client.DTOs.Artifact>();
						List<GravityLevel2> resultData = new List<GravityLevel2>();
						GravityLevel2 g2 = new GravityLevel2();
						Guid childFieldNameGuid = g2.GetCustomAttribute<RelativityObjectFieldAttribute>("Name").FieldGuid;

						foreach (kCura.Relativity.Client.DTOs.Artifact child in (kCura.Relativity.Client.DTOs.FieldValueList<kCura.Relativity.Client.DTOs.Artifact>)newObjectValue)
						{
							//'Read' - need to get name.
							kCura.Relativity.Client.DTOs.RDO childRdo = new kCura.Relativity.Client.DTOs.RDO();
							childRdo.Fields = new List<FieldValue>() {new FieldValue(childFieldNameGuid) };
							childRdo = _client.Repositories.RDO.ReadSingle(child.ArtifactID);
							string childNameValue = childRdo.Fields.Where(x => x.Guids.Contains(childFieldNameGuid)).FirstOrDefault().ToString();

							resultData.Add(new GravityLevel2() { ArtifactId = child.ArtifactID, Name = childNameValue});
						}
						expectedData = (expectedData as IEnumerable<GravityLevel2>).ToDictionary(x => x.ArtifactId, x => x.Name);
						newObjectValue = (resultData).ToDictionary(x => x.ArtifactId, x => x.Name); 

						break;
				}

				Console.WriteLine("Act Complete....");

				//Assert
				Console.WriteLine("Starting Assertion....");

				//Assert
				Assert.AreEqual(expectedData, newObjectValue);

				Console.WriteLine("Assertion Complete....");
			}
			catch (Exception ex)
			{
				throw new Exception("Error encountered in " + System.Reflection.MethodBase.GetCurrentMethod().Name, ex);
			}
			finally
			{
				Console.WriteLine("Ending Test case " + System.Reflection.MethodBase.GetCurrentMethod().Name);
			}
		}

		[Test, Description("Verify RelativityObject field read correctly using Gravity"),
		 TestCaseSource(typeof(TestCaseDefinition), "SimpleFieldReadWriteTestCases")]
		public void Valid_Gravity_RelativityObject_Read_Field_Type<T>(string objectPropertyName, T sampleData)
		{
			Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name + " Created");

			try
			{
				//Arrange
				Console.WriteLine("Starting Arrangement for property...." + objectPropertyName);

				GravityLevelOne testObject = new GravityLevelOne();
				testObject.Name = "TestObjectRead_" + objectPropertyName + Guid.NewGuid().ToString();

				Guid testObjectTypeGuid = testObject.GetObjectLevelCustomAttribute<RelativityObjectAttribute>().ObjectTypeGuid;
				Guid nameFieldGuid = testObject.GetCustomAttribute<RelativityObjectFieldAttribute>("Name").FieldGuid;
				Guid testFieldGuid = testObject.GetCustomAttribute<RelativityObjectFieldAttribute>(objectPropertyName).FieldGuid;
				RdoFieldType fieldType = testObject.GetCustomAttribute<RelativityObjectFieldAttribute>(objectPropertyName).FieldType;

				_client.APIOptions.WorkspaceID = _workspaceId;

				object expectedData = sampleData;

				var dto = new RDO();
				int newArtifactId = -1;
				dto.ArtifactTypeGuids.Add(testObjectTypeGuid);
				dto.Fields.Add(new FieldValue(nameFieldGuid, testObject.Name));

				int objectToAttachID;

				//need this mess because when passing in tests for decimal and currency System wants to use double and causes problems
				switch (fieldType)
				{
					case RdoFieldType.SingleChoice:
						Enum singleChoice = (Enum)Enum.ToObject(sampleData.GetType(), sampleData);
						Guid singleChoiceGuid = singleChoice.GetRelativityObjectAttributeGuidValue();
						kCura.Relativity.Client.DTOs.Choice singleChoiceToAdd = new kCura.Relativity.Client.DTOs.Choice(singleChoiceGuid);
						dto.Fields.Add(new FieldValue(testFieldGuid, singleChoiceToAdd));
						break;
					case RdoFieldType.SingleObject:
						int objectToAttach =
								_testObjectHelper.CreateTestObjectWithGravity<GravityLevel2>(sampleData as GravityLevel2);
						dto.Fields.Add(new FieldValue(testFieldGuid, objectToAttach));
						expectedData = (sampleData as GravityLevel2).Name;
						break;
					case RdoFieldType.MultipleObject:
						IList<GravityLevel2> gravityLevel2s = (IList<GravityLevel2>)sampleData;
						FieldValueList<kCura.Relativity.Client.DTOs.Artifact> objects = new FieldValueList<kCura.Relativity.Client.DTOs.Artifact>();
						expectedData = new Dictionary<int, string>();
						foreach (GravityLevel2 child in gravityLevel2s)
						{
							objectToAttachID =
								_testObjectHelper.CreateTestObjectWithGravity<GravityLevel2>(child);
							objects.Add(new kCura.Relativity.Client.DTOs.Artifact(objectToAttachID));
							(expectedData as Dictionary<int, string>).Add(objectToAttachID, child.Name);
						}
						dto.Fields.Add(new FieldValue(testFieldGuid, objects));
						break;
					default:
						dto.Fields.Add(new FieldValue(testFieldGuid, sampleData));
						break;
				}

				WriteResultSet<RDO> writeResults = _client.Repositories.RDO.Create(dto);

				if (writeResults.Success)
				{
					newArtifactId = writeResults.Results[0].Artifact.ArtifactID;
					Console.WriteLine(string.Format("Object was created with Artifact ID {0}.", newArtifactId));
				}
				else
				{
					Console.WriteLine(string.Format("An error occurred creating object: {0}", writeResults.Message));
					foreach (var result in writeResults.Results.Select((item, index) => new { rdoResult = item, itemNumber = index }).Where(x => x.rdoResult.Success.Equals(false))
							.Where(y => y.rdoResult.Success.Equals(false)))
					{
						Console.WriteLine(String.Format("An error occurred in create request {0}: {1}", result.itemNumber, result.rdoResult.Message));
					}
				}

				Console.WriteLine("Arrangement Complete....");

				//Act
				Console.WriteLine("Starting Act....");

				object gravityFieldValue = null;

				if (newArtifactId > 0)
				{
					GravityLevelOne testGravityObject = _testObjectHelper.ReturnTestObjectWithGravity<GravityLevelOne>(newArtifactId);
					gravityFieldValue = testGravityObject.GetPropertyValue(objectPropertyName);
					if (gravityFieldValue != null)
					{
						switch (fieldType)
						{
							case RdoFieldType.SingleObject:
								gravityFieldValue = (gravityFieldValue as GravityLevel2).Name;
								break;
							case RdoFieldType.MultipleObject:
								gravityFieldValue = (gravityFieldValue as List<GravityLevel2>).ToDictionary(x => x.ArtifactId, x => x.Name);
								break;
						}
					}
				}

				Console.WriteLine("Act Complete....");

				//Assert
				Console.WriteLine("Starting Assertion....");

				if (newArtifactId > 0)
				{
					Assert.AreEqual(expectedData, gravityFieldValue);
				}
				else
				{
					Assert.Fail("Could not create object to test with through RSAPI.  This is not a Gravity failure.");
				}

				Console.WriteLine("Assertion Complete....");
			}
			catch (Exception ex)
			{
				throw new Exception("Error encountered in " + System.Reflection.MethodBase.GetCurrentMethod().Name, ex);
			}
			finally
			{
				Console.WriteLine("Ending Test case " + System.Reflection.MethodBase.GetCurrentMethod().Name);
			}
		}
		#endregion
	}
}
