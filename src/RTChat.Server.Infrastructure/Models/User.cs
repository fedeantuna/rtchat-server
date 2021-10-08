using System;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using RTChat.Server.Infrastructure.Constants;

namespace RTChat.Server.Infrastructure.Models
{
    public class User
    {
        [JsonPropertyName(OpenIdConnectParameterNames.UserId)]
        public String Id { get; init; }

        [JsonPropertyName(OpenIdConnectParameterNames.Username)]
        public String Username { get; set; }

        [JsonPropertyName(OpenIdConnectScope.Email)]
        public String Email { get; init; }

        [JsonPropertyName(AuthParameterNames.Picture)]
        public String Picture { get; init; }
    }
}