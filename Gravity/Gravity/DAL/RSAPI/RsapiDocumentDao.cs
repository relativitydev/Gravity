using kCura.Relativity.Client;
using kCura.Relativity.Client.DTOs;
using System.Collections.Generic;
using System.IO;
using Relativity.API;
using Gravity.Utils;

namespace Gravity.DAL.RSAPI
{
	public class RsapiDocumentDao : RsapiDaoBase
	{
		public RsapiDocumentDao(IHelper helper, int workspaceId, ExecutionIdentity executionIdentity, InvokeWithRetrySettings invokeWithRetrySettings = null)
			: base(helper, workspaceId, executionIdentity)
		{
		}

		public ResultSet<Document> QueryDocumentsByDocumentViewID(int documentViewId)
		{
			Query<Document> query = new Query<Document>()
			{
				Condition = new ViewCondition(documentViewId),
				Fields = FieldValue.SelectedFields
			};

			return InvokeProxyWithRetry(proxy => proxy.Repositories.Document.Query(query));
		}

		public KeyValuePair<byte[], FileMetadata> DownloadDocumentNative(int documentId)
		{
			Document doc = new Document(documentId);
			byte[] documentBytes;

			KeyValuePair<DownloadResponse, Stream> documentNativeResponse
				= InvokeProxyWithRetry(proxy => proxy.Repositories.Document.DownloadNative(doc));

			using (MemoryStream ms = (MemoryStream)documentNativeResponse.Value)
			{
				documentBytes = ms.ToArray();
			}

			return new KeyValuePair<byte[], FileMetadata>(documentBytes, documentNativeResponse.Key.Metadata);
		}
	}
}
