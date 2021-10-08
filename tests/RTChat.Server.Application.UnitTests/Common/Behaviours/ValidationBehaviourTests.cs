using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using RTChat.Server.Application.Common.Behaviours;
using Shouldly;
using Xunit;

namespace RTChat.Server.Application.UnitTests.Common.Behaviours
{
    [ExcludeFromCodeCoverage]
    public class ValidationBehaviourTests
    {
        private readonly Mock<ICollection<IValidator<IRequest<String>>>> _validatorsMock;
        
        private readonly ValidationBehaviour<IRequest<String>, String> _sut;

        public ValidationBehaviourTests()
        {
            this._validatorsMock = new Mock<ICollection<IValidator<IRequest<String>>>>();
            
            this._sut = new ValidationBehaviour<IRequest<String>, String>(this._validatorsMock.Object);
        }

        [Fact]
        public async Task Handle_ReturnsRequestHandlerResult_WhenThereAreNoValidators()
        {
            // Arrange
            var requestMock = new Mock<IRequest<String>>();
            var cancellationToken = default(CancellationToken);
            const String handlerResponse = "test-handler-response";
            Task<String> Handler() => Task.FromResult(handlerResponse);

            this._validatorsMock.Setup(v => v.Count).Returns(0);

            // Act
            var result = await this._sut.Handle(requestMock.Object, cancellationToken, Handler);
            
            // Arrange
            result.ShouldBe(handlerResponse);
        }

        [Fact]
        public async Task Handle_CallsValidateAsyncOnEachValidatorAndReturnsRequestHandlerResult_WhenValidationsAreSuccessful()
        {
            // Arrange
            var requestMock = new Mock<IRequest<String>>();
            var cancellationToken = default(CancellationToken);
            const String handlerResponse = "test-handler-response";
            Task<String> Handler() => Task.FromResult(handlerResponse);

            var validatorA = new Mock<IValidator<IRequest<String>>>();
            var validatorB = new Mock<IValidator<IRequest<String>>>();

            validatorA.Setup(v =>
                v.ValidateAsync(
                    It.Is<ValidationContext<IRequest<String>>>(vc => vc.InstanceToValidate == requestMock.Object),
                    cancellationToken)).ReturnsAsync(new ValidationResult());
            validatorB.Setup(v =>
                v.ValidateAsync(
                    It.Is<ValidationContext<IRequest<String>>>(vc => vc.InstanceToValidate == requestMock.Object),
                    cancellationToken)).ReturnsAsync(new ValidationResult());

            var validators = new List<IValidator<IRequest<String>>>
            {
                validatorA.Object,
                validatorB.Object
            };
            
            this._validatorsMock.Setup(v => v.Count).Returns(validators.Count);
            this._validatorsMock.Setup(v => v.GetEnumerator()).Returns(validators.GetEnumerator());

            // Act
            var result = await this._sut.Handle(requestMock.Object, cancellationToken, Handler);
            
            // Arrange
            validatorA.Verify(
                v => v.ValidateAsync(
                    It.Is<ValidationContext<IRequest<String>>>(vc => vc.InstanceToValidate == requestMock.Object),
                    cancellationToken), Times.Once);
            validatorB.Verify(
                v => v.ValidateAsync(
                    It.Is<ValidationContext<IRequest<String>>>(vc => vc.InstanceToValidate == requestMock.Object),
                    cancellationToken), Times.Once);
            result.ShouldBe(handlerResponse);
        }
        
        [Fact]
        public async Task Handle_CallsValidateAsyncOnEachValidatorAndThrowsValidationException_WhenValidationsContainFailures()
        {
            // Arrange
            var requestMock = new Mock<IRequest<String>>();
            var cancellationToken = default(CancellationToken);
            const String handlerResponse = "test-handler-response";
            Task<String> Handler() => Task.FromResult(handlerResponse);

            var validatorA = new Mock<IValidator<IRequest<String>>>();
            var validatorB = new Mock<IValidator<IRequest<String>>>();

            var validationFailures = new List<ValidationFailure>
            {
                new ("test-property", "test-error-message")
            };

            validatorA.Setup(v =>
                v.ValidateAsync(
                    It.Is<ValidationContext<IRequest<String>>>(vc => vc.InstanceToValidate == requestMock.Object),
                    cancellationToken)).ReturnsAsync(new ValidationResult());
            validatorB.Setup(v =>
                v.ValidateAsync(
                    It.Is<ValidationContext<IRequest<String>>>(vc => vc.InstanceToValidate == requestMock.Object),
                    cancellationToken)).ReturnsAsync(new ValidationResult(validationFailures));

            var validators = new List<IValidator<IRequest<String>>>
            {
                validatorA.Object,
                validatorB.Object
            };
            
            this._validatorsMock.Setup(v => v.Count).Returns(validators.Count);
            this._validatorsMock.Setup(v => v.GetEnumerator()).Returns(validators.GetEnumerator());

            // Act
            Task Execute() => this._sut.Handle(requestMock.Object, cancellationToken, Handler);
            
            // Arrange
            await Execute().ShouldThrowAsync<ValidationException>();
            validatorA.Verify(
                v => v.ValidateAsync(
                    It.Is<ValidationContext<IRequest<String>>>(vc => vc.InstanceToValidate == requestMock.Object),
                    cancellationToken), Times.Once);
            validatorB.Verify(
                v => v.ValidateAsync(
                    It.Is<ValidationContext<IRequest<String>>>(vc => vc.InstanceToValidate == requestMock.Object),
                    cancellationToken), Times.Once);
        }
    }
}