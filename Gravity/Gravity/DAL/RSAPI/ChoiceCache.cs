﻿using Gravity.Extensions;
using kCura.Relativity.Client.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace Gravity.DAL.RSAPI
{
    public class ChoiceCache
    {
		private static readonly CacheItemPolicy CachePolicy = new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMinutes(15) };
		private static readonly MemoryCache Cache = new MemoryCache(nameof(ChoiceCache));
		private readonly Guid CacheInstanceId = Guid.NewGuid();


		private readonly IRsapiProvider rsapiProvider;

		public ChoiceCache(IRsapiProvider rsapiProvider)
		{
			this.rsapiProvider = rsapiProvider;
		}

		private Dictionary<int, T> GetEnumDictionary<T>()
		{
			var cacheKey = $"{CacheInstanceId}_{typeof(T).GUID}";
			if (Cache.Get(cacheKey) is Dictionary<int, T> cacheItem)
			{
				return cacheItem;
			}

			var set = EnumHelpers.GetAttributesForValues<T, RelativityObjectAttribute>().Where(x => x.Value != null).ToList();
			var choices = rsapiProvider.Read(set.Select(x => new RDO(x.Value.ObjectTypeGuid)).ToList()).GetResultData();

			var newCacheItem = Enumerable.Range(0, set.Count).ToDictionary(
				i => choices[i].ArtifactID,
				i => set[i].Key
			);

			Cache.Add(cacheKey, newCacheItem, CachePolicy);
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