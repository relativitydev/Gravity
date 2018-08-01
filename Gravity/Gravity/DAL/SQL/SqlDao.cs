using Gravity.Globals;
using Gravity.Utils;
using Relativity.API;

namespace Gravity.DAL.SQL
{
	public partial class SqlDao
	{
		private const int DefaultBatchSize = 1000;

		protected int workspaceId;
		protected IHelper helper;
		protected IDBContext dbContext;
		protected IDBContext masterDbContext;
		protected int batchSize;

		private InvokeWithRetryService invokeWithRetryService;

		public SqlDao(IHelper helper, int workspaceId, InvokeWithRetrySettings invokeWithRetrySettings = null)
		{
			this.helper = helper;
			this.workspaceId = workspaceId;
			this.dbContext = helper.GetDBContext(workspaceId);
			this.masterDbContext = helper.GetDBContext(-1);
			this.batchSize = DefaultBatchSize;

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
