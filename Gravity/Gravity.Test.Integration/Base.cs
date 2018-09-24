using System;
using System.Configuration;
using Gravity.Test.Helpers;
using kCura.Relativity.Client;
using NUnit.Framework;
using Relativity.API;
using Relativity.Test.Helpers;
using Relativity.Test.Helpers.ServiceFactory.Extentions;
using Relativity.Test.Helpers.SharedTestHelpers;
using Relativity.Test.Helpers.WorkspaceHelpers;

namespace Gravity.Test.Integration
{
	public class Base
	{
		#region Variables

		//this is set to true and should skip the creation of the database, installation of the testing application, and the deletion of the workspace.
		//must point _workspaceId = valid workspace with application already installed in the Setup
		internal static bool _debug = Convert.ToBoolean(ConfigurationManager.AppSettings["Debug"]);
		internal static int _debugWorkspaceId = Convert.ToInt32(ConfigurationManager.AppSettings["DebugWorkspaceId"]);

		internal static IRSAPIClient _client;
		internal static readonly string _workspaceName = $"GravityTest_{Guid.NewGuid()}";
		internal static int _workspaceId;
		internal static Relativity.API.IServicesMgr _servicesManager;
		internal static IDBContext _eddsDbContext;
		internal static IDBContext _dbContext;
		internal static TestObjectHelper _testObjectHelper;
		#endregion

		[SetUpFixture]
		public class MySetUpClass
		{
			private string _applicationFilePath = ConfigurationManager.AppSettings["TestApplicationLocation"];
			private string _applicationName = ConfigurationManager.AppSettings["TestApplicationName"];

			[OneTimeSetUp]
			public void RunBeforeAnyTests()
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

						_eddsDbContext = GetDBContext(-1);
						_dbContext = GetDBContext(_workspaceId);

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
					_testObjectHelper = new TestObjectHelper(_servicesManager, _workspaceId, _dbContext, _eddsDbContext, 1);
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

			[OneTimeTearDown]
			public void RunAfterAnyTests()
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

			private IDBContext GetDBContext(int caseID)
			{
				string dbName = "EDDS";

				if (caseID > 0)
				{
					dbName += caseID.ToString();
				}

				return new DbContextHelper.DbContext(ConfigurationHelper.SQL_SERVER_ADDRESS, dbName, ConfigurationHelper.SQL_USER_NAME, ConfigurationHelper.SQL_PASSWORD);
			}

		}

		public static void LogStart(string message) => Console.WriteLine($"Starting {message}....");
		public static void LogEnd(string message) => Console.WriteLine($"{message} Complete....");

		public static void TestWrapper(Action action)
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

	}
}
