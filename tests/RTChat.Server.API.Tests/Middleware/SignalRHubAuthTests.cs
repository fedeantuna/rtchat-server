using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;
using RTChat.Server.API.Middleware;
using Xunit;

namespace RTChat.Server.API.Tests.Middleware
{
    [ExcludeFromCodeCoverage]
    public class SignalRHubAuthTests
    {
        private readonly Mock<RequestDelegate> _requestDelegateMock;

        private readonly SignalRHubAuth _sut;
        
        public SignalRHubAuthTests()
        {
            this._requestDelegateMock = new Mock<RequestDelegate>();

            this._sut = new SignalRHubAuth(this._requestDelegateMock.Object);
        }

        [Fact]
        public async Task
            Invoke_AddsAuthorizationHeaderWithAccessToken_WhenPathStartsWithForwardSlashHubAndTokenIsPresent()
        {
            // Arrange
            const String requestPath = "/hub/chat/negotiate";
            const String accessToken = "access_token";
            const String token = "token";
            var accessTokenQuery = new Dictionary<String, StringValues>
            {
                { accessToken, new StringValues(token) }
            };

            const String authorizationHeaderKey = "Authorization";
            var authorizationHeaderValue = $"Bearer {token}";

            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Path = requestPath,
                    Query = new QueryCollection(accessTokenQuery)
                }
            };

            // Act
            await this._sut.Invoke(httpContext);
            
            // Assert
            Assert.Equal(authorizationHeaderKey, httpContext.Request.Headers.First().Key);
            Assert.Equal(authorizationHeaderValue, httpContext.Request.Headers.First().Value);
        }
        
        [Fact]
        public async Task
            Invoke_DoesNotAddAuthorizationHeader_WhenPathDoesNotStartsWithForwardSlashHub()
        {
            // Arrange
            const String requestPath = "/chat/negotiate";
            const String accessToken = "access_token";
            const String token = "token";
            var accessTokenQuery = new Dictionary<String, StringValues>
            {
                { accessToken, new StringValues(token) }
            };

            const String authorizationHeaderKey = "Authorization";

            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Path = requestPath,
                    Query = new QueryCollection(accessTokenQuery)
                }
            };

            // Act
            await this._sut.Invoke(httpContext);
            
            // Assert
            Assert.DoesNotContain(httpContext.Request.Headers.Keys, item => item == authorizationHeaderKey);
        }
        
        [Fact]
        public async Task
            Invoke_DoesNotAddAuthorizationHeader_WhenAccessTokenIsNotPresent()
        {
            // Arrange
            const String requestPath = "/hub/chat/negotiate";

            const String authorizationHeaderKey = "Authorization";

            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Path = requestPath
                }
            };

            // Act
            await this._sut.Invoke(httpContext);
            
            // Assert
            Assert.DoesNotContain(httpContext.Request.Headers.Keys, item => item == authorizationHeaderKey);
        }
        
        [Fact]
        public async Task Invoke_CallsNextRequestDelegate()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();

            // Act
            await this._sut.Invoke(httpContext);
            
            // Assert
            this._requestDelegateMock.Verify(rdm => rdm(httpContext), Times.Once);
        }
    }
}