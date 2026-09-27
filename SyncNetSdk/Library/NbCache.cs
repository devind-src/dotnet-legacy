using Microsoft.Extensions.Caching.Memory;
using System;

namespace SyncNet.Library
{
    public interface INbCache
    {
        bool Add<T>(string key, T value);
        bool GetValue<T>(string key, out T value);
        void Remove(string key);
    }

    public class NbCache : INbCache
    {
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _defaultExpiresIn;

        public NbCache(IMemoryCache cache, int expiresInMinutes = 5)
        {
            //minimum 1 minute
            if (expiresInMinutes <= 0) expiresInMinutes = 1;

            _cache = cache;
            _defaultExpiresIn = TimeSpan.FromMinutes(expiresInMinutes);
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
