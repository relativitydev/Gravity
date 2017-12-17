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
