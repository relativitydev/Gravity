using System;
using System.Linq;
using Gravity.Utils;

namespace Gravity.DAL.RSAPI
{
	public class FileMD5Cache : CacheBase<string>
	{
		private readonly IRsapiProvider rsapiProvider;

		public FileMD5Cache(IRsapiProvider rsapiProvider)
		{
			this.rsapiProvider = rsapiProvider;
		}

		private string GetCacheKey(Guid fieldGuid, int artifactId) => $"{artifactId}:{fieldGuid}";

		public string Get(Guid fieldGuid, int artifactId)
			=> GetInner(GetCacheKey(fieldGuid, artifactId));

		public void Set(Guid fieldGuid, int artifactId, string MD5)
			=> SetInner(GetCacheKey(fieldGuid, artifactId), MD5);
	}
}
