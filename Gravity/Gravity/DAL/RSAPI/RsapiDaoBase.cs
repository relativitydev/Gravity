using kCura.Relativity.Client;
using Relativity.API;
using Gravity.Globals;
using Gravity.Utils;

namespace Gravity.DAL.RSAPI
{
	using System;
	using System.Runtime.CompilerServices;
	using Gravity.Exceptions;
	public abstract class RsapiDaoBase
	{
		public ExecutionIdentity CurrentExecutionIdentity { get; set; }

		protected IServicesMgr servicesManager;
		protected int workspaceId;

		protected InvokeWithRetryService invokeWithRetryService;

		protected IRSAPIClient CreateProxy()
		{
			var proxy = servicesManager.CreateProxy<IRSAPIClient>(this.CurrentExecutionIdentity);
			proxy.APIOptions.WorkspaceID = workspaceId;

			return proxy;
		}

		public RsapiDaoBase(IServicesMgr servicesManager, int workspaceId, ExecutionIdentity executionIdentity, InvokeWithRetrySettings invokeWithRetrySettings = null)
		{
			this.servicesManager = servicesManager;
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

		#region InvokeProxyWithRetry
		/*
			These methods (and their overloads) replace the existing retry and error handling logic.
			When these functions are called, memberName will be replaced by the name of the calling
			function if not explicitly provided. This is what replaces the MethodInfo.GetCurrentMethod()
			method when this code was copied to each individual call.
		*/

		protected T InvokeProxyWithRetry<T>(IRSAPIClient proxy, Func<IRSAPIClient, T> func, [CallerMemberName] string memberName = null)
		{
			try
			{
				return invokeWithRetryService.InvokeWithRetry(() => func(proxy));
			}
			catch (Exception ex)
			{
				throw new ProxyOperationFailedException("Failed in method: " + memberName, ex);
			}
		}

		protected void InvokeProxyWithRetry(IRSAPIClient proxy, Action<IRSAPIClient> func, [CallerMemberName] string memberName = null)
		{
			try
			{
				invokeWithRetryService.InvokeVoidMethodWithRetry(() => func(proxy));
			}
			catch (Exception ex)
			{
				throw new ProxyOperationFailedException("Failed in method: " + memberName, ex);
			}
		}

		/*
			In addition to what the above methods do, these also create the proxy when it is only needed once.
		*/

		protected T InvokeProxyWithRetry<T>(Func<IRSAPIClient, T> func, [CallerMemberName] string memberName = null)
		{
			using (var proxy = CreateProxy())
			{
				return InvokeProxyWithRetry(proxy, func, memberName);
			}
		}

		protected void InvokeProxyWithRetry(Action<IRSAPIClient> func, [CallerMemberName] string memberName = null)
		{
			using (var proxy = CreateProxy())
			{
				InvokeProxyWithRetry(proxy, func, memberName);
			}
		}
		#endregion
	}
}