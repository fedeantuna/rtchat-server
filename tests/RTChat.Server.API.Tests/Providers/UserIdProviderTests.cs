using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RTChat.Server.API.Providers;
using Xunit;

namespace RTChat.Server.API.Tests.Providers
{
    public class UserIdProviderTests
    {
        private readonly UserIdProvider _sut;

        public UserIdProviderTests()
        {
            this._sut = new UserIdProvider();
        }
        
        [Fact]
        public void GetUserId_ReturnsNameIdentifier()
        {
            // Arrange
            var nameIdentifier = Guid.NewGuid().ToString();
            var userIdClaim = new Claim(ClaimTypes.NameIdentifier, nameIdentifier);
            var userClaims = new List<Claim> { userIdClaim };
            var user = new ClaimsIdentity(userClaims);
            
            var connectionContext = new DefaultConnectionContext();
            connectionContext.User = new ClaimsPrincipal(user);
            var hubConnectionContextOptions = new HubConnectionContextOptions();
            var loggerFactory = new LoggerFactory();
            
            var hubConnectionContext = new HubConnectionContext(connectionContext, hubConnectionContextOptions, loggerFactory);

            // Act
            var result = this._sut.GetUserId(hubConnectionContext);

            // Assert
            Assert.Equal(nameIdentifier, result);
        }
        
        [Fact]
        public void GetUserId_ReturnsNull_WhenThereIsNoUser()
        {
            // Arrange
            var connectionContext = new DefaultConnectionContext();
            var hubConnectionContextOptions = new HubConnectionContextOptions();
            var loggerFactory = new LoggerFactory();
            
            var hubConnectionContext = new HubConnectionContext(connectionContext, hubConnectionContextOptions, loggerFactory);

            // Act
            var result = this._sut.GetUserId(hubConnectionContext);

            // Assert
            Assert.Null(result);
        }
        
        [Fact]
        public void GetUserId_ReturnsNull_WhenThereIsNoNameIdentifierClaim()
        {
            // Arrange
            var user = new ClaimsIdentity();
            
            var connectionContext = new DefaultConnectionContext();
            connectionContext.User = new ClaimsPrincipal(user);
            var hubConnectionContextOptions = new HubConnectionContextOptions();
            var loggerFactory = new LoggerFactory();
            
            var hubConnectionContext = new HubConnectionContext(connectionContext, hubConnectionContextOptions, loggerFactory);

            // Act
            var result = this._sut.GetUserId(hubConnectionContext);

            // Assert
            Assert.Null(result);
        }
    }
}