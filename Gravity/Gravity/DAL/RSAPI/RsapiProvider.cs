using Gravity.Base;
using Gravity.Exceptions;
using Gravity.Utils;
using kCura.Relativity.Client;
using kCura.Relativity.Client.DTOs;
using kCura.Relativity.Client.Repositories;
using Relativity.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Gravity.DAL.RSAPI
{
	public class RsapiProvider : IRsapiProvider
	{

		private readonly InvokeWithRetryService invokeWithRetryService;
		private readonly IServicesMgr servicesManager;
		private readonly int workspaceId;
		private readonly int batchSize;

		public ExecutionIdentity CurrentExecutionIdentity { get; }

		public RsapiProvider(IServicesMgr servicesManager, ExecutionIdentity executionIdentity, InvokeWithRetryService invokeWithRetryService,
			int workspaceId, int batchSize)
		{
			this.servicesManager = servicesManager;
			this.workspaceId = workspaceId;
			this.CurrentExecutionIdentity = executionIdentity;
			this.invokeWithRetryService = invokeWithRetryService;
			this.batchSize = batchSize;

		}

		private IRSAPIClient CreateProxy()
		{
			var proxy = servicesManager.CreateProxy<IRSAPIClient>(this.CurrentExecutionIdentity);
			proxy.APIOptions.WorkspaceID = workspaceId;

			return proxy;
		}

		#region File Operations

		public void ClearFile(int fieldId, int objectArtifactId)
		{
			using (IRSAPIClient proxyToWorkspace = CreateProxy())
			{
				var fileRequest = new FileRequest(proxyToWorkspace.APIOptions)
				{
					Target =
					{
						FieldId = fieldId,
						ObjectArtifactId = objectArtifactId
					}
				};

				InvokeProxyWithRetry(proxyToWorkspace, proxy => proxy.Clear(fileRequest));
			}
		}

		public Tuple<FileMetadata, MemoryStream> DownloadFile(int fieldId, int objectArtifactId)
		{
			using (IRSAPIClient proxyToWorkspace = CreateProxy())
			{
				var fileRequest = new FileRequest(proxyToWorkspace.APIOptions)
				{
					Target =
					{
						FieldId = fieldId,
						ObjectArtifactId = objectArtifactId
					}
				};

				return InvokeProxyWithRetry(proxyToWorkspace, proxy => {
					var download = proxy.Download(fileRequest);
					return Tuple.Create(download.Key.Metadata, (MemoryStream)download.Value);
				});
			}
		}

		public void UploadFile(int fieldId, int parentId, string fileName)
		{
			using (IRSAPIClient proxyToWorkspace = CreateProxy())
			{
				var uploadRequest = new UploadRequest(proxyToWorkspace.APIOptions)
				{
					Metadata =
					{
						FileName = fileName,
						FileSize = new FileInfo(fileName).Length
					},
					Overwrite = true,
					Target =
					{
						FieldId = fieldId,
						ObjectArtifactId = parentId
					}
				};
				InvokeProxyWithRetry(proxyToWorkspace, proxy => proxy.Upload(uploadRequest));
			}
		}

		#endregion

		#region RDO CRUD

		/// <summary>
		/// Runs an RSAPI Query in pages.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <returns>An enumerable that yields each batch result set</returns>
		public IEnumerable<QueryResultSet<RDO>> Query(Query<RDO> query)
		{
			var initialResultSet = InvokeRepositoryWithRetry(x => x.Query(query));
			yield return initialResultSet;

			string queryToken = initialResultSet.QueryToken;

			// Iterate though all remaining pages 
			var totalCount = initialResultSet.TotalCount;
			int currentPosition = batchSize + 1;

			while (currentPosition <= totalCount)
			{
				yield return InvokeRepositoryWithRetry(x => x.QuerySubset(queryToken, currentPosition, batchSize));
				currentPosition += batchSize;
			}
		}

		// Effectively implement IGenericRepository<RDO>, so that we won't need in the application

		public WriteResultSet<RDO> Create(params RDO[] artifacts) => InvokeRepositoryWithRetry(x => x.Create(artifacts));

		public int CreateSingle(RDO artifact) => InvokeRepositoryWithRetry(x => x.CreateSingle(artifact));

		public ResultSet<RDO> Read(params RDO[] artifacts) => InvokeRepositoryWithRetry(x => x.Read(artifacts));

		public RDO ReadSingle(int artifactID) => InvokeRepositoryWithRetry(x => x.ReadSingle(artifactID));

		public RDO ReadSingle(Guid artifactGuid) => InvokeRepositoryWithRetry(x => x.ReadSingle(artifactGuid));

		public WriteResultSet<RDO> Update(params RDO[] artifacts) => InvokeRepositoryWithRetry(x => x.Update(artifacts));

		public void UpdateSingle(RDO artifact) => InvokeRepositoryWithRetry(x => x.UpdateSingle(artifact));

		public WriteResultSet<RDO> Delete(params RDO[] artifacts) => InvokeRepositoryWithRetry(x => x.Delete(artifacts));

		public void DeleteSingle(int artifactID) => InvokeRepositoryWithRetry(x => x.DeleteSingle(artifactID));

		public void DeleteSingle(Guid artifactGuid) => InvokeRepositoryWithRetry(x => x.DeleteSingle(artifactGuid));

		public ResultSet<RDO> Read(params int[] artifactIDs) => InvokeRepositoryWithRetry(x => x.Read(artifactIDs));

		public WriteResultSet<RDO> Delete(params int[] artifactIDs) => InvokeRepositoryWithRetry(x => x.Delete(artifactIDs));

		public ResultSet<RDO> Read(List<int> artifactIDs) => InvokeRepositoryWithRetry(x => x.Read(artifactIDs));

		public WriteResultSet<RDO> Delete(List<int> artifactIDs) => InvokeRepositoryWithRetry(x => x.Delete(artifactIDs));

		public WriteResultSet<RDO> Create(List<RDO> artifacts) => InvokeRepositoryWithRetry(x => x.Create(artifacts));

		public ResultSet<RDO> Read(List<RDO> artifacts) => InvokeRepositoryWithRetry(x => x.Read(artifacts));

		public WriteResultSet<RDO> Update(List<RDO> artifacts) => InvokeRepositoryWithRetry(x => x.Update(artifacts));

		public WriteResultSet<RDO> Delete(List<RDO> artifacts) => InvokeRepositoryWithRetry(x => x.Delete(artifacts));

		public WriteResultSet<RDO> MassEdit(RDO templateArtifact, List<int> artifactIDs) => InvokeRepositoryWithRetry(x => x.MassEdit(templateArtifact, artifactIDs));

		public WriteResultSet<RDO> MassCreate(RDO templateArtifact, List<RDO> artifacts) => InvokeRepositoryWithRetry(x => x.MassCreate(templateArtifact, artifacts));

		#endregion

		#region InvokeProxyWithRetry/InvokeRepositoryWithRetry

		/*
			These methods (and their overloads) replace the existing retry and error handling logic.
			When these functions are called, memberName will be replaced by the name of the calling
			function if not explicitly provided. This is what replaces the MethodInfo.GetCurrentMethod()
			method when this code was copied to each individual call.
		*/

		private T InvokeProxyWithRetry<T>(IRSAPIClient proxy, Func<IRSAPIClient, T> func, [CallerMemberName] string memberName = null)
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

		private void InvokeProxyWithRetry(IRSAPIClient proxy, Action<IRSAPIClient> func, [CallerMemberName] string memberName = null)
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

		private void InvokeRepositoryWithRetry(Action<RDORepository> func, [CallerMemberName] string memberName = null)
		{
			using (var proxy = CreateProxy())
			{
				InvokeProxyWithRetry(proxy, x => func(x.Repositories.RDO), memberName);
			}
		}

		private T InvokeRepositoryWithRetry<T>(Func<RDORepository, T> func, [CallerMemberName] string memberName = null)
		{
			using (var proxy = CreateProxy())
			{
				return InvokeProxyWithRetry(proxy, x => func(x.Repositories.RDO), memberName);
			}
		}

		#endregion
	}
}
