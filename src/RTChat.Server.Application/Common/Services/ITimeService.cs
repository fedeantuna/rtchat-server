using System;

namespace RTChat.Server.Application.Common.Services
{
    public interface ITimeService
    {
        DateTimeOffset UtcNow();
    }
}