using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using RTChat.Server.Application.Common.Behaviours;
using RTChat.Server.Application.Common.Messages;
using RTChat.Server.Application.Common.Services;
using RTChat.Server.Application.UnitTests.Shared;
using Xunit;

namespace RTChat.Server.Application.UnitTests.Common.Behaviours
{
    [ExcludeFromCodeCoverage]
    public class LoggingBehaviourTests
    {
        private readonly Mock<ILogger<IBaseRequest>> _loggerMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<IIdentityService> _identityServiceMock;

        private readonly LoggingBehaviour<IBaseRequest> _sut;

        public LoggingBehaviourTests()
        {
            this._loggerMock = new Mock<ILogger<IBaseRequest>>();
            this._currentUserServiceMock = new Mock<ICurrentUserService>();
            this._identityServiceMock = new Mock<IIdentityService>();

            this._sut = new LoggingBehaviour<IBaseRequest>(this._loggerMock.Object,
                this._currentUserServiceMock.Object,
                this._identityServiceMock.Object);
        }

        [Fact]
        public async Task Process_LogsInformationAboutTheUserAndTheRequest_WhenCurrentUserIdIsNotNull()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            const String userName = "obi-wan.kenobi";
            var requestMock = new Mock<IBaseRequest>();
            const String requestName = nameof(IBaseRequest);
            var cancellationToken = default(CancellationToken);

            this._currentUserServiceMock.Setup(cus => cus.GetUserId()).Returns(userId);
            this._identityServiceMock.Setup(i => i.GetUsername(userId)).ReturnsAsync(userName);
            
            // Act
            await this._sut.Process(requestMock.Object, cancellationToken);
            
            // Assert
            this._loggerMock.Verify(l =>
                    l.Log(LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((state, t) => LoggerHelper.CheckValue(state, LoggingBehaviourMessages.LoggingBehaviourInformationMessage, "{OriginalFormat}")
                                                          && LoggerHelper.CheckValue(state, requestName, LoggingBehaviourMessages.LoggingBehaviourInformationMessageNameParameter)
                                                          && LoggerHelper.CheckValue(state, userId, LoggingBehaviourMessages.LoggingBehaviourInformationMessageUserIdParameter)
                                                          && LoggerHelper.CheckValue(state, userName, LoggingBehaviourMessages.LoggingBehaviourInformationMessageUserNameParameter)
                                                          && LoggerHelper.CheckValue(state, requestMock.Object, LoggingBehaviourMessages.LoggingBehaviourInformationMessageRequestParameter)),
                        It.IsAny<Exception>(),
                        (Func<It.IsAnyType, Exception, String>)It.IsAny<Object>()),
                Times.Once);
            this._currentUserServiceMock.Verify(cus => cus.GetUserId(), Times.Once);
            this._identityServiceMock.Verify(i => i.GetUsername(userId), Times.Once);
        }
        
        [Fact]
        public async Task Process_LogsInformationAboutTheRequest_WhenCurrentUserIdIsNull()
        {
            // Arrange
            var userName = String.Empty;
            var requestMock = new Mock<IBaseRequest>();
            const String requestName = nameof(IBaseRequest);
            var cancellationToken = default(CancellationToken);

            this._currentUserServiceMock.Setup(cus => cus.GetUserId()).Returns((String)null);
            
            // Act
            await this._sut.Process(requestMock.Object, cancellationToken);
            
            // Assert
            this._loggerMock.Verify(l =>
                    l.Log(LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((state, t) => LoggerHelper.CheckValue(state, LoggingBehaviourMessages.LoggingBehaviourInformationMessage, "{OriginalFormat}")
                                                          && LoggerHelper.CheckValue(state, requestName, LoggingBehaviourMessages.LoggingBehaviourInformationMessageNameParameter)
                                                          && LoggerHelper.CheckValue(state, String.Empty, LoggingBehaviourMessages.LoggingBehaviourInformationMessageUserIdParameter)
                                                          && LoggerHelper.CheckValue(state, userName, LoggingBehaviourMessages.LoggingBehaviourInformationMessageUserNameParameter)
                                                          && LoggerHelper.CheckValue(state, requestMock.Object, LoggingBehaviourMessages.LoggingBehaviourInformationMessageRequestParameter)),
                        It.IsAny<Exception>(),
                        (Func<It.IsAnyType, Exception, String>)It.IsAny<Object>()),
                Times.Once);
            this._currentUserServiceMock.Verify(cus => cus.GetUserId(), Times.Once);
            this._identityServiceMock.Verify(i => i.GetUsername(It.IsAny<String>()), Times.Never);
        }
    }
}