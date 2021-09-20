using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Moq;
using Moq.Protected;
using RTChat.Server.API.Cache;
using RTChat.Server.API.Constants;
using RTChat.Server.API.Exceptions;
using RTChat.Server.API.Models;
using RTChat.Server.API.Services;
using Xunit;

namespace RTChat.Server.API.Tests.Services
{
    [ExcludeFromCodeCoverage]
    public class TokenServiceTests
    {
        private const String Auth0ManagementApiAudience = "https://localhost/";
        private const String Auth0ManagementApiClientId = "94ca20a4b6564f12887f1b8775a55990";
        private const String Auth0ManagementApiClientSecret =
            "aa9bc74d670a-459093a211b4770d7e8f3ef4fda4365243e38412d85c35187ba4";
        private const String Auth0ManagementApiBaseAddress = "https://localhost";
        private const String Auth0ManagementApiTokenEndpoint = "/token";

        private const String SendAsync = nameof(SendAsync);

        private readonly Mock<IApplicationCache> _applicationCacheMock;
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;

        private readonly TokenService _sut;

        public TokenServiceTests()
        {
            this._httpMessageHandlerMock = new Mock<HttpMessageHandler>();

            this._applicationCacheMock = new Mock<IApplicationCache>();
            var httpClientFactoryMock = this.SetUpHttpClientFactory();
            var configuration = SetUpInMemoryConfiguration();

            this._sut = new TokenService(this._applicationCacheMock.Object, httpClientFactoryMock.Object, configuration);
        }

        [Fact]
        public async Task GetToken_ReturnsTokenResponseFromCache_WhenTokenIsInMemoryCache()
        {
            // Arrange
            var memoryCache = this.SetUpInMemoryCache();
            var tokenResponse = new TokenResponse
            {
                Scope = "test-scope",
                AccessToken = "test-access-token",
                ExpiresIn = 42,
                TokenType = "test-token-type"
            };

            memoryCache.Set(ApplicationCacheKeys.TokenResponse, tokenResponse);

            // Act
            var result = await this._sut.GetToken();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(tokenResponse, result);
        }
        
        [Fact]
        public async Task GetToken_SavesTokenInMemoryCache()
        {
            // Arrange
            var memoryCache = this.SetUpInMemoryCache();
            var tokenRequest = new TokenRequest
            {
                Audience = Auth0ManagementApiAudience,
                ClientId = Auth0ManagementApiClientId,
                ClientSecret = Auth0ManagementApiClientSecret,
                GrantType = OpenIdConnectGrantTypes.ClientCredentials
            };
            var serializedTokenRequest = JsonSerializer.Serialize(tokenRequest);
            var content = new StringContent(serializedTokenRequest, Encoding.UTF8, MediaTypeNames.Application.Json);
            var endpoint = $"{Auth0ManagementApiBaseAddress}{Auth0ManagementApiTokenEndpoint}";

            var contentAsString = await content.ReadAsStringAsync();

            var tokenResponse = new TokenResponse
            {
                Scope = "test-scope",
                AccessToken = "test-access-token",
                ExpiresIn = 42,
                TokenType = "test-token-type"
            };

            this.SetUpHttpMessageHandler(endpoint, contentAsString, tokenResponse);

            await this._sut.GetToken();

            // Act
            var result = memoryCache.Get<TokenResponse>(ApplicationCacheKeys.TokenResponse);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(tokenResponse.Scope, result.Scope);
            Assert.Equal(tokenResponse.AccessToken, result.AccessToken);
            Assert.Equal(tokenResponse.ExpiresIn, result.ExpiresIn);
            Assert.Equal(tokenResponse.TokenType, result.TokenType);
        }
        
        [Fact]
        public async Task GetToken_ReturnsTokenResponseFromHttp_WhenTokenIsNotInMemory()
        {
            // Arrange
            this.SetUpInMemoryCache();
            var tokenRequest = new TokenRequest
            {
                Audience = Auth0ManagementApiAudience,
                ClientId = Auth0ManagementApiClientId,
                ClientSecret = Auth0ManagementApiClientSecret,
                GrantType = OpenIdConnectGrantTypes.ClientCredentials
            };
            var serializedTokenRequest = JsonSerializer.Serialize(tokenRequest);
            var content = new StringContent(serializedTokenRequest, Encoding.UTF8, MediaTypeNames.Application.Json);
            var endpoint = $"{Auth0ManagementApiBaseAddress}{Auth0ManagementApiTokenEndpoint}";

            var contentAsString = await content.ReadAsStringAsync();

            var tokenResponse = new TokenResponse
            {
                Scope = "test-scope",
                AccessToken = "test-access-token",
                ExpiresIn = 42,
                TokenType = "test-token-type"
            };

            this.SetUpHttpMessageHandler(endpoint, contentAsString, tokenResponse);

            // Act
            var result = await this._sut.GetToken();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(tokenResponse.Scope, result.Scope);
            Assert.Equal(tokenResponse.AccessToken, result.AccessToken);
            Assert.Equal(tokenResponse.ExpiresIn, result.ExpiresIn);
            Assert.Equal(tokenResponse.TokenType, result.TokenType);
        }

