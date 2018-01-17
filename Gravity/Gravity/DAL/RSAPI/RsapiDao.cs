using System;
using Gravity.Utils;
using Relativity.API;

namespace Gravity.DAL.RSAPI
{
	public partial class RsapiDao : RsapiDaoBase
	{
		public RsapiDao(IHelper helper, int workspaceId, ExecutionIdentity executionIdentity, InvokeWithRetrySettings invokeWithRetrySettings = null)
			: base(helper, workspaceId, executionIdentity)
		{
		}

		[Obsolete("This constructor has been deprecated. Use RsapiDao(IHelper helper, int workspaceId, ExecutionIdentity executionIdentity, InvokeWithRetrySettings invokeWithRetrySettings) instead.")]
		public RsapiDao(IHelper helper, int workspaceId, InvokeWithRetrySettings invokeWithRetrySettings = null)
			: this(helper, workspaceId, ExecutionIdentity.System, invokeWithRetrySettings)
		{
		}
	}
}