using System;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using RTChat.Server.API.Cache;
using RTChat.Server.API.Constants;
using RTChat.Server.API.Exceptions;
using RTChat.Server.API.Models;

namespace RTChat.Server.API.Services
{
    public class TokenService : ITokenService
    {
        private readonly IApplicationCache _applicationCache;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public TokenService(IApplicationCache applicationCache, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            this._applicationCache = applicationCache;
            this._httpClient = httpClientFactory.CreateClient(HttpClientNames.Auth0ManagementApi);
            this._configuration = configuration;
        }

        public async Task<TokenResponse> GetToken()
        {
            var tokenResponse = await this.GetTokenResponse();

            return tokenResponse;
        }
        
        private async Task<TokenResponse> GetTokenResponse()
        {
            if (this._applicationCache.MemoryCache.TryGetValue(ApplicationCacheKeys.TokenResponse, out TokenResponse tokenResponse))
            {
                return tokenResponse;
            }
            
            var tokenRequestBody = this.GetTokenRequestBody();

            var tokenEndpoint = this._configuration[ConfigurationKeys.Auth0ManagementApiTokenEndpoint];

            var response = await this._httpClient.PostAsync(tokenEndpoint, tokenRequestBody);
            response.EnsureSuccessStatusCode();
            var stringResponse = await response.Content.ReadAsStringAsync();
            
            tokenResponse = JsonSerializer.Deserialize<TokenResponse>(stringResponse);

            if (tokenResponse == null)
            {
                throw new NullTokenException();
            }
            
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSize(ApplicationCacheEntrySizes.TokenResponse)
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(tokenResponse.ExpiresIn * 0.9));

            this._applicationCache.MemoryCache.Set(ApplicationCacheKeys.TokenResponse, tokenResponse, cacheEntryOptions);

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
    }
}