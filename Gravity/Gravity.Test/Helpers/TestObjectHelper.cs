using Gravity.DAL.RSAPI;
using Relativity.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gravity.Utils;

namespace Gravity.Test
{
    public class TestObjectHelper
    {
        IServicesMgr _servicesManager;
        int _workspaceId;
        private InvokeWithRetrySettings _retrySettings;

        public TestObjectHelper(IServicesMgr servicesManager, int workspaceId, int numberOfRetrySettings)
        {
            _servicesManager = servicesManager;
            _workspaceId = workspaceId;
            _retrySettings = new InvokeWithRetrySettings(numberOfRetrySettings, 1000);
        }

        public int CreateTestObjectWithGravity()
        {
            

            RsapiDao gravityRsapiDao = new RsapiDao(_servicesManager, _workspaceId, ExecutionIdentity.System, _retrySettings);

            var testDto = new TestClasses.TestObject
            {
                Name = "Test Name",
                TextField = "Text Field"
            };

            int testDtoId = gravityRsapiDao.InsertRelativityObject<TestClasses.TestObject>(testDto);

            return testDtoId;
        }
    }
}
