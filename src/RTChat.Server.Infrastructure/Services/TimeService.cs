using System;
using RTChat.Server.Application.Common.Services;

namespace RTChat.Server.Infrastructure.Services
{
    public class TimeService : ITimeService
    {
        public DateTimeOffset UtcNow()
        {
            return DateTimeOffset.UtcNow;
        }
    }
}