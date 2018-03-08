using System;
using Gravity.Utils;
using Relativity.API;

namespace Gravity.DAL.RSAPI
{
	public partial class RsapiDao : RsapiDaoBase
	{
		private const int DefaultBatchSize = 1000;

		private readonly int BatchSize;
	
		public RsapiDao(IServicesMgr servicesManager, int workspaceId, ExecutionIdentity executionIdentity,
				InvokeWithRetrySettings invokeWithRetrySettings = null, 
				int batchSize = DefaultBatchSize)
			: base(servicesManager, workspaceId, executionIdentity)

		{
			this.BatchSize = batchSize;
		}

	}
}