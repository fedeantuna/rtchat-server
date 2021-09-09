using System;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using RTChat.Server.API.Constants;

namespace RTChat.Server.API.Models
{
    public class User
    {
        [JsonPropertyName(OpenIdConnectParameterNames.UserId)]
        public String Id { get; init; }

        [JsonPropertyName(OpenIdConnectScope.Email)]
        public String Email { get; init; }

        [JsonPropertyName((AuthParameterNames.Picture))]
        public String Picture { get; init; }
        
        [JsonPropertyName(AuthParameterNames.Status)]
        public String Status { get; set; }
    }
}