using System;
using System.Diagnostics.CodeAnalysis;
using Moq;
using RTChat.Server.Domain.Common;
using Shouldly;
using Xunit;

namespace RTChat.Server.Domain.UnitTests.Common
{
    [ExcludeFromCodeCoverage]
    public class DomainEventTests
    {
        [Fact]
        public void DomainEvent_RequiresOccurredAt()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;
            
            // Act
            var sut = new Mock<DomainEvent>(now);
            
            // Assert
            sut.Object.OccurredAt.ShouldBe(now);
        }

        [Fact]
        public void DomainEvent_HasIsPublishedField()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;
            const Boolean isPublished = true;
            
            // Act
            var sut = new Mock<DomainEvent>(now)
            {
                Object =
                {
                    IsPublished = isPublished
                }
            };
            
            // Assert
            sut.Object.IsPublished.ShouldBe(isPublished);
        }

        [Fact]
        public void IsPublished_DefaultsToFalse()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;
            
            // Act
            var sut = new Mock<DomainEvent>(now);
            
            // Assert
            sut.Object.IsPublished.ShouldBeFalse();
        }
    }
}