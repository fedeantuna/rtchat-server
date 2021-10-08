using System;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using RTChat.Server.Infrastructure.Constants;

namespace RTChat.Server.Infrastructure.Models
{
    public class TokenRequest
    {
        [JsonPropertyName(OpenIdConnectParameterNames.ClientId)]
        public String ClientId { get; init; }
        
        [JsonPropertyName(OpenIdConnectParameterNames.ClientSecret)]
        public String ClientSecret { get; init; }
        
        [JsonPropertyName(AuthParameterNames.Audience)]
        public String Audience { get; init; }
        
        [JsonPropertyName(OpenIdConnectParameterNames.GrantType)]
        public String GrantType { get; init; }
    }
}