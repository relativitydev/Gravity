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
using Choice = kCura.Relativity.Client.DTOs.Choice;
using Artifact = kCura.Relativity.Client.DTOs.Artifact;

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
		public string _applicationFilePath = ConfigurationManager.AppSettings["TestApplicationLocation"];
		public string _applicationName = ConfigurationManager.AppSettings["TestApplicationName"];

		private IRSAPIClient _client;
		private readonly string _workspaceName = $"GravityTest_{Guid.NewGuid()}";
		private int _workspaceId;
		private IServicesMgr _servicesManager;
		private IDBContext _eddsDbContext;
		private IDBContext _dbContext;
		private TestObjectHelper _testObjectHelper;
		#endregion

		#region Setup
		[OneTimeSetUp]
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
							CreateWorkspace.CreateWorkspaceAsync(_workspaceName,
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

					LogStart("Application Import");
					if (!_debug)
					{
						//Import Application
						Relativity.Test.Helpers.Application.ApplicationHelpers.ImportApplication(_client, _workspaceId, true, _applicationFilePath, _applicationName);
						LogEnd("Application Import");
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
				_testObjectHelper = new TestObjectHelper(_servicesManager, _workspaceId, 1);
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
		[OneTimeTearDown]
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
			void Inner()
			{
				//Arrange
				LogStart("Arrangement");

				GravityLevelOne testObject = new GravityLevelOne() { Name = $"TestObject_{Guid.NewGuid()}" };


				LogEnd("Arrangement");

				//Act
				LogStart("Act");

				var newRdoArtifactId = _testObjectHelper.CreateTestObjectWithGravity<GravityLevelOne>(testObject);

				LogEnd("Act");

				//Assert
				LogStart("Assertion");

				//Assert object returned valid Artifact ID
				LogStart("Artifact ID > 0 Assertion");
				Assert.Greater(newRdoArtifactId, 0);
				LogEnd($"Artifact ID > 0 Assertion (was {newRdoArtifactId})");

				LogEnd("Assertion");
			}
			TestWrapper(Inner);
		}

		[Test, Description("Verify RelativityObject field created correctly using Gravity"),
		 TestCaseSource(typeof(TestCaseDefinition), nameof(TestCaseDefinition.SimpleFieldReadWriteTestCases))]
		//need object fields, could get a little more difficult
		public void Valid_Gravity_RelativityObject_Create_Field_Type<T>(string objectPropertyName, T sampleData)
		{
			void Inner()
			{
				//Arrange
				LogStart($"for property{objectPropertyName}");

				GravityLevelOne testObject = new GravityLevelOne() { Name = $"TestObjectCreate_{objectPropertyName}{Guid.NewGuid()}" };

				var testFieldAttribute = testObject.GetCustomAttribute<RelativityObjectFieldAttribute>(objectPropertyName);
				Guid testFieldGuid = testFieldAttribute.FieldGuid;
				RdoFieldType fieldType = testFieldAttribute.FieldType;

				
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

				LogEnd("Arrangement");

				//Act
				LogStart("Act");

				var newRdoArtifactId = _testObjectHelper.CreateTestObjectWithGravity<GravityLevelOne>(testObject);

				//read artifactID from RSAPI
				RDO newObject = _client.Repositories.RDO.ReadSingle(newRdoArtifactId);

				FieldValue field = newObject.Fields.Get(testFieldGuid);

				object newObjectValue = null;
				object expectedData = sampleData;

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
							Choice choice = _client.Repositories.Choice.ReadSingle(choiceArtifactId);
							Enum singleChoice = (Enum)Enum.ToObject(sampleData.GetType(), sampleData);
							Guid singleChoiceGuid = singleChoice.GetRelativityObjectAttributeGuidValue();
							newObjectValue = choice.Guids.SingleOrDefault(x => x == singleChoiceGuid);
							expectedData = singleChoiceGuid;
						}
						break;
					case RdoFieldType.SingleObject:
						newObjectValue = field.ValueAsSingleObject.ArtifactID;
						expectedData = testObject.GravityLevel2Obj.ArtifactId > 0 
							? (object)testObject.GravityLevel2Obj.ArtifactId 
							: null;
						break;
					case RdoFieldType.MultipleObject:
						var rawNewObjectValue = field.GetValueAsMultipleObject<Artifact>();
						var resultData = new List<GravityLevel2>();
						Guid childFieldNameGuid = new GravityLevel2().GetCustomAttribute<RelativityObjectFieldAttribute>("Name").FieldGuid;

						foreach (Artifact child in rawNewObjectValue)
						{
							//'Read' - need to get name.
							RDO childRdo = new RDO()
							{
								Fields = new List<FieldValue>() { new FieldValue(childFieldNameGuid) }
							};
							childRdo = _client.Repositories.RDO.ReadSingle(child.ArtifactID);
							string childNameValue = childRdo.Fields.Where(x => x.Guids.Contains(childFieldNameGuid)).FirstOrDefault().ToString();

							resultData.Add(new GravityLevel2() { ArtifactId = child.ArtifactID, Name = childNameValue});
						}
						newObjectValue = resultData.ToDictionary(x => x.ArtifactId, x => x.Name);
						expectedData = ((IEnumerable<GravityLevel2>)expectedData).ToDictionary(x => x.ArtifactId, x => x.Name);

						break;
				}

				LogEnd("Act");

				//Assert
				LogStart("Assertion");

				//Assert
				Assert.AreEqual(expectedData, newObjectValue);

				LogEnd("Assertion");
			}
			TestWrapper(Inner);
		}

		[Test, Description("Verify RelativityObject field read correctly using Gravity"),
		 TestCaseSource(typeof(TestCaseDefinition), nameof(TestCaseDefinition.SimpleFieldReadWriteTestCases))]
		public void Valid_Gravity_RelativityObject_Read_Field_Type<T>(string objectPropertyName, T sampleData)
		{
			void Inner()
			{
				//Arrange
				LogStart($"Arrangement for property {objectPropertyName}");

				GravityLevelOne testObject = new GravityLevelOne() { Name = $"TestObjectRead_{objectPropertyName}{Guid.NewGuid()}" };

				Guid testObjectTypeGuid = testObject.GetObjectLevelCustomAttribute<RelativityObjectAttribute>().ObjectTypeGuid;
				Guid nameFieldGuid = testObject.GetCustomAttribute<RelativityObjectFieldAttribute>("Name").FieldGuid;
				var testFieldAttribute = testObject.GetCustomAttribute<RelativityObjectFieldAttribute>(objectPropertyName);
				Guid testFieldGuid = testFieldAttribute.FieldGuid;
				RdoFieldType fieldType = testObject.GetCustomAttribute<RelativityObjectFieldAttribute>(objectPropertyName).FieldType;

				_client.APIOptions.WorkspaceID = _workspaceId;

				object expectedData = sampleData;

				var dto = new RDO() { ArtifactTypeGuids = new List<Guid> { testObjectTypeGuid } };
				int newArtifactId = -1;
				dto.Fields.Add(new FieldValue(nameFieldGuid, testObject.Name));

				int objectToAttachID;

				//need this mess because when passing in tests for decimal and currency System wants to use double and causes problems
				switch (fieldType)
				{
					case RdoFieldType.SingleChoice:
						Enum singleChoice = (Enum)Enum.ToObject(sampleData.GetType(), sampleData);
						Guid singleChoiceGuid = singleChoice.GetRelativityObjectAttributeGuidValue();
						Choice singleChoiceToAdd = new Choice(singleChoiceGuid);
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
						FieldValueList<Artifact> objects = new FieldValueList<Artifact>();
						expectedData = new Dictionary<int, string>();
						foreach (GravityLevel2 child in gravityLevel2s)
						{
							objectToAttachID =
								_testObjectHelper.CreateTestObjectWithGravity<GravityLevel2>(child);
							objects.Add(new Artifact(objectToAttachID));
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
					Console.WriteLine($"Object was created with Artifact ID {newArtifactId}.");
				}
				else
				{
					Console.WriteLine($"An error occurred creating object: {writeResults.Message}");
					foreach (var result in 
						writeResults.Results
							.Select((item, index) => new { rdoResult = item, itemNumber = index })
							.Where(x => x.rdoResult.Success == false))
					{
						Console.WriteLine($"An error occurred in create request {result.itemNumber}: {result.rdoResult.Message}");
					}
				}

				LogEnd("Arrangement");

				//Act
				LogStart("Act");

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
		#endregion

		#region Test Helpers
		private static void TestWrapper(Action action)
		{
			string testName = TestContext.CurrentContext.Test.Name;
			Console.WriteLine($"{testName} Created");
			try
			{
				action();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error encountered in {testName}:\r\n{ex}");
				throw;
			}
			finally
			{
				Console.WriteLine($"Ending Test case {testName}");
			}
		}

		private static void LogStart(string message) => Console.WriteLine($"Starting {message}....");
		private static void LogEnd(string message) => Console.WriteLine($"{message} Complete....");
		#endregion
	}
}
