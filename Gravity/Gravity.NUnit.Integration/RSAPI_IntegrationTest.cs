using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using kCura.Relativity.Client;
using NUnit.Framework;
using Relativity.API;
using Relativity.Test.Helpers;
using Relativity.Test.Helpers.ServiceFactory.Extentions;
using Relativity.Test.Helpers.SharedTestHelpers;
using Relativity.Test.Helpers.WorkspaceHelpers;
using Gravity.Test;

namespace Gravity.NUnit.Integration
{
    [TestFixture]
    public class RSAPI_IntegrationTest
    {
        #region Variables

        private bool _debug = true;

        private IRSAPIClient _client;
        private readonly string _workspaceName = $"GravityTest_{Guid.NewGuid()}";
        private int _workspaceId;
        private IServicesMgr _servicesManager;
        private IDBContext _eddsDbContext;
        private IDBContext _dbContext;
        //public string FilepathApplication = TestHelpers.Constants.Agent.DEFAULT_RAP_FILE_LOCATION + TestHelpers.Constants.Application.General.APPLICATION_NAME;
        public string FilepathApplication = "";
        private Test.TestObjectHelper _testObjectHelper;
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
                    _workspaceId = 1020846;
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
                        Relativity.Test.Helpers.Application.ApplicationHelpers.ImportApplication(_client, _workspaceId, true, Gravity.Test.Constants.GRAVITY_TEST_APPLICATION_LOCATION, Gravity.Test.Constants.GRAVITY_TEST_APPLICATION_NAME);
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
                _testObjectHelper = new Test.TestObjectHelper(_servicesManager, _workspaceId, 1);
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
                    Console.WriteLine("Not deleteing workspace because tests are using existing");
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
            Console.WriteLine("Valid_Gravity_Object_Created");

            //Create temp var to hold value of Workspace when entering method, will be used to reset upon exit.
            var clientWorkspaceArtifactId = _client.APIOptions.WorkspaceID;

            try
            {
                //Arrange
                Console.WriteLine("Starting Arrangement....");

                // Create Export Utility Job record
              
                Console.WriteLine("Arrangement Complete....");

                //Act
                Console.WriteLine("Starting Act....");

                var newRdoArtifactId = _testObjectHelper.CreateTestObjectWithGravity();

                Console.WriteLine("Act Complete....");

                //Assert
                Console.WriteLine("Starting Assertion....");

                //Assert object returned valid Artifact ID
                Console.WriteLine("Starting Artifact ID < 0 Assertion....");
                Assert.Greater(newRdoArtifactId, 0);
                Console.WriteLine("Artifact ID < 0 Assertion Complete...." + newRdoArtifactId.ToString());

                //should to validation of names and stuff
                Console.WriteLine("Assertion Complete....");
            }
            catch (Exception ex)
            {
                throw new Exception("Error encountered in Test Valid_Gravity_Object_Created.", ex);
            }
            finally
            {
                //Reset the client APIOptions Workspace context
                _client.APIOptions.WorkspaceID = clientWorkspaceArtifactId;
                Console.WriteLine("Ending Test case Valid_Gravity_Object_Created");
            }
        }
        #endregion

    }
}
