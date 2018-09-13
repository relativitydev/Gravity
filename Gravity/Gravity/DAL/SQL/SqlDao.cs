using System;
using System.Collections.Generic;
using Gravity.Base;
using Gravity.Globals;
using Gravity.Utils;
using Relativity.API;

namespace Gravity.DAL.SQL
{
	public partial class SqlDao : IGravityDao
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

		#region SQL Dao Not Implemented operations

		[Obsolete("SQL Dao Insert is not implemented yet.", true)]
		public int Insert<T>(T obj)
			where T : BaseDto
		{
			throw new NotImplementedException();
		}

		[Obsolete("SQL Dao Get by artifactIDs is not implemented yet.", true)]
		public List<T> Get<T>(int[] artifactIDs, ObjectFieldsDepthLevel depthLevel)
			where T : BaseDto, new()
		{
			throw new NotImplementedException();
		}

		[Obsolete("SQL Dao Update is not implemented yet.", true)]
		public void Update<T>(T obj)
			where T : BaseDto
		{
			throw new NotImplementedException();
		}

		[Obsolete("SQL Dao Delete is not implemented yet.", true)]
		public void Delete<T>(int artifactID)
			where T : BaseDto, new()
		{
			throw new NotImplementedException();
		}

		[Obsolete("SQL Dao Delete is not implemented yet.", true)]
		public void Delete<T>(T obj)
			where T : BaseDto
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
