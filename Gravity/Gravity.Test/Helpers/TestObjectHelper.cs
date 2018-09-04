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

		public RsapiDao GetDao()
		{
			return new RsapiDao(_servicesManager, _workspaceId, ExecutionIdentity.System, _retrySettings);
		}

		public static RDO GetStubRDO<T>(int artifactId) where T : BaseDto
		{
			RelativityObjectAttribute objectTypeAttribute = typeof(T).GetCustomAttribute<RelativityObjectAttribute>(false);
			RDO stubRdo = new RDO(objectTypeAttribute.ObjectTypeGuid, artifactId);

			var fieldValues = BaseDto.GetFieldsGuids<T>().Select(x => new FieldValue(x, null));
			stubRdo.Fields.AddRange(fieldValues);

			return stubRdo;
		}

		public static Guid FieldGuid<T>(string fieldName)
			=> typeof(T).GetProperty(fieldName).GetCustomAttribute<RelativityObjectFieldAttribute>().FieldGuid;
	}
}