using Ccxc.Core.DbOrm;
using Ccxc.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.DataServices
{
    public class DataCache : IDataCache
    {
        private readonly string RedisConnStr;
        private readonly int RedisDatabase;

        private const string CACHE_HEADER = "ccxc-backend";

        public DataCache(string redisConnStr, int redisDatabase = 0)
        {
            RedisConnStr = redisConnStr;
            RedisDatabase = redisDatabase;
        }

        public Task Put(string key, object value, long timeoutMilliseconds = -1)
        {
            var redis = new RedisClient(RedisConnStr, RedisDatabase);
            return redis.PutObject(key, value, timeoutMilliseconds);
        }

        public Task PutString(string key, string value, long timeoutMilliseconds = -1)
        {
            var redis = new RedisClient(RedisConnStr, RedisDatabase);
            return redis.PutString(key, value, timeoutMilliseconds);
        }

        public Task<T> Get<T>(string key)
        {
            var redis = new RedisClient(RedisConnStr, RedisDatabase);
            return redis.GetObject<T>(key);
        }

        public Task<string> GetString(string key)
        {
            var redis = new RedisClient(RedisConnStr, RedisDatabase);
            return redis.GetString(key);
        }

        public Task PutAll(string key, IDictionary<string, object> values, long timeoutMilliseconds = -1)
        {
            var redis = new RedisClient(RedisConnStr, RedisDatabase);
            return redis.PutHash(key, values, timeoutMilliseconds);
        }

        public Task<T> GetFromPk<T>(string key, string pk)
        {
            var redis = new RedisClient(RedisConnStr, RedisDatabase);
            return redis.GetHash<T>(key, pk);
        }

        public Task<List<T>> GetAll<T>(string key)
        {
            var redis = new RedisClient(RedisConnStr, RedisDatabase);
            return redis.GetHashAll<T>(key);
        }

        public Task<List<(string key, T value)>> GetHashKeys<T>(string key)
        {
            var redis = new RedisClient(RedisConnStr, RedisDatabase);
            return redis.GetHashKeys<T>(key);
        }

        public Task PutList(string key, IList<object> list, long timeout)
        {
            var redis = new RedisClient(RedisConnStr, RedisDatabase);
            return redis.PutList(key, list, timeout);
        }

        public Task<List<T>> GetList<T>(string key, int start, int end, long updateTimeout)
        {
            var redis = new RedisClient(RedisConnStr, RedisDatabase);
            return redis.GetList<T>(key, start, end, updateTimeout);
        }

        public Task<long> GetListLength(string key)
        {
            var redis = new RedisClient(RedisConnStr, RedisDatabase);
            return redis.GetListLength(key);
        }

        public Task<long> GetHashLength(string key)
        {
            var redis = new RedisClient(RedisConnStr, RedisDatabase);
            return redis.GetHashLength(key);
        }

        public Task Delete(string key)
        {
            var redis = new RedisClient(RedisConnStr, RedisDatabase);
            return redis.Delete(key);
        }

        public Task Delete(string key, string hkey)
        {
            var redis = new RedisClient(RedisConnStr, RedisDatabase);
            return redis.Delete(key, hkey);
        }

        public List<string> FindKeys(string keyPattern)
        {
            var redis = new RedisClient(RedisConnStr, RedisDatabase);
            return redis.FindKeys(keyPattern);
        }

        public Task<List<(string key, T value)>> SearchHashKey<T>(string key, string pattern)
        {
            var redis = new RedisClient(RedisConnStr, RedisDatabase);
            return redis.SearchHashKey<T>(key, pattern);
        }

        public Task<List<(string key, T value)>> SearchHashValue<T>(string key, string pattern)
        {
            var redis = new RedisClient(RedisConnStr, RedisDatabase);
            return redis.SearchHashValue<T>(key, pattern);
        }

        public string GetCacheKey(string cacheTag)
        {
            return $"/{CACHE_HEADER}/recordcache/{cacheTag}";
        }

        public string GetDataKey(string cacheKey)
        {
            return $"/{CACHE_HEADER}/datacache/{cacheKey}";
        }

        public string GetUserSessionKey(string uuid)
        {
            return $"/{CACHE_HEADER}/usersession/{uuid}";
        }

        public string GetTempTicketKey(string uuid)
        {
            return $"/{CACHE_HEADER}/tempticket/{uuid}";
        }
    }
}
