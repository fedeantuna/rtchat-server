using System;

namespace RTChat.Server.Application.Common.Services
{
    public interface ICurrentUserService
    {
        String GetUserId();
    }
}