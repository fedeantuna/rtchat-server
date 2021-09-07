using System;

namespace RTChat.Server.API.Constants
{
    public static class ConfigurationKeys
    {
        public const String CorsAllowedOrigins = "Cors:AllowedOrigins";
        public const String Auth0Domain = "Auth0:Domain";
        public const String Auth0Audience = "Auth0:Audience";
        public const String Auth0ManagementApiBaseAddress = "Auth0ManagementAPI:BaseAddress";
        public const String Auth0ManagementApiTokenEndpoint = "Auth0ManagementAPI:TokenEndpoint";
        public const String Auth0ManagementApiAudience = "Auth0ManagementAPI:Audience";
        public const String Auth0ManagementApiClientId = "Auth0ManagementAPI:ClientId";
        public const String Auth0ManagementApiClientSecret = "Auth0ManagementAPI:ClientSecret";
        public const String Auth0ManagementApiUsersByIdEndpoint = "Auth0ManagementAPI:UsersByIdEndpoint";
        public const String Auth0ManagementApiUsersByEmailEndpoint = "Auth0ManagementAPI:UsersByEmailEndpoint";
    }
}