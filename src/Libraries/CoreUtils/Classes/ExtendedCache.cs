using System;
using System.IO;
using System.Runtime.Caching;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NeoSmart.Caching.Sqlite;

namespace CoreUtils.Classes
{

    public class ExtendedCache
    {
        private long _countOfSetOperations;

        public ExtendedCache(TimeSpan slidingExpiration, TimeSpan cleanupTimeSpan, ILogger<SqliteCache> logger = null)
        {
            //
            if (cleanupTimeSpan.TotalMinutes < 60) cleanupTimeSpan = TimeSpan.FromHours(1);
            Options = new SqliteCacheOptions
            {
                CachePath = $"{Path.GetTempFileName()}.sqlite",
                CleanupInterval = cleanupTimeSpan != null ? cleanupTimeSpan : TimeSpan.FromHours(1),
                MemoryOnly = true
            };

            //
            if (slidingExpiration.TotalMinutes < 60) slidingExpiration = TimeSpan.FromHours(1);

            CacheEntryOptions = new DistributedCacheEntryOptions
            {
                SlidingExpiration = slidingExpiration
            };

            //
            if (logger == null)
                Logger = null;
            else
                Logger = logger;
            try
            {
                DiskCache = new SqliteCache(Options, Logger);
            }
            catch (Exception)
            {
                // ignore - happens in Web! get error Exception: Library e_sqlite3 not found
                // plat: win
                // suffix: DLL
            }

            //
            MemoryCache = MemoryCache.Default;
        }

        // for large object storage to avoid overloading memory as it is slower
        private SqliteCache DiskCache { get; }

        // for string storage as it is faster
        private MemoryCache MemoryCache { get; }

        private SqliteCacheOptions Options { get; }

        public ILogger<SqliteCache> Logger { get; }

        public TimeSpan SlidingExpiration { get; } = TimeSpan.FromHours(1);

        public DistributedCacheEntryOptions CacheEntryOptions { get; }

        // stores string in DiskCache
        public object Add(string key, object item)
        {
            if (item == null)
                return null;

            //
            if (item is string || item is string || item is int || item is long || item is bool || DiskCache == null)
                return AddToMemoryCache(key, item);

            // remove expired every N sets
            _countOfSetOperations++;
            if (_countOfSetOperations >= 1000) DiskCache.RemoveExpiredAsync();

            var bf = new BinaryFormatter();
            using var ms = new MemoryStream();
            bf.Serialize(ms, item);
            var byteArray = ms.ToArray();

            DiskCache.Set(key, byteArray, CacheEntryOptions);

            // remove from MemoryCache so we don't have a previous invalid value returned
            MemoryCache.Remove(key);

            return item;
        }

        // stores string in MemoryCache
        public object Add(string key, string item)
        {
            if (item == null)
                return null;

            var dateTimeOffset =
                new DateTimeOffset(DateTime.UtcNow, TimeSpan.Zero).AddSeconds(SlidingExpiration.TotalSeconds);
            MemoryCache.Set(key, item, dateTimeOffset);

            return item;
        }

        public object AddToMemoryCache(string key, object item)
        {
            if (item == null)
                return null;

            var dateTimeOffset =
                new DateTimeOffset(DateTime.UtcNow, TimeSpan.Zero).AddSeconds(SlidingExpiration.TotalSeconds);
            MemoryCache.Set(key, item, dateTimeOffset);

            return item;
        }

        public object Get(string key)
        {
            // try mem cache
            var value = MemoryCache.Get(key);
            if (value != null) return value;

            if (DiskCache == null) return null;
            // try DiskCache
            var byteArray = DiskCache.Get(key);
            if (byteArray == null || byteArray.Length == 0) return null;
            //
            using var memStream = new MemoryStream();
            var binForm = new BinaryFormatter();
            memStream.Write(byteArray, 0, byteArray.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            //
            var item = binForm.Deserialize(memStream);
            return item;
        }

        public bool ContainsKey(string key)
        {
            // try mem cache
            var item = MemoryCache.Get(key);
            if (item != null) return true;

            if (DiskCache == null) return false;

            // try DiskCache
            var byteArray = DiskCache.Get(key);
            if (byteArray == null || byteArray.Length == 0) return false;

            return true;
        }
    }

}