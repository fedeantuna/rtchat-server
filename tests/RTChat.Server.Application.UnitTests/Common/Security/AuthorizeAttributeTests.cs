using System;
using System.Diagnostics.CodeAnalysis;
using RTChat.Server.Application.Common.Security;
using Shouldly;
using Xunit;

namespace RTChat.Server.Application.UnitTests.Common.Security
{
    [ExcludeFromCodeCoverage]
    public class AuthorizeAttributeTests
    {
        [Fact]
        public void AuthorizeAttribute_CanBeCreatedWithoutParameters()
        {
            // Act
            var sut = new AuthorizeAttribute();
            
            // Assert
            sut.ShouldBeOfType(typeof(AuthorizeAttribute));
        }

        [Fact]
        public void AuthorizeAttribute_CanBeCreatedWithPolicy()
        {
            // Arrange
            const String policy = "test-policy";
            
            // Act
            var sut = new AuthorizeAttribute(policy);
            
            // Assert
            sut.Policy.ShouldBe(policy);
        }

        [Fact]
        public void AuthorizeAttribute_HasPolicyProperty()
        {
            // Arrange
            const String policy = "test-policy";
            
            // Act
            var sut = new AuthorizeAttribute
            {
                Policy = policy
            };

            // Assert
            sut.Policy.ShouldBe(policy);
        }
        
        [Fact]
        public void AuthorizeAttribute_HasRolesProperty()
        {
            // Arrange
            const String roles = "test-roles";
            
            // Act
            var sut = new AuthorizeAttribute
            {
                Roles = roles
            };

            // Assert
            sut.Roles.ShouldBe(roles);
        }
        
        [Fact]
        public void AuthorizeAttribute_HasAuthenticationSchemesProperty()
        {
            // Arrange
            const String authenticationSchemes = "test-authentication-schemes";
            
            // Act
            var sut = new AuthorizeAttribute
            {
                AuthenticationSchemes = authenticationSchemes
            };

            // Assert
            sut.AuthenticationSchemes.ShouldBe(authenticationSchemes);
        }
    }
}