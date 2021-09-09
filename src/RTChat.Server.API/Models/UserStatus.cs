using System;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using RTChat.Server.API.Constants;

namespace RTChat.Server.API.Models
{
    public class UserStatus
    {
        [JsonPropertyName(OpenIdConnectParameterNames.UserId)]
        public String UserId { get; init; }
        
        [JsonPropertyName(AuthParameterNames.Status)]
        public String Status { get; init; }
    }
}