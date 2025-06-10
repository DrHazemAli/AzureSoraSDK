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
            request.NSeconds.Should().Be(0);
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
                NSeconds = 10
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
                NSeconds = 10
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
                NSeconds = 10
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
                NSeconds = 10
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
                NSeconds = 10
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
                NSeconds = duration
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
                NSeconds = 10
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
                NSeconds = 10
            };

            // Act & Assert
            var act = () => request.Validate();
            act.Should().Throw<ValidationException>()
                .WithMessage("*divisible by 8*");
        }

        [Fact]
        public void VideoGenerationRequest_WithMetadata_StoresCorrectly()
        {
            // Arrange
            var metadata = new Dictionary<string, string>
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            };

            var request = new VideoGenerationRequest
            {
                Prompt = "Test",
                Width = 1280,
                Height = 720,
                NSeconds = 10,
                Metadata = metadata
            };

            // Act & Assert
            request.Metadata.Should().NotBeNull();
            request.Metadata.Should().HaveCount(2);
            request.Metadata["key1"].Should().Be("value1");
            request.Metadata["key2"].Should().Be("value2");
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