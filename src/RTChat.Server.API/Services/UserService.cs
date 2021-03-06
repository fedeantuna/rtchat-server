using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using RTChat.Server.API.Cache;
using RTChat.Server.API.Constants;
using RTChat.Server.API.Models;

namespace RTChat.Server.API.Services
{
    public class UserService : IUserService
    {
        private readonly IApplicationCache _applicationCache;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public UserService(IApplicationCache applicationCache, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            this._applicationCache = applicationCache;
            this._httpClient = httpClientFactory.CreateClient(HttpClientNames.Auth0ManagementApi);
            this._configuration = configuration;
        }
        
        public async Task<User> GetUser(String id, TokenResponse tokenResponse)
        {
            var cacheEntry = $"{ApplicationCacheKeys.UserPrefix}{id}";
            var user = await this._applicationCache.MemoryCache.GetOrCreateAsync(cacheEntry, entry =>
            {
                entry.SetSize(ApplicationCacheEntrySizes.User);

                return this.GetUserById(id, tokenResponse);
            });
            
            return user;
        }

        public async Task<User> GetUser(MailAddress mailAddress, TokenResponse tokenResponse)
        {
            var cacheEntry = $"{ApplicationCacheKeys.UserPrefix}{mailAddress.Address}";
            var user = await this._applicationCache.MemoryCache.GetOrCreateAsync(cacheEntry, entry =>
            {
                entry.SetSize(ApplicationCacheEntrySizes.User);

                return this.GetUserByEmail(mailAddress.Address, tokenResponse);
            });

            return user;
        }

        private async Task<User> GetUserByEmail(String mailAddress, TokenResponse tokenResponse)
        {
            var serializedUser = await this.GetSerializedUser(
                () => this.GetUserByEmailEndpoint(mailAddress),
                tokenResponse.TokenType,
                tokenResponse.AccessToken);
            
            var user = JsonSerializer.Deserialize<IEnumerable<User>>(serializedUser);

            return user?.FirstOrDefault();
        }

        private async Task<User> GetUserById(String id, TokenResponse tokenResponse)
        {
            var serializedUser = await this.GetSerializedUser(
                () => this.GetUserByIdEndpoint(id),
                tokenResponse.TokenType,
                tokenResponse.AccessToken);

            var user = JsonSerializer.Deserialize<User>(serializedUser);

            return user;
        }

        private String GetUserByIdEndpoint(String id)
        {
            var baseEndpoint = this._configuration[ConfigurationKeys.Auth0ManagementApiUsersByIdEndpoint];

            var parameters = GetUserParameters();

            var endpoint = QueryHelpers.AddQueryString($"{baseEndpoint}/{id}", parameters);
            
            return endpoint;
        }
        
        private String GetUserByEmailEndpoint(String address)
        {
            var baseEndpoint = this._configuration[ConfigurationKeys.Auth0ManagementApiUsersByEmailEndpoint];

            var parameters = GetUserParameters();
            parameters.Add(OpenIdConnectScope.Email, address);
            
            var endpoint = QueryHelpers.AddQueryString(baseEndpoint, parameters);
            
            return endpoint;
        }

        private async Task<String> GetSerializedUser(Func<String> getEndpoint, String tokenType, String accessToken)
        {
            var endpoint = getEndpoint();

            this._httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(tokenType, accessToken);

            var response = await this._httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            var stringResponse = await response.Content.ReadAsStringAsync();

            return stringResponse;
        }

        private static String[] GetUserFields()
        {
            var fields = new[]
            {
                OpenIdConnectParameterNames.UserId,
                OpenIdConnectScope.Email,
                AuthParameterNames.Picture
            };

            return fields;
        }

        private static Dictionary<String, String> GetUserParameters()
        {
            var fields = GetUserFields();

            var parameters = new Dictionary<String, String>
            {
                { AuthParameterNames.Fields, String.Join(',', fields) },
                { AuthParameterNames.IncludeFields, true.ToString().ToLowerInvariant() }
            };

            return parameters;
        }
    }
}