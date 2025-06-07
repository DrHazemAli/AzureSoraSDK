using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AzureSoraSDK.Models;
using FluentAssertions;
using Xunit;

namespace AzureSoraSDK.Tests
{
    public class ModelsTests
    {
        [Fact]
        public void VideoGenerationRequest_DefaultValues_AreCorrect()
        {
            // Act
            var request = new VideoGenerationRequest();

            // Assert
            request.Prompt.Should().BeEmpty();
            request.Width.Should().Be(0);
            request.Height.Should().Be(0);
            request.DurationInSeconds.Should().Be(0);
            request.AspectRatio.Should().BeNull();
            request.FrameRate.Should().BeNull();
            request.Seed.Should().BeNull();
            request.Quality.Should().BeNull();
            request.Style.Should().BeNull();
            request.Metadata.Should().BeNull();
        }

        [Fact]
        public void VideoGenerationRequest_Validate_WithValidRequest_DoesNotThrow()
        {
            // Arrange
            var request = new VideoGenerationRequest
            {
                Prompt = "A beautiful sunset",
                Width = 1280,
                Height = 720,
                DurationInSeconds = 10,
                AspectRatio = "16:9",
                FrameRate = 30
            };

            // Act & Assert
            var act = () => request.Validate();
            act.Should().NotThrow();
        }

        [Fact]
        public void VideoGenerationRequest_Validate_WithEmptyPrompt_ThrowsValidationException()
        {
            // Arrange
            var request = new VideoGenerationRequest
            {
                Prompt = "",
                Width = 1280,
                Height = 720,
                DurationInSeconds = 10
            };

            // Act & Assert
            var act = () => request.Validate();
            act.Should().Throw<ValidationException>();
        }

        [Fact]
        public void VideoGenerationRequest_Validate_WithTooLongPrompt_ThrowsValidationException()
        {
            // Arrange
            var request = new VideoGenerationRequest
            {
                Prompt = new string('a', 4001), // Over 4000 chars
                Width = 1280,
                Height = 720,
                DurationInSeconds = 10
            };

            // Act & Assert
            var act = () => request.Validate();
            act.Should().Throw<ValidationException>();
        }

        [Theory]
        [InlineData(127)] // Below minimum
        [InlineData(2049)] // Above maximum
        public void VideoGenerationRequest_Validate_WithInvalidWidth_ThrowsValidationException(int width)
        {
            // Arrange
            var request = new VideoGenerationRequest
            {
                Prompt = "Test",
                Width = width,
                Height = 720,
                DurationInSeconds = 10
            };

            // Act & Assert
            var act = () => request.Validate();
            act.Should().Throw<ValidationException>();
        }

        [Theory]
        [InlineData(127)] // Below minimum
        [InlineData(2049)] // Above maximum
        public void VideoGenerationRequest_Validate_WithInvalidHeight_ThrowsValidationException(int height)
        {
            // Arrange
            var request = new VideoGenerationRequest
            {
                Prompt = "Test",
                Width = 1280,
                Height = height,
                DurationInSeconds = 10
            };

            // Act & Assert
            var act = () => request.Validate();
            act.Should().Throw<ValidationException>();
        }

        [Theory]
        [InlineData(0)] // Below minimum
        [InlineData(61)] // Above maximum
        public void VideoGenerationRequest_Validate_WithInvalidDuration_ThrowsValidationException(int duration)
        {
            // Arrange
            var request = new VideoGenerationRequest
            {
                Prompt = "Test",
                Width = 1280,
                Height = 720,
                DurationInSeconds = duration
            };

            // Act & Assert
            var act = () => request.Validate();
            act.Should().Throw<ValidationException>();
        }

        [Theory]
        [InlineData(640, 480)] // Valid dimensions divisible by 8
        [InlineData(1920, 1080)] // Valid HD dimensions
        public void VideoGenerationRequest_Validate_WithValidDimensionsDivisibleBy8_DoesNotThrow(int width, int height)
        {
            // Arrange
            var request = new VideoGenerationRequest
            {
                Prompt = "Test",
                Width = width,
                Height = height,
                DurationInSeconds = 10
            };

            // Act & Assert
            var act = () => request.Validate();
            act.Should().NotThrow();
        }

        [Theory]
        [InlineData(641, 480)] // Width not divisible by 8
        [InlineData(640, 481)] // Height not divisible by 8
        [InlineData(645, 485)] // Both not divisible by 8
        public void VideoGenerationRequest_Validate_WithDimensionsNotDivisibleBy8_ThrowsValidationException(int width, int height)
        {
            // Arrange
            var request = new VideoGenerationRequest
            {
                Prompt = "Test",
                Width = width,
                Height = height,
                DurationInSeconds = 10
            };

            // Act & Assert
            var act = () => request.Validate();
            act.Should().Throw<ValidationException>()
                .WithMessage("*divisible by 8*");
        }

        [Theory]
        [InlineData("16:9")]
        [InlineData("4:3")]
        [InlineData("1:1")]
        [InlineData("21:9")]
        public void VideoGenerationRequest_Validate_WithValidAspectRatio_DoesNotThrow(string aspectRatio)
        {
            // Arrange
            var request = new VideoGenerationRequest
            {
                Prompt = "Test",
                Width = 1280,
                Height = 720,
                DurationInSeconds = 10,
                AspectRatio = aspectRatio
            };

            // Act & Assert
            var act = () => request.Validate();
            act.Should().NotThrow();
        }

