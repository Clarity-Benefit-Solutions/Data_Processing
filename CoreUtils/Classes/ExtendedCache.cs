using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private SqliteCache Cache { get; } = null;

        private SqliteCacheOptions Options { get; }

        public ILogger<SqliteCache> Logger { get; } = null;

        public TimeSpan SlidingExpiration { get; } = TimeSpan.FromHours(1);

        public DistributedCacheEntryOptions CacheEntryOptions { get; }

        private int _countOfSetOperations = 0;

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
                MemoryOnly = false
            };

            //
            if (slidingExpiration.TotalMinutes < 60)
            {
                slidingExpiration = TimeSpan.FromHours(1);
            }

            CacheEntryOptions = new DistributedCacheEntryOptions
            {
                SlidingExpiration = slidingExpiration
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

            Cache = new SqliteCache(Options, Logger);

        }

        public Object Add(string key, Object item)
        {
            if (item == null)
                return null;

            BinaryFormatter bf = new BinaryFormatter();
            using MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, item);
            Byte[] byteArray = ms.ToArray();

            Cache.Set(key, byteArray, CacheEntryOptions);

            return item;
        }
        public Object Get(string key)
        {
            // remove expired every N sets
            _countOfSetOperations++;
            if (_countOfSetOperations >= 100)
            {
                Cache.RemoveExpiredAsync();
            }

            //
            Byte[] byteArray = Cache.Get(key);
            if (byteArray == null || byteArray.Length == 0)
            {
                return null;
            }

            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(byteArray, 0, byteArray.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            //
            Object item = (Object)binForm.Deserialize(memStream);
            return item;
        }
        public Boolean ContainsKey(string key)
        {
            Byte[] byteArray = Cache.Get(key);

            if (byteArray == null || byteArray.Length == 0)
            {
                return false;
            }

            return true;
        }

    }
}
