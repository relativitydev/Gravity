using System;
using System.Collections.Generic;
using Gravity.Base;
using Gravity.Utils;
using Relativity.API;

namespace Gravity.DAL.SQL
{
	public partial class SqlDao : IGravityDao
	{
		private const int defaultBatchSize = 1000;

		protected int workspaceId;
		protected IDBContext dbContext;
		protected IDBContext masterDbContext;
		protected int batchSize;

		private InvokeWithRetryService invokeWithRetryService;

		private SqlDao(IDBContext workspaceContext, IDBContext masterContext)
		{
			dbContext = workspaceContext;
			masterDbContext = masterContext;
		}

		public SqlDao(IHelper helper, int workspaceId, InvokeWithRetrySettings invokeWithRetrySettings = null)
			: this(helper.GetDBContext(workspaceId), helper.GetDBContext(-1))
		{
			this.workspaceId = workspaceId;
			batchSize = defaultBatchSize;
			invokeWithRetryService = InvokeWithRetryService.GetInvokeWithRetryService(invokeWithRetrySettings);
		}

		#region SQL Dao Not Implemented operations

		[Obsolete("SQL Dao Insert is not implemented yet.", true)]
		public int Insert<T>(T obj, ObjectFieldsDepthLevel depthLevel)
			where T : BaseDto => throw new NotImplementedException();

		[Obsolete("SQL Dao Bulk Insert is not implemented yet.", true)]
		public void Insert<T>(IList<T> objs, ObjectFieldsDepthLevel depthLevel)
			where T : BaseDto => throw new NotImplementedException();

		[Obsolete("SQL Dao Bulk Get by artifactIDs is not implemented yet.", true)]
		public List<T> Get<T>(IList<int> artifactIDs, ObjectFieldsDepthLevel depthLevel)
			where T : BaseDto, new() => throw new NotImplementedException();

		[Obsolete("SQL Dao Update is not implemented yet.", true)]
		public void Update<T>(T obj, ObjectFieldsDepthLevel depthLevel)
			where T : BaseDto => throw new NotImplementedException();

		[Obsolete("SQL Dao Bulk Update is not implemented yet.", true)]
		public void Update<T>(IList<T> objs, ObjectFieldsDepthLevel depthLevel)
			where T : BaseDto => throw new NotImplementedException();

		[Obsolete("SQL Dao Delete is not implemented yet.", true)]
		public void Delete<T>(int artifactID, ObjectFieldsDepthLevel depthLevel)
			where T : BaseDto, new() => throw new NotImplementedException();

		#endregion
	}
}
