using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using RTChat.Server.API.Constants;
using RTChat.Server.API.Exceptions;
using RTChat.Server.API.Models;

namespace RTChat.Server.API.Services
{
    public class TokenService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public TokenService(HttpClient httpClient, IConfiguration configuration)
        {
            this._httpClient = httpClient;
            this._configuration = configuration;
        }

        public async Task<TokenResponse> GetToken()
        {
            var tokenRequestBody = this.GetTokenRequestBody();

            var tokenResponse = await this.GetResponseFromTokenEndpoint(tokenRequestBody);

            return tokenResponse;
        }
        
        private StringContent GetTokenRequestBody()
        {
            var audience = this._configuration[ConfigurationKeys.Auth0ManagementApiAudience];
            var clientId = this._configuration[ConfigurationKeys.Auth0ManagementApiClientId];
            var clientSecret = this._configuration[ConfigurationKeys.Auth0ManagementApiClientSecret];
            
            var tokenRequest = new TokenRequest
            {
                Audience = audience,
                ClientId = clientId,
                ClientSecret = clientSecret,
                GrantType = OpenIdConnectGrantTypes.ClientCredentials
            };

            var serializedTokenRequest = JsonSerializer.Serialize(tokenRequest);
            
            var body = new StringContent(serializedTokenRequest, Encoding.UTF8, MediaTypeNames.Application.Json);

            return body;
        }
        
        private async Task<TokenResponse> GetResponseFromTokenEndpoint(HttpContent tokenRequestBody)
        {
            var tokenEndpoint = this._configuration[ConfigurationKeys.Auth0ManagementApiTokenEndpoint];

            var response = await this._httpClient.PostAsync(tokenEndpoint, tokenRequestBody);
            response.EnsureSuccessStatusCode();
            var stringResponse = await response.Content.ReadAsStringAsync();
            
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(stringResponse);

            if (tokenResponse == null)
            {
                throw new NullTokenException();
            }

            return tokenResponse;
        }
    }
}