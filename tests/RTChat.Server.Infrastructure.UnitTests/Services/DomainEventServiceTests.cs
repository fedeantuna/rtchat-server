using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using RTChat.Server.Domain.Common;
using RTChat.Server.Infrastructure.Messages;
using RTChat.Server.Infrastructure.Services;
using RTChat.Server.Infrastructure.UnitTests.Shared;
using Xunit;

namespace RTChat.Server.Infrastructure.UnitTests.Services
{
    [ExcludeFromCodeCoverage]
    public class DomainEventServiceTests
    {
        private readonly Mock<ILogger<DomainEventService>> _loggerMock;
        private readonly Mock<IPublisher> _mediatRMock;

        private readonly DomainEventService _sut;

        public DomainEventServiceTests()
        {
            this._loggerMock = new Mock<ILogger<DomainEventService>>();
            this._mediatRMock = new Mock<IPublisher>();

            this._sut = new DomainEventService(this._loggerMock.Object, this._mediatRMock.Object);
        }
        
        [Fact]
        public async Task Publish_CallsPublishOnMediatorWithCorrespondingDomainEvent()
        {
            // Arrange
            var now = DateTimeOffset.Now;
            var domainEventMock = new Mock<DomainEvent>(now);
            var cancellationToken = default(CancellationToken);
            
            // Act
            await this._sut.Publish(domainEventMock.Object, cancellationToken);
            
            // Assert
            this._mediatRMock.Verify(p => p.Publish(It.IsAny<INotification>(), cancellationToken), Times.Once);
        }
        
        [Fact]
        public async Task Publish_LogsInformationAboutTheEvent()
        {
            // Arrange
            var now = DateTimeOffset.Now;
            var domainEventMock = new Mock<DomainEvent>(now);
            var domainEventName = domainEventMock.Object.GetType().Name;
            var cancellationToken = default(CancellationToken);
            
            // Act
            await this._sut.Publish(domainEventMock.Object, cancellationToken);
            
            // Assert
            this._loggerMock.Verify(l =>
                    l.Log(LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((state, t) => LoggerHelper.CheckValue(state, DomainEventServiceMessages.DomainEventServiceInformationMessage, "{OriginalFormat}")
                                                          && LoggerHelper.CheckValue(state, domainEventName, DomainEventServiceMessages.DomainEventServiceInformationMessageEventParameter)),
                        It.IsAny<Exception>(),
                        (Func<It.IsAnyType, Exception, String>)It.IsAny<Object>()),
                Times.Once);
        }
    }
}