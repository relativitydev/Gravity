using kCura.Relativity.Client;
using Relativity.API;
using Gravity.Globals;
using Gravity.Utils;

namespace Gravity.DAL.RSAPI
{
	using System;
	using System.Runtime.CompilerServices;
	using Gravity.Exceptions;
	using Polly;

	public partial class RsapiDao
	{

		public ExecutionIdentity CurrentExecutionIdentity { get; set; }

		protected IHelper helper;
		protected int workspaceId;
		
		private Policy proxyPolicy;
		private Policy filePolicy;

		protected IRSAPIClient CreateProxy()
		{
			var proxy = helper.GetServicesManager().CreateProxy<IRSAPIClient>(this.CurrentExecutionIdentity);
			proxy.APIOptions.WorkspaceID = workspaceId;

			return proxy;
		}

        /// <summary>
        /// Creates an instance of the RsapiDao class using default InvokeWithRetry policies for RSAPI and File IO failures.
        /// </summary>
        public RsapiDao(IHelper helper, int workspaceId, ExecutionIdentity executionIdentity)
            : this(helper, workspaceId, executionIdentity, StandardPolicies.InvokeWithRetry(), StandardPolicies.InvokeWithRetry())
        {

        }

        /// <summary>
        /// Creates an instance of the RsapiDao class using separate policies for RSAPI and File IO failures.
        /// </summary>
        public RsapiDao(IHelper helper, int workspaceId, ExecutionIdentity executionIdentity, Policy proxyPolicy, Policy filePolicy)
		{
			this.helper = helper;
			this.workspaceId = workspaceId;
			this.CurrentExecutionIdentity = executionIdentity;
			this.proxyPolicy = proxyPolicy;
			this.filePolicy = filePolicy;
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
				return this.proxyPolicy.Execute(() => func(proxy));
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
				this.proxyPolicy.Execute(() => func(proxy));
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