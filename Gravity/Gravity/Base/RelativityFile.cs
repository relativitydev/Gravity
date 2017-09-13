using kCura.Relativity.Client;
using System;

namespace Gravity.Base
{
	[Serializable]
	public class RelativityFile
	{
		public RelativityFile()
		{ }

		public RelativityFile(int artifactTypeID)
		{
			this.ArtifactTypeId = artifactTypeID;
		}

		public RelativityFile(int artifactTypeID, FileValue fieldValue, FileMetadata fileMetadata )
		{
			this.ArtifactTypeId = artifactTypeID;
			this.FileMetadata = fileMetadata;
			this.FileValue = fieldValue;
		}

		public int ArtifactTypeId { get; set; }

		public FileValue FileValue { get; set; }

		public FileMetadata FileMetadata { get; set; }
	}
}
