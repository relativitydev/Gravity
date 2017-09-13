using kCura.Data.RowDataGateway;
using kCura.Relativity.Client;
using Relativity.API;
using System;
using System.Configuration;

namespace Gravity.Base
{
	public class AppConfigConnectionHelper : IHelper
	{
		string sqlServerHostName = ConfigurationManager.AppSettings["SQLServerHostName"];
		string sqlServerUsername = ConfigurationManager.AppSettings["SQLServerUsername"];
		string sqlServerPassword = ConfigurationManager.AppSettings["SQLServerPassword"];

		public IDBContext GetDBContext(int caseID)
		{
			if (caseID < 0)
			{
				return new DBContext(new Context(sqlServerHostName, "EDDS", sqlServerUsername, sqlServerPassword));
			}
			else
			{
				return new DBContext(new Context(sqlServerHostName, "EDDS" + caseID.ToString(), sqlServerUsername, sqlServerPassword));
			}
		}

		public IServicesMgr GetServicesManager()
		{
			return new UnitTestServicesManager();
		}

		public IUrlHelper GetUrlHelper()
		{
			throw new NotImplementedException();
		}

		public ILogFactory GetLoggerFactory()
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}
	}

	public class UnitTestServicesManager : IServicesMgr
	{
		string rsapiUrl = ConfigurationManager.AppSettings["RsapiUrl"];
		string rsapiUsername = ConfigurationManager.AppSettings["RsapiUsername"];
		string rsapiPassword = ConfigurationManager.AppSettings["RsapiPassword"];

		// TODO: Get the ident thing sorted out one day
		public T CreateProxy<T>(ExecutionIdentity ident) where T : IDisposable
		{
			var proxy = new RSAPIClient(
				new Uri(rsapiUrl),
				new UsernamePasswordCredentials(rsapiUsername, rsapiPassword)) as IRSAPIClient;

			return (T)proxy;
		}

		public Uri GetRESTServiceUrl()
		{
			throw new NotImplementedException();
		}

		public Uri GetServicesURL()
		{
			throw new NotImplementedException();
		}
	}
}
