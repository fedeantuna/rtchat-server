using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using RTChat.Server.API.Constants;
using RTChat.Server.API.Models;
using RTChat.Server.API.Services;
using Xunit;

namespace RTChat.Server.API.Tests.Services
{
    [ExcludeFromCodeCoverage]
    public class UserServiceTests
    {
        private const String Auth0ManagementApiBaseAddress = "https://localhost";
        private const String Auth0ManagementApiUsersByEmailEndpoint = "/users-by-email";
        private const String Auth0ManagementApiUsersByIdEndpoint = "/users-by-id";

        private const String SendAsync = nameof(SendAsync);

        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;

        private readonly UserService _sut;

        public UserServiceTests()
        {
            this._httpMessageHandlerMock = new Mock<HttpMessageHandler>();

            var httpClientFactoryMock = this.SetUpHttpClientFactory();
            var configuration = SetUpInMemoryConfiguration();

            this._sut = new UserService(httpClientFactoryMock.Object, configuration);
        }

        [Fact]
        public async Task GetUser_ReturnsUserById()
        {
            // Arrange
            const String fields = "user_id,email,picture";

            var id = Guid.NewGuid().ToString();
            var tokenResponse = new TokenResponse
            {
                Scope = "test-scope",
                AccessToken = "test-access-token",
                ExpiresIn = 42,
                TokenType = "test-token-type"
            };
            var userByIdEndpoint =
                $"{Auth0ManagementApiBaseAddress}{Auth0ManagementApiUsersByIdEndpoint}/{id}?fields={fields}&include_fields=true";

            var user = new User
            {
                Id = id,
                Email = "obiwankenobi@jediorder.rep",
                Picture = "some-picture"
            };

            this.SetUpHttpMessageHandler(userByIdEndpoint, tokenResponse, user);

            // Act
            var result = await this._sut.GetUser(id, tokenResponse);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
            Assert.Equal(user.Email, result.Email);
            Assert.Equal(user.Picture, result.Picture);
        }

        [Fact]
        public async Task GetUser_ThrowsHttpRequestException_WhenResponseByIdIsNotOk()
        {
            // Arrange
            const String fields = "user_id,email,picture";

            var id = Guid.NewGuid().ToString();
            var tokenResponse = new TokenResponse
            {
                Scope = "test-scope",
                AccessToken = "test-access-token",
                ExpiresIn = 42,
                TokenType = "test-token-type"
            };
            var userByIdEndpoint =
                $"{Auth0ManagementApiBaseAddress}{Auth0ManagementApiUsersByIdEndpoint}/{id}?fields={fields}&include_fields=true";

            this.SetUpHttpMessageHandlerWithNullUserResponse(userByIdEndpoint, tokenResponse);

            // Act
            Task User() => this._sut.GetUser(id, tokenResponse);
            
            // Assert
            await Assert.ThrowsAsync<HttpRequestException>(User);
        }

        [Fact]
        public async Task GetUser_ReturnsUserByEmail()
        {
            // Arrange
            const String fields = "user_id,email,picture";

            const String email = "obiwankenobi@jediorder.rep";
            var mailAddress = new MailAddress(email);
            var id = Guid.NewGuid().ToString();
            var tokenResponse = new TokenResponse
            {
                Scope = "test-scope",
                AccessToken = "test-access-token",
                ExpiresIn = 42,
                TokenType = "test-token-type"
            };
            var userByEmailEndpoint =
                $"{Auth0ManagementApiBaseAddress}{Auth0ManagementApiUsersByEmailEndpoint}?fields={fields}&include_fields=true&email={email}";

            var user = new User
            {
                Id = id,
                Email = email,
                Picture = "some-picture"
            };

            this.SetUpHttpMessageHandlerWithListOfUserResponse(userByEmailEndpoint, tokenResponse, new[] { user });

            // Act
            var result = await this._sut.GetUser(mailAddress, tokenResponse);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
            Assert.Equal(user.Email, result.Email);
            Assert.Equal(user.Picture, result.Picture);
        }
        
