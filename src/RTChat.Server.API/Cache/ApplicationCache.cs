using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using RTChat.Server.API.Constants;

namespace RTChat.Server.API.Cache
{
    public class ApplicationCache : IApplicationCache
    {
        public ApplicationCache(IConfiguration configuration)
        {
            this.MemoryCache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = configuration.GetValue<Int32>(ConfigurationKeys.InMemoryCacheSizeLimit)
            });
        }
        
        public IMemoryCache MemoryCache { get; }
    }
}