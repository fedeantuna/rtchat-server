using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Moq;
using RTChat.Server.Application.Common.Behaviours;
using RTChat.Server.Application.Common.Exceptions;
using RTChat.Server.Application.Common.Security;
using RTChat.Server.Application.Common.Services;
using Shouldly;
using Xunit;

namespace RTChat.Server.Application.UnitTests.Common.Behaviours
{
    [ExcludeFromCodeCoverage]
    public class AuthorizationBehaviourTests
    {
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<IIdentityService> _identityServiceMock;

        private readonly AuthorizationBehaviour<IRequest<String>, String> _sut;
        
        public AuthorizationBehaviourTests()
        {
            this._currentUserServiceMock = new Mock<ICurrentUserService>();
            this._identityServiceMock = new Mock<IIdentityService>();

            this._sut = new AuthorizationBehaviour<IRequest<String>, String>(this._currentUserServiceMock.Object, this._identityServiceMock.Object);
        }
        
        [Fact]
        public async Task Handle_ReturnsRequestHandlerResult_WhenRequestDoesNotRequireAuthorization()
        {
            // Arrange
            var requestMock = new Mock<IRequest<String>>();
            var cancellationToken = default(CancellationToken);
            const String handlerResponse = "test-handler-response";
            Task<String> Handler() => Task.FromResult(handlerResponse);
            
            // Act
            var result = await this._sut.Handle(requestMock.Object, cancellationToken, Handler);
            
            // Assert
            result.ShouldBe(handlerResponse);
        }

        [Fact]
        public async Task Handle_ThrowsUnauthorizedAccessException_WhenCurrentUserIdIsNull()
        {
            // Arrange
            var requestMock = new Mock<IRequest<String>>();
            var cancellationToken = default(CancellationToken);
            const String handlerResponse = "test-handler-response";
            Task<String> Handler() => Task.FromResult(handlerResponse);

            TypeDescriptor.AddAttributes(requestMock.Object, new AuthorizeAttribute());

            this._currentUserServiceMock.Setup(cus => cus.GetUserId()).Returns(() => null);
            
            // Act
            Task Execute() => this._sut.Handle(requestMock.Object, cancellationToken, Handler);
            
            // Assert
            await Execute().ShouldThrowAsync<UnauthorizedAccessException>();
        }
        
        [Fact]
        public async Task Handle_ReturnsRequestHandlerResult_WhenCurrentUserIsInAtLeastOneRole()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var requestMock = new Mock<IRequest<String>>();
            var cancellationToken = default(CancellationToken);
            const String handlerResponse = "test-handler-response";
            Task<String> Handler() => Task.FromResult(handlerResponse);

            const String roleA = "testA";
            const String roleB = "testB";
            const String roleC = "testC";
            const String validRole = roleC;

            var firstAuthorizeAttribute = new AuthorizeAttribute
            {
                Roles = $"{roleA}, {roleB}"
            };
            var secondAuthorizeAttribute = new AuthorizeAttribute
            {
                Roles = roleC
            };
            TypeDescriptor.AddAttributes(requestMock.Object, firstAuthorizeAttribute, secondAuthorizeAttribute);

            this._currentUserServiceMock.Setup(cus => cus.GetUserId()).Returns(userId);
            this._identityServiceMock.Setup(i => i.IsInRole(userId, It.Is<String>(s => s != validRole))).ReturnsAsync(false);
            this._identityServiceMock.Setup(i => i.IsInRole(userId, validRole)).ReturnsAsync(true);
            
            // Act
            var result = await this._sut.Handle(requestMock.Object, cancellationToken, Handler);
            
            // Assert
            result.ShouldBe(handlerResponse);
            this._identityServiceMock.Verify(i => i.IsInRole(userId, roleA), Times.Once);
            this._identityServiceMock.Verify(i => i.IsInRole(userId, roleB), Times.Once);
            this._identityServiceMock.Verify(i => i.IsInRole(userId, roleC), Times.Once);
        }
        