        [Fact]
        public async Task GetUser_ReturnsNull_WhenUsersByEmailIsEmpty()
        {
            // Arrange
            const String fields = "user_id,email,picture";

            const String email = "obiwankenobi@jediorder.rep";
            var mailAddress = new MailAddress(email);
            var tokenResponse = new TokenResponse
            {
                Scope = "test-scope",
                AccessToken = "test-access-token",
                ExpiresIn = 42,
                TokenType = "test-token-type"
            };
            var userByEmailEndpoint =
                $"{Auth0ManagementApiBaseAddress}{Auth0ManagementApiUsersByEmailEndpoint}?fields={fields}&include_fields=true&email={email}";

            this.SetUpHttpMessageHandlerWithListOfUserResponse(userByEmailEndpoint, tokenResponse, Array.Empty<User>());

            // Act
            var result = await this._sut.GetUser(mailAddress, tokenResponse);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUser_ThrowsHttpRequestException_WhenResponseByEmailIsNotOk()
        {
            // Arrange
            const String fields = "user_id,email,picture";

            const String email = "obiwankenobi@jediorder.rep";
            var mailAddress = new MailAddress(email);
            var tokenResponse = new TokenResponse
            {
                Scope = "test-scope",
                AccessToken = "test-access-token",
                ExpiresIn = 42,
                TokenType = "test-token-type"
            };
            var userByEmailEndpoint =
                $"{Auth0ManagementApiBaseAddress}{Auth0ManagementApiUsersByEmailEndpoint}?fields={fields}&include_fields=true&email={email}";

            this.SetUpHttpMessageHandlerWithBadRequest(userByEmailEndpoint, tokenResponse);

            // Act
            Task User() => this._sut.GetUser(mailAddress, tokenResponse);
            
            // Assert
            await Assert.ThrowsAsync<HttpRequestException>(User);
        }

        private void SetUpHttpMessageHandler(String absoluteUri, TokenResponse tokenResponse, User user)
        {
            var httpRequestMessageMatch = SetUpHttpRequestMessageMatch(absoluteUri, tokenResponse);
            this._httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(SendAsync, ItExpr.Is(httpRequestMessageMatch),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(user), Encoding.UTF8,
                        MediaTypeNames.Application.Json)
                });
        }

        private void SetUpHttpMessageHandlerWithNullUserResponse(String absoluteUri, TokenResponse tokenResponse)
        {
            var httpRequestMessageMatch = SetUpHttpRequestMessageMatch(absoluteUri, tokenResponse);
            this._httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(SendAsync, ItExpr.Is(httpRequestMessageMatch),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent(JsonSerializer.Serialize<User>(null), Encoding.UTF8,
                        MediaTypeNames.Application.Json)
                });
        }

        private void SetUpHttpMessageHandlerWithListOfUserResponse(String absoluteUri, TokenResponse tokenResponse,
            IEnumerable<User> users)
        {
            var httpRequestMessageMatch = SetUpHttpRequestMessageMatch(absoluteUri, tokenResponse);
            this._httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(SendAsync, ItExpr.Is(httpRequestMessageMatch),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(users), Encoding.UTF8,
                        MediaTypeNames.Application.Json)
                });
        }

        private void SetUpHttpMessageHandlerWithBadRequest(String absoluteUri, TokenResponse tokenResponse)
        {
            var httpRequestMessageMatch = SetUpHttpRequestMessageMatch(absoluteUri, tokenResponse);
            this._httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(SendAsync,
                    ItExpr.Is(httpRequestMessageMatch),
                    ItExpr.IsAny<CancellationToken>())
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

        private static IConfiguration SetUpInMemoryConfiguration()
        {
            var inMemoryConfiguration = new Dictionary<String, String>
            {
                { ConfigurationKeys.Auth0ManagementApiUsersByEmailEndpoint, Auth0ManagementApiUsersByEmailEndpoint },
                { ConfigurationKeys.Auth0ManagementApiUsersByIdEndpoint, Auth0ManagementApiUsersByIdEndpoint }
            };
            
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemoryConfiguration)
                .Build();

            return configuration;
        }
        
        private static Expression<Func<HttpRequestMessage, Boolean>> SetUpHttpRequestMessageMatch(String absoluteUri,
            TokenResponse tokenResponse)
        {
            Expression<Func<HttpRequestMessage, Boolean>> match = hrm =>
                hrm.RequestUri.AbsoluteUri == absoluteUri
                && hrm.Method == HttpMethod.Get
                && hrm.Headers.Authorization.Scheme == tokenResponse.TokenType
                && hrm.Headers.Authorization.Parameter == tokenResponse.AccessToken;

            return match;
        }
    }
}