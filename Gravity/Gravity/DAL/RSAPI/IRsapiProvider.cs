using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gravity.Base;
using kCura.Relativity.Client;
using kCura.Relativity.Client.DTOs;
using Relativity.API;

namespace Gravity.DAL.RSAPI
{
	public interface IRsapiProvider
	{
		void ClearFile(int fieldId, int objectArtifactId);
		WriteResultSet<RDO> Create(params RDO[] artifacts);
		WriteResultSet<RDO> Create(List<RDO> artifacts);
		int CreateSingle(RDO artifact);
		WriteResultSet<RDO> Delete(params RDO[] artifacts);
		WriteResultSet<RDO> Delete(params int[] artifactIDs);
		WriteResultSet<RDO> Delete(List<RDO> artifacts);
		WriteResultSet<RDO> Delete(List<int> artifactIDs);
		void DeleteSingle(Guid artifactGuid);
		void DeleteSingle(int artifactID);
		Tuple<FileMetadata, MemoryStream> DownloadFile(int fieldId, int objectArtifactId);
		WriteResultSet<RDO> MassCreate(RDO templateArtifact, List<RDO> artifacts);
		WriteResultSet<RDO> MassEdit(RDO templateArtifact, List<int> artifactIDs);

		/// <summary>
		/// Runs an RSAPI Query in pages.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <returns>An enumerable that yields each batch result set</returns>
		IEnumerable<QueryResultSet<RDO>> Query(Query<RDO> query);
		ResultSet<RDO> Read(params int[] artifactIDs);
		ResultSet<RDO> Read(params RDO[] artifacts);
		ResultSet<RDO> Read(List<int> artifactIDs);
		ResultSet<RDO> Read(List<RDO> artifacts);
		RDO ReadSingle(Guid artifactGuid);
		RDO ReadSingle(int artifactID);
		WriteResultSet<RDO> Update(params RDO[] artifacts);
		WriteResultSet<RDO> Update(List<RDO> artifacts);
		void UpdateSingle(RDO artifact);
		void UploadFile(int fieldId, int parentId, string fileName);
	}
}