        [Fact]
        public async Task Handle_StopsEarly_WhenCurrentUserIsInOneRole()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var requestMock = new Mock<IRequest<String>>();
            var cancellationToken = default(CancellationToken);
            const String handlerResponse = "test-handler-response";
            Task<String> Handler() => Task.FromResult(handlerResponse);

            const String roleA = "testA";
            const String roleB = "testB";
            const String roleC = "testC";
            const String validRole = roleB;

            var firstAuthorizeAttribute = new AuthorizeAttribute
            {
                Roles = $"{roleA}, {roleB}"
            };
            var secondAuthorizeAttribute = new AuthorizeAttribute
            {
                Roles = roleC
            };
            TypeDescriptor.AddAttributes(requestMock.Object, firstAuthorizeAttribute, secondAuthorizeAttribute);

            this._currentUserServiceMock.Setup(cus => cus.GetUserId()).Returns(userId);
            this._identityServiceMock.Setup(i => i.IsInRole(userId, It.Is<String>(s => s != validRole))).ReturnsAsync(false);
            this._identityServiceMock.Setup(i => i.IsInRole(userId, validRole)).ReturnsAsync(true);
            
            // Act
            await this._sut.Handle(requestMock.Object, cancellationToken, Handler);
            
            // Assert
            this._identityServiceMock.Verify(i => i.IsInRole(userId, roleA), Times.Once);
            this._identityServiceMock.Verify(i => i.IsInRole(userId, roleB), Times.Once);
            this._identityServiceMock.Verify(i => i.IsInRole(userId, roleC), Times.Never);
        }
        
        [Fact]
        public async Task Handle_ThrowsForbiddenAccessException_WhenCurrentUserIsNotInAnyRole()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var requestMock = new Mock<IRequest<String>>();
            var cancellationToken = default(CancellationToken);
            const String handlerResponse = "test-handler-response";
            Task<String> Handler() => Task.FromResult(handlerResponse);

            const String roleA = "testA";
            const String roleB = "testB";
            const String roleC = "testC";
            const String validRole = "testD";

            var firstAuthorizeAttribute = new AuthorizeAttribute
            {
                Roles = $"{roleA}, {roleB}"
            };
            var secondAuthorizeAttribute = new AuthorizeAttribute
            {
                Roles = roleC
            };
            TypeDescriptor.AddAttributes(requestMock.Object, firstAuthorizeAttribute, secondAuthorizeAttribute);

            this._currentUserServiceMock.Setup(cus => cus.GetUserId()).Returns(userId);
            this._identityServiceMock.Setup(i => i.IsInRole(userId, It.Is<String>(s => s != validRole))).ReturnsAsync(false);
            this._identityServiceMock.Setup(i => i.IsInRole(userId, validRole)).ReturnsAsync(true);
            
            // Act
            Task Execute() => this._sut.Handle(requestMock.Object, cancellationToken, Handler);
            
            // Assert
            await Execute().ShouldThrowAsync<ForbiddenAccessException>();
            this._identityServiceMock.Verify(i => i.IsInRole(userId, roleA), Times.Once);
            this._identityServiceMock.Verify(i => i.IsInRole(userId, roleB), Times.Once);
            this._identityServiceMock.Verify(i => i.IsInRole(userId, roleC), Times.Once);
        }
        
        [Fact]
        public async Task Handle_ReturnsRequestHandlerResult_WhenCurrentUserIsAuthorizedByPolicies()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var requestMock = new Mock<IRequest<String>>();
            var cancellationToken = default(CancellationToken);
            const String handlerResponse = "test-handler-response";
            Task<String> Handler() => Task.FromResult(handlerResponse);

            const String policyA = "testA";
            const String policyB = "testB";

            var firstAuthorizeAttribute = new AuthorizeAttribute
            {
                Policy = policyA
            };
            var secondAuthorizeAttribute = new AuthorizeAttribute
            {
                Policy = policyB
            };
            TypeDescriptor.AddAttributes(requestMock.Object, firstAuthorizeAttribute, secondAuthorizeAttribute);

            this._currentUserServiceMock.Setup(cus => cus.GetUserId()).Returns(userId);
            this._identityServiceMock.Setup(i => i.Authorize(userId, policyA)).ReturnsAsync(true);
            this._identityServiceMock.Setup(i => i.Authorize(userId, policyB)).ReturnsAsync(true);
            
            // Act
            var result = await this._sut.Handle(requestMock.Object, cancellationToken, Handler);
            
            // Assert
            result.ShouldBe(handlerResponse);
            this._identityServiceMock.Verify(i => i.Authorize(userId, policyA), Times.Once);
            this._identityServiceMock.Verify(i => i.Authorize(userId, policyB), Times.Once);
        }
        
        [Fact]
        public async Task Handle_ThrowsForbiddenAccessException_WhenCurrentUserIsNotAuthorizedByAtLeastOnePolicy()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var requestMock = new Mock<IRequest<String>>();
            var cancellationToken = default(CancellationToken);
            const String handlerResponse = "test-handler-response";
            Task<String> Handler() => Task.FromResult(handlerResponse);

            const String policyA = "testA";
            const String policyB = "testB";

            var firstAuthorizeAttribute = new AuthorizeAttribute
            {
                Policy = policyA
            };
            var secondAuthorizeAttribute = new AuthorizeAttribute
            {
                Policy = policyB
            };
            TypeDescriptor.AddAttributes(requestMock.Object, firstAuthorizeAttribute, secondAuthorizeAttribute);

            this._currentUserServiceMock.Setup(cus => cus.GetUserId()).Returns(userId);
            this._identityServiceMock.Setup(i => i.Authorize(userId, policyA)).ReturnsAsync(true);
            this._identityServiceMock.Setup(i => i.Authorize(userId, policyB)).ReturnsAsync(false);
            
            // Act
            Task Execute() => this._sut.Handle(requestMock.Object, cancellationToken, Handler);
            
            // Assert
            await Execute().ShouldThrowAsync<ForbiddenAccessException>();
            this._identityServiceMock.Verify(i => i.Authorize(userId, policyA), Times.Once);
            this._identityServiceMock.Verify(i => i.Authorize(userId, policyB), Times.Once);
        }
        
        [Fact]
        public async Task Handle_ThrowsForbiddenAccessExceptionAndStopsEarlier_WhenCurrentUserIsNotAuthorizedByOnePolicy()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var requestMock = new Mock<IRequest<String>>();
            var cancellationToken = default(CancellationToken);
            const String handlerResponse = "test-handler-response";
            Task<String> Handler() => Task.FromResult(handlerResponse);

            const String policyA = "testA";
            const String policyB = "testB";

            var firstAuthorizeAttribute = new AuthorizeAttribute
            {
                Policy = policyA
            };
            var secondAuthorizeAttribute = new AuthorizeAttribute
            {
                Policy = policyB
            };
            TypeDescriptor.AddAttributes(requestMock.Object, firstAuthorizeAttribute, secondAuthorizeAttribute);

            this._currentUserServiceMock.Setup(cus => cus.GetUserId()).Returns(userId);
            this._identityServiceMock.Setup(i => i.Authorize(userId, policyA)).ReturnsAsync(false);
            this._identityServiceMock.Setup(i => i.Authorize(userId, policyB)).ReturnsAsync(true);
            
            // Act
            Task Execute() => this._sut.Handle(requestMock.Object, cancellationToken, Handler);
            
            // Assert
            await Execute().ShouldThrowAsync<ForbiddenAccessException>();
            this._identityServiceMock.Verify(i => i.Authorize(userId, policyA), Times.Once);
            this._identityServiceMock.Verify(i => i.Authorize(userId, policyB), Times.Never);
        }
    }
}