        [Theory]
        [InlineData("16-9")] // Wrong separator
        [InlineData("16:9:4")] // Too many parts
        [InlineData("16")] // Missing part
        [InlineData("a:b")] // Non-numeric
        public void VideoGenerationRequest_Validate_WithInvalidAspectRatio_ThrowsValidationException(string aspectRatio)
        {
            // Arrange
            var request = new VideoGenerationRequest
            {
                Prompt = "Test",
                Width = 1280,
                Height = 720,
                DurationInSeconds = 10,
                AspectRatio = aspectRatio
            };

            // Act & Assert
            var act = () => request.Validate();
            act.Should().Throw<ValidationException>();
        }

        [Theory]
        [InlineData(11)] // Below minimum
        [InlineData(61)] // Above maximum
        public void VideoGenerationRequest_Validate_WithInvalidFrameRate_ThrowsValidationException(int frameRate)
        {
            // Arrange
            var request = new VideoGenerationRequest
            {
                Prompt = "Test",
                Width = 1280,
                Height = 720,
                DurationInSeconds = 10,
                FrameRate = frameRate
            };

            // Act & Assert
            var act = () => request.Validate();
            act.Should().Throw<ValidationException>();
        }

        [Theory]
        [InlineData("standard")]
        [InlineData("high")]
        [InlineData("ultra")]
        [InlineData("Standard")] // Case insensitive
        [InlineData("HIGH")]
        [InlineData("Ultra")]
        public void VideoGenerationRequest_Validate_WithValidQuality_DoesNotThrow(string quality)
        {
            // Arrange
            var request = new VideoGenerationRequest
            {
                Prompt = "Test",
                Width = 1280,
                Height = 720,
                DurationInSeconds = 10,
                Quality = quality
            };

            // Act & Assert
            var act = () => request.Validate();
            act.Should().NotThrow();
        }

        [Theory]
        [InlineData("low")]
        [InlineData("medium")]
        [InlineData("super")]
        [InlineData("4k")]
        public void VideoGenerationRequest_Validate_WithInvalidQuality_ThrowsValidationException(string quality)
        {
            // Arrange
            var request = new VideoGenerationRequest
            {
                Prompt = "Test",
                Width = 1280,
                Height = 720,
                DurationInSeconds = 10,
                Quality = quality
            };

            // Act & Assert
            var act = () => request.Validate();
            act.Should().Throw<ValidationException>()
                .WithMessage("*Quality must be one of:*");
        }

        [Fact]
        public void VideoGenerationRequest_Validate_WithNegativeSeed_ThrowsValidationException()
        {
            // Arrange
            var request = new VideoGenerationRequest
            {
                Prompt = "Test",
                Width = 1280,
                Height = 720,
                DurationInSeconds = 10,
                Seed = -1
            };

            // Act & Assert
            var act = () => request.Validate();
            act.Should().Throw<ValidationException>();
        }

        [Fact]
        public void JobDetails_DefaultValues_AreCorrect()
        {
            // Act
            var details = new JobDetails();

            // Assert
            details.JobId.Should().BeEmpty();
            details.Status.Should().Be(JobStatus.Unknown);
            details.VideoUrl.Should().BeNull();
            details.ErrorMessage.Should().BeNull();
            details.ErrorCode.Should().BeNull();
            details.CreatedAt.Should().BeNull();
            details.UpdatedAt.Should().BeNull();
            details.CompletedAt.Should().BeNull();
            details.ProgressPercentage.Should().BeNull();
            details.EstimatedTimeRemaining.Should().BeNull();
            details.Metadata.Should().BeNull();
        }

        [Fact]
        public void JobDetails_CanSetAllProperties()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;
            var metadata = new Dictionary<string, object> { ["key"] = "value" };

            // Act
            var details = new JobDetails
            {
                JobId = "job-123",
                Status = JobStatus.Running,
                VideoUrl = "https://example.com/video.mp4",
                ErrorMessage = "Error occurred",
                ErrorCode = "ERR_001",
                CreatedAt = now,
                UpdatedAt = now.AddMinutes(1),
                CompletedAt = now.AddMinutes(5),
                ProgressPercentage = 75,
                EstimatedTimeRemaining = TimeSpan.FromMinutes(2),
                Metadata = metadata
            };

            // Assert
            details.JobId.Should().Be("job-123");
            details.Status.Should().Be(JobStatus.Running);
            details.VideoUrl.Should().Be("https://example.com/video.mp4");
            details.ErrorMessage.Should().Be("Error occurred");
            details.ErrorCode.Should().Be("ERR_001");
            details.CreatedAt.Should().Be(now);
            details.UpdatedAt.Should().Be(now.AddMinutes(1));
            details.CompletedAt.Should().Be(now.AddMinutes(5));
            details.ProgressPercentage.Should().Be(75);
            details.EstimatedTimeRemaining.Should().Be(TimeSpan.FromMinutes(2));
            details.Metadata.Should().BeSameAs(metadata);
        }

        [Theory]
        [InlineData(JobStatus.Unknown, 0)]
        [InlineData(JobStatus.Running, 1)]
        [InlineData(JobStatus.Succeeded, 2)]
        [InlineData(JobStatus.Failed, 3)]
        [InlineData(JobStatus.Cancelled, 4)]
        [InlineData(JobStatus.Pending, 5)]
        public void JobStatus_HasCorrectValues(JobStatus status, int expectedValue)
        {
            // Assert
            ((int)status).Should().Be(expectedValue);
        }
    }
} 