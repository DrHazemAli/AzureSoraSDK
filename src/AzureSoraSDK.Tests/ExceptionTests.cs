using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using AzureSoraSDK.Exceptions;
using FluentAssertions;
using Xunit;

namespace AzureSoraSDK.Tests
{
    public class ExceptionTests
    {
        [Fact]
        public void SoraException_DefaultConstructor_CreatesInstance()
        {
            // Act
            var exception = new SoraException();

            // Assert
            exception.Should().NotBeNull();
            exception.Message.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void SoraException_WithMessage_SetsMessage()
        {
            // Arrange
            const string message = "Test error message";

            // Act
            var exception = new SoraException(message);

            // Assert
            exception.Message.Should().Be(message);
            exception.ErrorCode.Should().BeNull();
            exception.HttpStatusCode.Should().BeNull();
        }

        [Fact]
        public void SoraException_WithMessageAndInnerException_SetsProperties()
        {
            // Arrange
            const string message = "Outer error";
            var innerException = new InvalidOperationException("Inner error");

            // Act
            var exception = new SoraException(message, innerException);

            // Assert
            exception.Message.Should().Be(message);
            exception.InnerException.Should().Be(innerException);
        }

        [Fact]
        public void SoraException_WithMessageAndErrorCode_SetsProperties()
        {
            // Arrange
            const string message = "Error occurred";
            const string errorCode = "ERR_001";

            // Act
            var exception = new SoraException(message, errorCode);

            // Assert
            exception.Message.Should().Be(message);
            exception.ErrorCode.Should().Be(errorCode);
        }

        [Fact]
        public void SoraException_WithMessageStatusCodeAndErrorCode_SetsAllProperties()
        {
            // Arrange
            const string message = "Request failed";
            const HttpStatusCode statusCode = HttpStatusCode.BadRequest;
            const string errorCode = "BAD_REQUEST";

            // Act
            var exception = new SoraException(message, statusCode, errorCode);

            // Assert
            exception.Message.Should().Be(message);
            exception.HttpStatusCode.Should().Be(statusCode);
            exception.ErrorCode.Should().Be(errorCode);
        }

        [Fact]
        public void SoraAuthenticationException_SetsCorrectDefaults()
        {
            // Arrange
            const string message = "Auth failed";

            // Act
            var exception = new SoraAuthenticationException(message);

            // Assert
            exception.Message.Should().Be(message);
            exception.HttpStatusCode.Should().Be(HttpStatusCode.Unauthorized);
            exception.ErrorCode.Should().Be("AUTH_FAILED");
        }

        [Fact]
        public void SoraNotFoundException_WithMessage_SetsCorrectDefaults()
        {
            // Arrange
            const string message = "Resource not found";

            // Act
            var exception = new SoraNotFoundException(message);

            // Assert
            exception.Message.Should().Be(message);
            exception.HttpStatusCode.Should().Be(HttpStatusCode.NotFound);
            exception.ErrorCode.Should().Be("NOT_FOUND");
            exception.ResourceId.Should().BeNull();
        }

        [Fact]
        public void SoraNotFoundException_WithMessageAndResourceId_SetsProperties()
        {
            // Arrange
            const string message = "Job not found";
            const string resourceId = "job-123";

            // Act
            var exception = new SoraNotFoundException(message, resourceId);

            // Assert
            exception.Message.Should().Be(message);
            exception.ResourceId.Should().Be(resourceId);
        }

        [Fact]
        public void SoraRateLimitException_WithMessage_SetsCorrectDefaults()
        {
            // Arrange
            const string message = "Rate limit exceeded";

            // Act
            var exception = new SoraRateLimitException(message);

            // Assert
            exception.Message.Should().Be(message);
            exception.HttpStatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            exception.ErrorCode.Should().Be("RATE_LIMIT_EXCEEDED");
            exception.RetryAfter.Should().BeNull();
        }

        [Fact]
        public void SoraRateLimitException_WithMessageAndRetryAfter_SetsProperties()
        {
            // Arrange
            const string message = "Rate limit exceeded";
            var retryAfter = TimeSpan.FromSeconds(30);

            // Act
            var exception = new SoraRateLimitException(message, retryAfter);

            // Assert
            exception.Message.Should().Be(message);
            exception.RetryAfter.Should().Be(retryAfter);
        }

        [Fact]
        public void SoraTimeoutException_SetsProperties()
        {
            // Arrange
            const string message = "Request timed out";
            var timeout = TimeSpan.FromMinutes(5);

            // Act
            var exception = new SoraTimeoutException(message, timeout);

            // Assert
            exception.Message.Should().Be(message);
            exception.ErrorCode.Should().Be("TIMEOUT");
            exception.Timeout.Should().Be(timeout);
        }

        [Fact]
        public void SoraValidationException_WithMessage_SetsCorrectDefaults()
        {
            // Arrange
            const string message = "Validation failed";

            // Act
            var exception = new SoraValidationException(message);

            // Assert
            exception.Message.Should().Be(message);
            exception.HttpStatusCode.Should().Be(HttpStatusCode.BadRequest);
            exception.ErrorCode.Should().Be("VALIDATION_FAILED");
            exception.ValidationErrors.Should().BeNull();
        }

        [Fact]
        public void SoraValidationException_WithMessageAndErrors_SetsProperties()
        {
            // Arrange
            const string message = "Validation failed";
            var errors = new Dictionary<string, string[]>
            {
                ["field1"] = new[] { "Error 1", "Error 2" },
                ["field2"] = new[] { "Error 3" }
            };

            // Act
            var exception = new SoraValidationException(message, errors);

            // Assert
            exception.Message.Should().Be(message);
            exception.ValidationErrors.Should().BeEquivalentTo(errors);
        }

        [Fact]
        public void AllExceptions_AreSerializable()
        {
            // Note: Binary serialization is deprecated in .NET 5+, so we're just checking the attribute
            // Arrange
            var exceptionTypes = new[]
            {
                typeof(SoraException),
                typeof(SoraAuthenticationException),
                typeof(SoraNotFoundException),
                typeof(SoraRateLimitException),
                typeof(SoraTimeoutException),
                typeof(SoraValidationException)
            };

            // Act & Assert
            foreach (var type in exceptionTypes)
            {
                type.Should().BeDecoratedWith<SerializableAttribute>(
                    $"{type.Name} should be marked as Serializable");
            }
        }

        [Fact]
        public void SoraException_WithRequestId_SetsProperty()
        {
            // Arrange
            var exception = new SoraException("Error");
            const string requestId = "req-123";

            // Act
            exception.RequestId = requestId;

            // Assert
            exception.RequestId.Should().Be(requestId);
        }

        [Fact]
        public void SoraRateLimitException_WithAdditionalProperties_SetsAll()
        {
            // Arrange
            var exception = new SoraRateLimitException("Rate limited");
            const int remainingRequests = 0;
            var resetTime = DateTimeOffset.UtcNow.AddMinutes(1);

            // Act
            exception.RemainingRequests = remainingRequests;
            exception.ResetTime = resetTime;

            // Assert
            exception.RemainingRequests.Should().Be(remainingRequests);
            exception.ResetTime.Should().Be(resetTime);
        }
    }
} 