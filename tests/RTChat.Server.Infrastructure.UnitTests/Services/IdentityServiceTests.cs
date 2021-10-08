using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using RTChat.Server.Application.Common.Options;
using RTChat.Server.Infrastructure.Models;
using RTChat.Server.Infrastructure.Services;
using Shouldly;
using Xunit;

namespace RTChat.Server.Infrastructure.UnitTests.Services
{
    [ExcludeFromCodeCoverage]
    public class IdentityServiceTests
    {
        private readonly Mock<IAuthorizationService> _authorizationServiceMock;
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;

        private readonly IdentityService _sut;

        private Auth0ManagementApiOptions _auth0ManagementApiOptions;
        
        public IdentityServiceTests()
        {
            this._authorizationServiceMock = new Mock<IAuthorizationService>();
            this._httpMessageHandlerMock = new Mock<HttpMessageHandler>();

            var optionsMock = this.SetUpOptions();
            var httpClientFactoryMock = this.SetUpHttpClientFactory();

            this._sut = new IdentityService(this._authorizationServiceMock.Object,
                httpClientFactoryMock.Object,
                optionsMock.Object);
        }

        [Fact]
        public async Task GetUsername_ReturnsTheUserNameForTheUser()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            const String email = "obi-wan.kenobi@jediorder.rep";
            const String picture = "some-picture";

            var user = new User
            {
                Email = email,
                Id = userId,
                Picture = picture,
                Username = email
            };
            var tokenResponse = new TokenResponse
            {
                Scope = "test-scope",
                AccessToken = "test-access-token",
                ExpiresIn = 42,
                TokenType = "test-token-type"
            };
            
            const String fields = "user_id,email,picture";
            var userByIdEndpoint =
                $"{this._auth0ManagementApiOptions.BaseAddress}{this._auth0ManagementApiOptions.UsersByIdEndpoint}/{userId}?fields={fields}&include_fields=true";

            Expression<Func<HttpRequestMessage, Boolean>> requestMessageMatch = hrm =>
                hrm.RequestUri.AbsoluteUri == userByIdEndpoint
                && hrm.Method == HttpMethod.Get
                && hrm.Headers.Authorization.Scheme == tokenResponse.TokenType
                && hrm.Headers.Authorization.Parameter == tokenResponse.AccessToken;
            
            this._httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(nameof(HttpClient.SendAsync), ItExpr.Is(requestMessageMatch),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(user), Encoding.UTF8,
                        MediaTypeNames.Application.Json)
                });

            // Act
            var result = await this._sut.GetUsername(userId);
            
            // Assert
            result.ShouldBe(email);
        }
        
        private Mock<IHttpClientFactory> SetUpHttpClientFactory()
        {
            var httpClient = new HttpClient(this._httpMessageHandlerMock.Object);
            httpClient.BaseAddress = new Uri(this._auth0ManagementApiOptions.BaseAddress);

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            
            httpClientFactoryMock.Setup(hcf => hcf.CreateClient(nameof(IdentityService)))
                .Returns(httpClient);

            return httpClientFactoryMock;
        }

        private Mock<IOptions<Auth0ManagementApiOptions>> SetUpOptions()
        {
            this._auth0ManagementApiOptions = new Auth0ManagementApiOptions
            {
                Audience = "auth-0-management-api-audience",
                BaseAddress = "https://localhost/auth-0-management-api-base-address",
                ClientId = "auth-0-management-api-client-id",
                ClientSecret = "auth-0-management-api-client-secret",
                TokenEndpoint = "auth-0-management-api-token-endpoint",
                UsersByEmailEndpoint = "/auth-0-management-api-users-by-email-endpoint",
                UsersByIdEndpoint = "/auth-0-management-api-users-by-id-endpoint"
            };

            var optionsMock = new Mock<IOptions<Auth0ManagementApiOptions>>();

            optionsMock.SetupGet(o => o.Value).Returns(this._auth0ManagementApiOptions);

            return optionsMock;
        }
    }
}