using Gravity.DAL.RSAPI;
using Relativity.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gravity.Utils;
using Gravity.Base;
using kCura.Relativity.Client.DTOs;
using System.Reflection;
using Gravity.Extensions;

namespace Gravity.Test.Helpers
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

        public int CreateTestObjectWithGravity<T>(BaseDto testObject)
        {
            RsapiDao gravityRsapiDao = new RsapiDao(_servicesManager, _workspaceId, ExecutionIdentity.System, _retrySettings);

            int testDtoId = gravityRsapiDao.InsertRelativityObject<T>(testObject);

            return testDtoId;
        }

        public T ReturnTestObjectWithGravity<T>(int artifactId) where T : BaseDto, new()
        {
            RsapiDao gravityRsapiDao = new RsapiDao(_servicesManager, _workspaceId, ExecutionIdentity.System, _retrySettings);
            return gravityRsapiDao.GetRelativityObject<T>(artifactId,ObjectFieldsDepthLevel.FirstLevelOnly);
        }

		public static RDO GetStubRDO<T>(int artifactId) where T : BaseDto, new()
		{
			RelativityObjectAttribute objectTypeAttribute = typeof(T).GetCustomAttribute<RelativityObjectAttribute>(false);
			RDO stubRdo = new RDO(objectTypeAttribute.ObjectTypeGuid, artifactId);

			var fieldValues = BaseDto.GetFieldsGuids<T>().Select(x => new FieldValue(x, null));
			stubRdo.Fields.AddRange(fieldValues);

			return stubRdo;
		}
	}
}