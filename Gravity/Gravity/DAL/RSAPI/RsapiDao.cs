using System;
using Gravity.Utils;
using Relativity.API;

namespace Gravity.DAL.RSAPI
{
	public partial class RsapiDao : RsapiDaoBase
	{
		private const int DefaultBatchSize = 1000;

		private readonly int BatchSize;
	
		public RsapiDao(IHelper helper, int workspaceId, ExecutionIdentity executionIdentity,
				InvokeWithRetrySettings invokeWithRetrySettings = null, 
				int batchSize = DefaultBatchSize)
			: base(helper, workspaceId, executionIdentity)

		{
			this.BatchSize = batchSize;
		}

		[Obsolete("This constructor has been deprecated. Use RsapiDao(IHelper helper, int workspaceId, ExecutionIdentity executionIdentity, InvokeWithRetrySettings invokeWithRetrySettings, int batchSize) instead.")]
		public RsapiDao(IHelper helper, int workspaceId, InvokeWithRetrySettings invokeWithRetrySettings = null)
			: this(helper, workspaceId, ExecutionIdentity.System, invokeWithRetrySettings)
		{
		}
	}
}