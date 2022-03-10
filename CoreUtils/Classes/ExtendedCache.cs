using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoSmart.Caching.Sqlite;
using Microsoft.Extensions.Caching.Distributed;

namespace CoreUtils.Classes
{

    public class ExtendedCache
    {
        // for large object storage to avoid overloading memory as it is slower
        private SqliteCache DiskCache { get; } = null;

        // for string storage as it is faster
        private MemoryCache MemoryCache { get; } = null;

        private SqliteCacheOptions Options { get; }

        public ILogger<SqliteCache> Logger { get; } = null;

        public TimeSpan SlidingExpiration { get; } = TimeSpan.FromHours(1);

        public DistributedCacheEntryOptions CacheEntryOptions { get; }

        private Int64 _countOfSetOperations = 0;

        public ExtendedCache(TimeSpan slidingExpiration, TimeSpan cleanupTimeSpan, ILogger<SqliteCache> logger = null)
        {
            //
            if (cleanupTimeSpan.TotalMinutes < 60)
            {
                cleanupTimeSpan = TimeSpan.FromHours(1);
            }
            Options = new SqliteCacheOptions
            {
                CachePath = $"{Path.GetTempFileName()}.sqlite",
                CleanupInterval = cleanupTimeSpan != null ? cleanupTimeSpan : TimeSpan.FromHours(1),
                MemoryOnly = true
            };

            //
            if (slidingExpiration.TotalMinutes < 60)
            {
                slidingExpiration = TimeSpan.FromHours(1);
            }

            CacheEntryOptions = new DistributedCacheEntryOptions
            {
                SlidingExpiration = slidingExpiration,
            };


            //
            if (logger == null)
            {
                this.Logger = null;
            }
            else
            {
                Logger = logger;
            }
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

        // stores string in DiskCache
        public Object Add(string key, Object item)
        {
            if (item == null)
                return null;

            //
            if (item is String || item is string || item is Int32 || item is Int64 || item is Boolean || DiskCache == null)
            {
                return this.AddToMemoryCache(key, item);
            }

            // remove expired every N sets
            _countOfSetOperations++;
            if (_countOfSetOperations >= 1000)
            {
                DiskCache.RemoveExpiredAsync();
            }

            BinaryFormatter bf = new BinaryFormatter();
            using MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, item);
            Byte[] byteArray = ms.ToArray();

            DiskCache.Set(key, byteArray, CacheEntryOptions);

            // remove from MemoryCache so we don't have a previous invalid value returned
            MemoryCache.Remove(key);

            return item;
        }
        // stores string in MemoryCache
        public Object Add(string key, string item)
        {
            if (item == null)
                return null;

            DateTimeOffset dateTimeOffset = new DateTimeOffset(DateTime.UtcNow, TimeSpan.Zero).AddSeconds(SlidingExpiration.TotalSeconds);
            MemoryCache.Set(key, item, dateTimeOffset);

            return item;
        }   
        public Object AddToMemoryCache(string key, object item)
        {
            if (item == null)
                return null;

            DateTimeOffset dateTimeOffset = new DateTimeOffset(DateTime.UtcNow, TimeSpan.Zero).AddSeconds(SlidingExpiration.TotalSeconds);
            MemoryCache.Set(key, item, dateTimeOffset);

            return item;
        }

        public Object Get(string key)
        {
            // try mem cache
            string value = (string)MemoryCache.Get(key);
            if (value != null)
            {
                return value;
            }

            if (DiskCache == null)
            {
                return null;
            }
            // try DiskCache
            Byte[] byteArray = DiskCache.Get(key);
            if (byteArray == null || byteArray.Length == 0)
            {
                return null;
            }
            //
            using MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(byteArray, 0, byteArray.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            //
            Object item = (Object)binForm.Deserialize(memStream);
            return item;
        }
        public Boolean ContainsKey(string key)
        {
            // try mem cache
            string item = (string)MemoryCache.Get(key);
            if (item != null)
            {
                return true;
            }

            if (DiskCache == null)
            {
                return false;
            }


            // try DiskCache
            Byte[] byteArray = DiskCache.Get(key);
            if (byteArray == null || byteArray.Length == 0)
            {
                return false;
            }

            return true;
        }

    }
}
