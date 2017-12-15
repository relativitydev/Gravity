using kCura.Relativity.Client;
using kCura.Relativity.Client.DTOs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Gravity.Base;
using Gravity.Exceptions;
using Gravity.Extensions;

namespace Gravity.DAL.RSAPI
{
	public partial class RsapiDao
	{
		public ResultSet<Document> QueryDocumentsByDocumentViewID(int documentViewId)
		{
			ResultSet<Document> returnObject;

			Query<Document> query = new Query<Document>()
			{
				Condition = new ViewCondition(documentViewId),
				Fields = FieldValue.SelectedFields
			};

			using (IRSAPIClient proxy = CreateProxy())
			{
				try
				{
					returnObject = invokeWithRetryService.InvokeWithRetry(() => proxy.Repositories.Document.Query(query));
				}
				catch (Exception ex)
				{
					throw new ProxyOperationFailedException("Failed in method: " + MethodBase.GetCurrentMethod(), ex);
				}
			}

			return returnObject;
		}

		public KeyValuePair<byte[], FileMetadata> DownloadDocumentNative(int documentId)
		{
			Document doc = new Document(documentId);
			byte[] documentBytes;

			KeyValuePair<DownloadResponse, Stream> documentNativeResponse = new KeyValuePair<DownloadResponse, Stream>();

			using (IRSAPIClient proxy = CreateProxy())
			{
				try
				{
					documentNativeResponse = invokeWithRetryService.InvokeWithRetry(() => proxy.Repositories.Document.DownloadNative(doc));
				}
				catch (Exception ex)
				{
					throw new ProxyOperationFailedException("Failed in method: " + MethodInfo.GetCurrentMethod(), ex);
				}
			}

			using (MemoryStream ms = (MemoryStream)documentNativeResponse.Value)
			{
				documentBytes = ms.ToArray();
			}

			return new KeyValuePair<byte[], FileMetadata>(documentBytes, documentNativeResponse.Key.Metadata);
		}
	}
}