        [Fact]
        public async Task GetToken_ThrowsNullTokenException_WhenTokenResponseIsNull()
        {
            // Arrange
            this.SetUpInMemoryCache();
            var tokenRequest = new TokenRequest
            {
                Audience = Auth0ManagementApiAudience,
                ClientId = Auth0ManagementApiClientId,
                ClientSecret = Auth0ManagementApiClientSecret,
                GrantType = OpenIdConnectGrantTypes.ClientCredentials
            };
            var serializedTokenRequest = JsonSerializer.Serialize(tokenRequest);
            var content = new StringContent(serializedTokenRequest, Encoding.UTF8, MediaTypeNames.Application.Json);
            var endpoint = $"{Auth0ManagementApiBaseAddress}{Auth0ManagementApiTokenEndpoint}";

            var contentAsString = await content.ReadAsStringAsync();

            this.SetUpHttpMessageHandlerWithNullResponse(endpoint, contentAsString);

            // Act
            Task Token() => this._sut.GetToken();
            
            // Assert
            await Assert.ThrowsAsync<NullTokenException>(Token);
        }

        [Fact]
        public async Task GetToken_ThrowsHttpRequestException_WhenResponseIsNotOk()
        {
            // Arrange
            this.SetUpInMemoryCache();
            var tokenRequest = new TokenRequest
            {
                Audience = Auth0ManagementApiAudience,
                ClientId = Auth0ManagementApiClientId,
                ClientSecret = Auth0ManagementApiClientSecret,
                GrantType = OpenIdConnectGrantTypes.ClientCredentials
            };
            var serializedTokenRequest = JsonSerializer.Serialize(tokenRequest);
            var content = new StringContent(serializedTokenRequest, Encoding.UTF8, MediaTypeNames.Application.Json);
            var endpoint = $"{Auth0ManagementApiBaseAddress}{Auth0ManagementApiTokenEndpoint}";

            var contentAsString = await content.ReadAsStringAsync();

            this.SetUpHttpMessageHandlerWithBadRequest(endpoint, contentAsString);

            // Act
            Task Token() => this._sut.GetToken();
            
            // Assert
            await Assert.ThrowsAsync<HttpRequestException>(Token);
        }

        private void SetUpHttpMessageHandler(String endpoint, String contentAsString, TokenResponse tokenResponse)
        {
            var httpRequestMessageMatch = SetUpHttpRequestMessageMatch(endpoint, contentAsString);
            this._httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(SendAsync, ItExpr.Is(httpRequestMessageMatch), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(tokenResponse), Encoding.UTF8,
                        MediaTypeNames.Application.Json)
                });
        }

        private void SetUpHttpMessageHandlerWithNullResponse(String endpoint, String contentAsString)
        {
            var httpRequestMessageMatch = SetUpHttpRequestMessageMatch(endpoint, contentAsString);
            this._httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(SendAsync, ItExpr.Is(httpRequestMessageMatch), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize<TokenResponse>(null), Encoding.UTF8,
                        MediaTypeNames.Application.Json)
                });
        }
        
        private void SetUpHttpMessageHandlerWithBadRequest(String endpoint, String contentAsString)
        {
            var httpRequestMessageMatch = SetUpHttpRequestMessageMatch(endpoint, contentAsString);
            this._httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(SendAsync, ItExpr.Is(httpRequestMessageMatch), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest
                });
        }
        
        private Mock<IHttpClientFactory> SetUpHttpClientFactory()
        {
            var httpClient = new HttpClient(this._httpMessageHandlerMock.Object);
            httpClient.BaseAddress = new Uri(Auth0ManagementApiBaseAddress);

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            
            httpClientFactoryMock.Setup(hcf => hcf.CreateClient(HttpClientNames.Auth0ManagementApi))
                .Returns(httpClient);

            return httpClientFactoryMock;
        }
        
        private IMemoryCache SetUpInMemoryCache()
        {
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            this._applicationCacheMock.SetupGet(ac => ac.MemoryCache).Returns(memoryCache);

            return memoryCache;
        }

        private static IConfiguration SetUpInMemoryConfiguration()
        {
            var inMemoryConfiguration = new Dictionary<String, String>
            {
                { ConfigurationKeys.Auth0ManagementApiAudience, Auth0ManagementApiAudience },
                { ConfigurationKeys.Auth0ManagementApiClientId, Auth0ManagementApiClientId },
                { ConfigurationKeys.Auth0ManagementApiClientSecret, Auth0ManagementApiClientSecret },
                { ConfigurationKeys.Auth0ManagementApiBaseAddress, Auth0ManagementApiBaseAddress },
                { ConfigurationKeys.Auth0ManagementApiTokenEndpoint, Auth0ManagementApiTokenEndpoint },
            };
            
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemoryConfiguration)
                .Build();

            return configuration;
        }

        private static Expression<Func<HttpRequestMessage, Boolean>> SetUpHttpRequestMessageMatch(String endpoint, String contentAsString)
        {
            Expression<Func<HttpRequestMessage, Boolean>> match = hrm =>
                hrm.RequestUri.AbsoluteUri == endpoint
                && hrm.Method == HttpMethod.Post
                && hrm.Content.ReadAsStringAsync().GetAwaiter().GetResult() == contentAsString;

            return match;
        }
    }
}