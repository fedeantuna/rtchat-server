using System;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace RTChat.Server.API.Providers
{
    public class UserIdProvider : IUserIdProvider
    {
        public String? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}