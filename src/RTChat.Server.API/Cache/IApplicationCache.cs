using Microsoft.Extensions.Caching.Memory;

namespace RTChat.Server.API.Cache
{
    public interface IApplicationCache
    {
        MemoryCache MemoryCache { get; }
    }
}