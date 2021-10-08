using System;

namespace RTChat.Server.Application.Common.Options
{
    public class Auth0ManagementApiOptions
    {
        public String BaseAddress { get; init; }
        public String TokenEndpoint { get; init; }
        public String Audience { get; init; }
        public String ClientId { get; init; }
        public String ClientSecret { get; init; }
        public String UsersByEmailEndpoint { get; init; }
        public String UsersByIdEndpoint { get; init; } 
    }
}