using System;
using System.Linq;
using Gravity.Extensions;
using Gravity.Utils;
using kCura.Relativity.Client;
using kCura.Relativity.Client.DTOs;

namespace Gravity.DAL.RSAPI
{
	public class ArtifactGuidCache : CacheBase<int>
	{
		private readonly IRsapiProvider rsapiProvider;

		public ArtifactGuidCache(IRsapiProvider rsapiProvider)
		{
			this.rsapiProvider = rsapiProvider;
		}

		public int Get(Guid artifactGuid)
		{
			string cacheKey = artifactGuid.ToString();
			if (!TryGetInner(cacheKey, out int artifactId))
			{
				artifactId = this.rsapiProvider.Read(new RDO((int)ArtifactType.Field, artifactGuid)).GetResultData().Single().ArtifactID;
				AddInner(artifactGuid.ToString(), artifactId);
			}
			return artifactId;
		}
	}
}
