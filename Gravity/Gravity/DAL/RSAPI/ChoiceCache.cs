using Gravity.Extensions;
using Gravity.Utils;
using kCura.Relativity.Client;
using kCura.Relativity.Client.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace Gravity.DAL.RSAPI
{
    public class ChoiceCache : CacheBase<System.Collections.IDictionary>
    {
		private readonly IRsapiProvider rsapiProvider;

		public ChoiceCache(IRsapiProvider rsapiProvider)
		{
			this.rsapiProvider = rsapiProvider;
		}

		private Dictionary<int, T> GetEnumDictionary<T>()
		{
			// each type needs its own dictionary, obviously
			// but each instance does too, since they could point to, e.g. different workspaces, and thus have different IDs.

			var cacheKey = typeof(T).GUID.ToString();

			if (GetInner(cacheKey) is Dictionary<int, T> cacheItem)
			{
				return cacheItem;
			}

			var set = EnumHelpers.GetAttributesForValues<T, RelativityObjectAttribute>().Where(x => x.Value != null).ToList();
			var rdosToRead = set.Select(x => new RDO(x.Value.ObjectTypeGuid) { ArtifactTypeID = (int)ArtifactType.Code }).ToList();
			var choices = rsapiProvider.Read(rdosToRead).GetResultData();

			var newCacheItem = Enumerable.Range(0, set.Count).ToDictionary(
				i => choices[i].ArtifactID,
				i => set[i].Key
			);

			AddInner(cacheKey, newCacheItem);
			return newCacheItem;
		}

		public T GetEnum<T>(int artifactId)
		{
			if (!typeof(T).IsEnum)
			{
				throw new NotSupportedException($"{typeof(T).Name} does not represent an enumeration");
			}

			if (!GetEnumDictionary<T>().TryGetValue(artifactId, out T value))
			{
				throw new InvalidOperationException($"No choices of {typeof(T).Name} have the ArtifactId {artifactId}");
			}

			return value;
		}
    }
}
