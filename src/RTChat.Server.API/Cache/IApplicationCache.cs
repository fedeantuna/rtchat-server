using Microsoft.Extensions.Caching.Memory;

namespace RTChat.Server.API.Cache
{
    public interface IApplicationCache
    {
        IMemoryCache MemoryCache { get; }
    }
}