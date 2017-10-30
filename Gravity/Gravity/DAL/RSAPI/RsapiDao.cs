using kCura.Relativity.Client;
using Relativity.API;
using Gravity.Globals;
using Gravity.Utils;

namespace Gravity.DAL.RSAPI
{
	using System;

	public partial class RsapiDao
	{
		public ExecutionIdentity CurrentExecutionIdentity { get; set; }

		protected IHelper helper;
		protected int workspaceId;
		
		private InvokeWithRetryService invokeWithRetryService;

		protected IRSAPIClient CreateProxy()
		{
			var proxy = helper.GetServicesManager().CreateProxy<IRSAPIClient>(this.CurrentExecutionIdentity);
			proxy.APIOptions.WorkspaceID = workspaceId;

			return proxy;
		}
		
		public RsapiDao(IHelper helper, int workspaceId, ExecutionIdentity executionIdentity, InvokeWithRetrySettings invokeWithRetrySettings = null)
		{
			this.helper = helper;
			this.workspaceId = workspaceId;
			this.CurrentExecutionIdentity = executionIdentity;

			if (invokeWithRetrySettings == null)
			{
				InvokeWithRetrySettings defaultSettings = new InvokeWithRetrySettings(SharedConstants.retryAttempts, SharedConstants.sleepTimeInMiliseconds);
				this.invokeWithRetryService = new InvokeWithRetryService(defaultSettings);
			}
			else
			{
				this.invokeWithRetryService = new InvokeWithRetryService(invokeWithRetrySettings);
			}
		}

		[Obsolete("This constructor has been deprecated. Use RsapiDao(IHelper helper, int workspaceId, ExecutionIdentity executionIdentity, InvokeWithRetrySettings invokeWithRetrySettings) instead.")]
		public RsapiDao(IHelper helper, int workspaceId, InvokeWithRetrySettings invokeWithRetrySettings = null)
			: this(helper, workspaceId, ExecutionIdentity.System, invokeWithRetrySettings)
		{
		}
	}
}