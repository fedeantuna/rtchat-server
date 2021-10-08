using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using RTChat.Server.Application.Common.Options;
using RTChat.Server.Application.Common.Services;
using RTChat.Server.Infrastructure.Constants;
using RTChat.Server.Infrastructure.Models;

namespace RTChat.Server.Infrastructure.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly HttpClient _httpClient;
        private readonly Auth0ManagementApiOptions _auth0ManagementApiOptions;

        public IdentityService(IAuthorizationService authorizationService, IHttpClientFactory httpClientFactory, IOptions<Auth0ManagementApiOptions> auth0ManagementApiOptions)
        {
            this._authorizationService = authorizationService;
            this._httpClient = httpClientFactory.CreateClient(nameof(IdentityService));
            this._auth0ManagementApiOptions = auth0ManagementApiOptions.Value;
        }
        
        public async Task<String> GetUsername(String userId)
        {
            var baseEndpoint = this._auth0ManagementApiOptions.UsersByIdEndpoint;
            var fields = new[]
            {
                OpenIdConnectParameterNames.UserId,
                OpenIdConnectScope.Email,
                AuthParameterNames.Picture
            };

            var parameters = new Dictionary<String, String>
            {
                { AuthParameterNames.Fields, String.Join(',', fields) },
                { AuthParameterNames.IncludeFields, true.ToString().ToLowerInvariant() }
            };
            
            var endpoint = QueryHelpers.AddQueryString($"{baseEndpoint}/{userId}", parameters);
            
            // token
            var audience = this._auth0ManagementApiOptions.Audience;
            var clientId = this._auth0ManagementApiOptions.ClientId;
            var clientSecret = this._auth0ManagementApiOptions.ClientSecret;
            
            var tokenRequest = new TokenRequest
            {
                Audience = audience,
                ClientId = clientId,
                ClientSecret = clientSecret,
                GrantType = OpenIdConnectGrantTypes.ClientCredentials
            };

            var serializedTokenRequest = JsonSerializer.Serialize(tokenRequest);
            
            var body = new StringContent(serializedTokenRequest, Encoding.UTF8, MediaTypeNames.Application.Json);
            
            var tokenEndpoint = this._auth0ManagementApiOptions.TokenEndpoint;
            
            var resp = await this._httpClient.PostAsync(tokenEndpoint, body);
            resp.EnsureSuccessStatusCode();
            var tStringResponse = await resp.Content.ReadAsStringAsync();
            
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(tStringResponse);
            // -token
            
            this._httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(tokenResponse.TokenType, tokenResponse.AccessToken);
            
            var response = await this._httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            var stringResponse = await response.Content.ReadAsStringAsync();
            
            var user = JsonSerializer.Deserialize<User>(stringResponse);

            return user?.Username;
        }

        public Task<Boolean> IsInRole(String userId, String role)
        {
            throw new NotImplementedException();
        }

        public Task<Boolean> Authorize(String userId, String policy)
        {
            throw new NotImplementedException();
        }
    }
}