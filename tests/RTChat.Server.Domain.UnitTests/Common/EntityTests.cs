using System;
using System.Diagnostics.CodeAnalysis;
using Moq;
using RTChat.Server.Domain.Common;
using Shouldly;
using Xunit;

namespace RTChat.Server.Domain.UnitTests.Common
{
    [ExcludeFromCodeCoverage]
    public class EntityTests
    {
        [Fact]
        public void Entity_RequiresId()
        {
            // Arrange
            var id = Guid.NewGuid();
            
            // Act
            var sut = new Mock<Entity>(id);
            
            // Assert
            sut.Object.Id.ShouldBe(id);
        }

        [Fact]
        public void Entity_HasAuditFields()
        {
            // Arrange
            var id = Guid.NewGuid();
            var createdAt = DateTime.UtcNow.Subtract(TimeSpan.FromDays(3));
            var createdBy = Guid.NewGuid();
            var lastModifiedAt = DateTime.UtcNow;
            var lastModifiedBy = Guid.NewGuid();

            // Act
            var sut = new Mock<Entity>(id)
            {
                Object =
                {
                    CreatedAt = createdAt,
                    CreatedBy = createdBy,
                    LastModifiedAt = lastModifiedAt,
                    LastModifiedBy = lastModifiedBy
                }
            };

            // Assert
            sut.Object.CreatedAt.ShouldBe(createdAt);
            sut.Object.CreatedBy.ShouldBe(createdBy);
            sut.Object.LastModifiedAt.ShouldBe(lastModifiedAt);
            sut.Object.LastModifiedBy.ShouldBe(lastModifiedBy);
        }
    }
}