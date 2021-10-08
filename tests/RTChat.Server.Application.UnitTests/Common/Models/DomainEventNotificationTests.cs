using System;
using System.Diagnostics.CodeAnalysis;
using Moq;
using RTChat.Server.Application.Common.Models;
using RTChat.Server.Domain.Common;
using Shouldly;
using Xunit;

namespace RTChat.Server.Application.UnitTests.Common.Models
{
    [ExcludeFromCodeCoverage]
    public class DomainEventNotificationTests
    {
        [Fact]
        public void DomainEventNotification_RequiresDomainEvent()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;
            var domainEventMock = new Mock<DomainEvent>(now);
            
            // Act
            var sut = new DomainEventNotification<DomainEvent>(domainEventMock.Object);
            
            // Assert
            sut.DomainEvent.ShouldBe(domainEventMock.Object);
        }
    }
}