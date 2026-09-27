using Microsoft.Extensions.Caching.Memory;
using System;

namespace SyncNet.Common
{
    public class CacheData
    {
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _defaultExpiresIn;

        public CacheData(int expiresInSeconds = 60)
        {
            //minimum expires 60 seconds
            if (expiresInSeconds <= 0) expiresInSeconds = 60;

            _cache = new MemoryCache(new MemoryCacheOptions());

            _defaultExpiresIn = TimeSpan.FromSeconds(expiresInSeconds);
        }

        public bool Add<T>(string key, T value)
        {
            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(_defaultExpiresIn);

            _cache.Set(key, value, options);

            // cek apakah tersimpan
            return _cache.TryGetValue(key, out _);
        }

        public bool GetValue<T>(string key, out T value)
        {
            return _cache.TryGetValue(key, out value);
        }

        public bool GetAndRemove<T>(string key, out T value)
        {
            bool bval = _cache.TryGetValue(key, out value);

            _cache.Remove(key);

            return bval;
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
        }
    }
}
