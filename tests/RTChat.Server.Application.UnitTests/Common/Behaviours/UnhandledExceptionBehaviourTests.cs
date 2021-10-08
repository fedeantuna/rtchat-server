using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using RTChat.Server.Application.Common.Behaviours;
using RTChat.Server.Application.Common.Messages;
using RTChat.Server.Application.UnitTests.Shared;
using Shouldly;
using Xunit;

namespace RTChat.Server.Application.UnitTests.Common.Behaviours
{
    [ExcludeFromCodeCoverage]
    public class UnhandledExceptionBehaviourTests
    {
        private readonly Mock<ILogger<IRequest<String>>> _loggerMock;

        private readonly UnhandledExceptionBehaviour<IRequest<String>, String> _sut;
        
        public UnhandledExceptionBehaviourTests()
        {
            this._loggerMock = new Mock<ILogger<IRequest<String>>>();

            this._sut = new UnhandledExceptionBehaviour<IRequest<String>, String>(this._loggerMock.Object);
        }
        
        [Fact]
        public async Task Handle_ReturnsRequestHandlerResult()
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
        public async Task Handle_LogsErrorAndThrowsException_WhenRequestHandlerIsNotValid()
        {
            // Arrange
            var requestMock = new Mock<IRequest<String>>();
            var requestName = typeof(IRequest<String>).Name;
            var cancellationToken = default(CancellationToken);
            var exception = new Exception("test-exception");
            Task<String> Handler() => Task.FromException<String>(exception);
            
            // Act
            Task<String> Execute() => this._sut.Handle(requestMock.Object, cancellationToken, Handler);
            
            // Assert
            await Execute().ShouldThrowAsync<Exception>(exception.Message);
            this._loggerMock.Verify(l =>
                l.Log(LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, t) => LoggerHelper.CheckValue(state, UnhandledExceptionBehaviourMessages.UnhandledExceptionErrorMessage, "{OriginalFormat}") 
                                                      && LoggerHelper.CheckValue(state, requestName, UnhandledExceptionBehaviourMessages.UnhandledExceptionErrorMessageNameParameter) 
                                                      && LoggerHelper.CheckValue(state, requestMock.Object, UnhandledExceptionBehaviourMessages.UnhandledExceptionErrorMessageRequestParameter)),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, String>)It.IsAny<Object>()),
                Times.Once);
        }
    }
}