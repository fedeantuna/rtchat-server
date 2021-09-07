using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Moq;
using Moq.Protected;
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
        private const String Auth0ManagementApiClientSecret = "aa9bc74d670a-459093a211b4770d7e8f3ef4fda4365243e38412d85c35187ba4";
        private const String Auth0ManagementApiBaseAddress = "https://localhost";
        private const String Auth0ManagementApiTokenEndpoint = "/token";

        private const String SendAsync = nameof(SendAsync);
            
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;

        private readonly TokenService _sut;

        public TokenServiceTests()
        {
            this._httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            
            var inMemoryConfiguration = new Dictionary<String, String>
            {
                { ConfigurationKeys.Auth0ManagementApiAudience, Auth0ManagementApiAudience },
                { ConfigurationKeys.Auth0ManagementApiClientId, Auth0ManagementApiClientId },
                { ConfigurationKeys.Auth0ManagementApiClientSecret, Auth0ManagementApiClientSecret },
                { ConfigurationKeys.Auth0ManagementApiBaseAddress, Auth0ManagementApiBaseAddress },
                { ConfigurationKeys.Auth0ManagementApiTokenEndpoint, Auth0ManagementApiTokenEndpoint },
            };

            var httpClient = new HttpClient(this._httpMessageHandlerMock.Object);
            httpClient.BaseAddress = new Uri(Auth0ManagementApiBaseAddress);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemoryConfiguration)
                .Build();

            this._sut = new TokenService(httpClient, configuration);
        }

        [Fact]
        public async Task GetToken_ReturnsTokenResponse()
        {
            // Arrange
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
            
            this._httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(SendAsync, ItExpr.Is<HttpRequestMessage>(hrm =>
                    hrm.RequestUri.AbsoluteUri == endpoint
                    && hrm.Method == HttpMethod.Post
                    && hrm.Content.ReadAsStringAsync().GetAwaiter().GetResult() == contentAsString
                ), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(tokenResponse), Encoding.UTF8, MediaTypeNames.Application.Json)
                });

            // Act
            var result = await this._sut.GetToken();

            // Assert
            Assert.Equal(tokenResponse.Scope, result.Scope);
            Assert.Equal(tokenResponse.AccessToken, result.AccessToken);
            Assert.Equal(tokenResponse.ExpiresIn, result.ExpiresIn);
            Assert.Equal(tokenResponse.TokenType, result.TokenType);
        }
        
        [Fact]
        public async Task GetToken_ThrowsNullTokenException_WhenTokenResponseIsNull()
        {
            // Arrange
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

            this._httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(SendAsync, ItExpr.Is<HttpRequestMessage>(hrm =>
                    hrm.RequestUri.AbsoluteUri == endpoint
                    && hrm.Method == HttpMethod.Post
                    && hrm.Content.ReadAsStringAsync().GetAwaiter().GetResult() == contentAsString
                ), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize<TokenResponse>(null), Encoding.UTF8, MediaTypeNames.Application.Json)
                });

            // Act
            // Assert
            await Assert.ThrowsAsync<NullTokenException>(() => this._sut.GetToken());
        }
        
        [Fact]
        public async Task GetToken_ThrowsHttpRequestException_WhenResponseIsNotOk()
        {
            // Arrange
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

            this._httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(SendAsync, ItExpr.Is<HttpRequestMessage>(hrm =>
                    hrm.RequestUri.AbsoluteUri == endpoint
                    && hrm.Method == HttpMethod.Post
                    && hrm.Content.ReadAsStringAsync().GetAwaiter().GetResult() == contentAsString
                ), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent(JsonSerializer.Serialize<TokenResponse>(null), Encoding.UTF8, MediaTypeNames.Application.Json)
                });

            // Act
            // Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => this._sut.GetToken());
        }
    }
}