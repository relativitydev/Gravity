using System;

namespace Gravity.Base
{
	[Serializable]
	public abstract class BaseMasterDto : BaseDto
	{
		// This is the guy that carries the info (Artifact ID) for this DTO from the EDDS/Master database
		// Example: Client, Workspace, User, Group (they all have Admin/Home/Master Artifact ID that is unique for the Relativity instance
		public abstract int MasterArtifactId { get; set; }

		public abstract bool Deleted { get; set; }

		public BaseMasterDto()
			: base()
		{
		}
	}
}
