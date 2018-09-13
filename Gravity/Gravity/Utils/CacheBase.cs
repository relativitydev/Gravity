using System;
using System.Linq;
using System.Runtime.Caching;

namespace Gravity.Utils
{
	public abstract class CacheBase<TValue>
	{
		private static readonly CacheItemPolicy CachePolicy = new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMinutes(15) };

		// caches are recommended to be static across all members of the class
		private static readonly MemoryCache Cache = new MemoryCache(nameof(CacheBase<TValue>));

		// we will use this below to ensure each cache gets a unique address in the Cache object
		private readonly Guid CacheInstanceId = Guid.NewGuid();

		private string GetCacheKey(string key) => $"{CacheInstanceId}_{key}";

		protected TValue GetInner(string key) => (TValue)Cache.Get(GetCacheKey(key));

		protected bool TryGetInner(string key, out TValue value)
		{
			if (Cache.Get(GetCacheKey(key)) is TValue foundValue)
			{
				value = foundValue;
				return true;
			}
			value = default(TValue);
			return false;
		}

		protected bool AddInner(string key, TValue value) => Cache.Add(GetCacheKey(key), value, CachePolicy);

		protected void SetInner(string key, TValue value) => Cache.Set(GetCacheKey(key), value, CachePolicy);
	}
}
