using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Orleans.Configuration;
using Orleans.Runtime;
using Orleans.Runtime.GrainDirectory;

namespace UnitTests.General
{
    public class TestDirectoryCache : IGrainDirectoryCache
    {
        public TestDirectoryCache(IServiceProvider serviceProvider)
        {
            var options = new GrainDirectoryOptions();
            InnerCache = GrainDirectoryCacheFactory.CreateGrainDirectoryCache(serviceProvider, options);
        }

        public IGrainDirectoryCache InnerCache { get; }

        public ConcurrentQueue<CacheOperation> Operations { get; } = new();

        public IEnumerable<(ActivationAddress ActivationAddress, int Version)> KeyValues => InnerCache.KeyValues;

        public void AddOrUpdate(ActivationAddress value, int version)
        {
            InnerCache.AddOrUpdate(value, version);
            Operations.Enqueue(new CacheOperation.AddOrUpdate(value, version));
        }

        public void Clear()
        {
            InnerCache.Clear();
            Operations.Enqueue(new CacheOperation.Clear());
        }

        public bool LookUp(GrainId key, out ActivationAddress result, out int version)
        {
            var exists = InnerCache.LookUp(key, out result, out version);
            Operations.Enqueue(new CacheOperation.Lookup(key, (exists, result, version)));
            return exists;
        }

        public bool Remove(GrainId key)
        {
            var exists = InnerCache.Remove(key);
            Operations.Enqueue(new CacheOperation.Remove(key, exists));
            return exists;
        }

        /// <summary>
        /// Removes an entry from the cache given its key
        /// </summary>
        /// <param name="key">key to remove</param>
        /// <returns>True if the entry was in the cache and the removal was successful</returns>
        public bool Remove(ActivationAddress key)
        {
            var exists = InnerCache.Remove(key);
            Operations.Enqueue(new CacheOperation.RemoveActivation(key, exists));
            return exists;
        }

        public record CacheOperation()
        {
            public record RemoveActivation(ActivationAddress Key, bool Result) : CacheOperation;
            public record Remove(GrainId Key, bool Result) : CacheOperation;
            public record Lookup(GrainId Key, (bool Exists, ActivationAddress Address, int Version) Result) : CacheOperation;
            public record AddOrUpdate(ActivationAddress Value, int Version) : CacheOperation;
            public record Clear() : CacheOperation;
        }
    }
}