using kCura.Relativity.Client;
using Relativity.API;
using Gravity.Globals;
using Gravity.Utils;

namespace Gravity.DAL.RSAPI
{
	public partial class RsapiDao
	{
		protected IHelper helper;
		protected int workspaceId;

		protected IRSAPIClient CreateProxy()
		{
			var proxy = helper.GetServicesManager().CreateProxy<IRSAPIClient>(ExecutionIdentity.System);
			proxy.APIOptions.WorkspaceID = workspaceId;

			return proxy;
		}

		private InvokeWithRetryService invokeWithRetryService;

		public RsapiDao(IHelper helper, int workspaceId, InvokeWithRetrySettings invokeWithRetrySettings = null)
		{
			this.helper = helper;
			this.workspaceId = workspaceId;

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
	}
}