using kCura.Data.RowDataGateway;
using Relativity.API;
using System;
using System.Configuration;

namespace Gravity.Test.Helpers
{
	public class AppConfigConnectionHelper : IHelper
	{
		public void Dispose()
		{
			this.Dispose();
		}

		public IDBContext GetDBContext(int caseID)
		{
			string sqlServerHostName = ConfigurationManager.AppSettings["SQLServerAddress"];
			string sqlServerUsername = ConfigurationManager.AppSettings["SQLUsername"];
			string sqlServerPassword = ConfigurationManager.AppSettings["SQLPassword"];

			if (caseID < 0)
			{
				return new DBContext(new Context(sqlServerHostName, "EDDS", sqlServerUsername, sqlServerPassword));
			}
			else
			{
				return new DBContext(new Context(sqlServerHostName, "EDDS" + caseID.ToString(), sqlServerUsername, sqlServerPassword));
			}

		}

		public Guid GetGuid(int workspaceID, int artifactID)
		{
			throw new NotImplementedException();
		}

		public ILogFactory GetLoggerFactory()
		{
			throw new NotImplementedException();
		}

		public string GetSchemalessResourceDataBasePrepend(IDBContext context)
		{
			throw new NotImplementedException();
		}

		public IServicesMgr GetServicesManager()
		{
			throw new NotImplementedException();
		}

		public IUrlHelper GetUrlHelper()
		{
			throw new NotImplementedException();
		}

		public string ResourceDBPrepend()
		{
			throw new NotImplementedException();
		}

		public string ResourceDBPrepend(IDBContext context)
		{
			throw new NotImplementedException();
		}
	}
}