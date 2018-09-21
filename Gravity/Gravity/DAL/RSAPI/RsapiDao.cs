using System;
using Gravity.Globals;
using Gravity.Utils;
using Relativity.API;

namespace Gravity.DAL.RSAPI
{
	public partial class RsapiDao : IGravityDao
	{
		private const int DefaultBatchSize = 1000;

		protected InvokeWithRetryService invokeWithRetryService;
		protected IRsapiProvider rsapiProvider;
		protected ChoiceCache choiceCache;
		protected ArtifactGuidCache guidCache;
		protected FileMD5Cache fileMd5Cache;

		public RsapiDao(IServicesMgr servicesManager, int workspaceId, ExecutionIdentity executionIdentity,
				InvokeWithRetrySettings invokeWithRetrySettings = null,
				int batchSize = DefaultBatchSize)
			: this(servicesManager, workspaceId, executionIdentity, InvokeWithRetryService.GetInvokeWithRetryService(invokeWithRetrySettings), batchSize)

		{
		}

		private RsapiDao(IServicesMgr servicesManager, int workspaceId, ExecutionIdentity executionIdentity,
				InvokeWithRetryService invokeWithRetryService,
				int batchSize = DefaultBatchSize)
			: this(new RsapiProvider(servicesManager, executionIdentity, invokeWithRetryService, workspaceId, batchSize), invokeWithRetryService)
		{
		}

		public RsapiDao(IRsapiProvider rsapiProvider, InvokeWithRetryService invokeWithRetryService)
		{
			this.invokeWithRetryService = invokeWithRetryService;

			this.rsapiProvider = rsapiProvider;
			this.choiceCache = new ChoiceCache(this.rsapiProvider);
			this.guidCache = new ArtifactGuidCache(this.rsapiProvider);
			this.fileMd5Cache = new FileMD5Cache(this.rsapiProvider);
		}
	}
}