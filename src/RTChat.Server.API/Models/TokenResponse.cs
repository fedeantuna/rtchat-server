using System;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace RTChat.Server.API.Models
{
    public class TokenResponse
    {
        [JsonPropertyName(OpenIdConnectParameterNames.AccessToken)]
        public String AccessToken { get; init; }
        
        [JsonPropertyName(OpenIdConnectParameterNames.Scope)]
        public String Scope { get; init; }
        
        [JsonPropertyName(OpenIdConnectParameterNames.ExpiresIn)]
        public Int32 ExpiresIn { get; init; }
        
        [JsonPropertyName(OpenIdConnectParameterNames.TokenType)]
        public String TokenType { get; init; }
    }
}