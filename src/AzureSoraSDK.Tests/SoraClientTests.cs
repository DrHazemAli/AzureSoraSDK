using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AzureSoraSDK;
using AzureSoraSDK.Configuration;
using AzureSoraSDK.Exceptions;
using AzureSoraSDK.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;
using Xunit;

namespace AzureSoraSDK.Tests
{
    public class SoraClientTests : IDisposable
    {
        private readonly MockHttpMessageHandler _mockHttp;
        private readonly HttpClient _httpClient;
        private readonly SoraClientOptions _options;
        private readonly Mock<ILogger<SoraClient>> _mockLogger;
        private readonly SoraClient _sut;
        private readonly JsonSerializerOptions _jsonOptions;

        public SoraClientTests()
        {
            _mockHttp = new MockHttpMessageHandler();
            _httpClient = new HttpClient(_mockHttp) { BaseAddress = new Uri("https://test.openai.azure.com") };
            _options = new SoraClientOptions
            {
                Endpoint = "https://test.openai.azure.com",
                ApiKey = "test-api-key",
                DeploymentName = "test-deployment",
                ApiVersion = "2024-10-21"
            };
            _mockLogger = new Mock<ILogger<SoraClient>>();
            _sut = new SoraClient(_httpClient, _options, _mockLogger.Object);
            _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        }

        public void Dispose()
        {
            _sut?.Dispose();
            _httpClient?.Dispose();
            _mockHttp?.Dispose();
        }

        [Fact]
        public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
        {
            // Act & Assert
            var act = () => new SoraClient(null!, _options);
            act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
        }

        [Fact]
        public void Constructor_WithNullOptions_ThrowsArgumentNullException()
        {
            // Act & Assert
            var act = () => new SoraClient(_httpClient, null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("options");
        }

        [Fact]
        public void Constructor_WithInvalidOptions_ThrowsException()
        {
            // Arrange
            var invalidOptions = new SoraClientOptions
            {
                Endpoint = "", // Invalid
                ApiKey = "test",
                DeploymentName = "test"
            };

            // Act & Assert
            var act = () => new SoraClient(_httpClient, invalidOptions);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public async Task SubmitVideoJobAsync_WithValidRequest_ReturnsJobId()
        {
            // Arrange
            const string expectedJobId = "job-123";
            var responseContent = new { jobId = expectedJobId };
            
            _mockHttp.When(HttpMethod.Post, "*/video/jobs*")
                .Respond("application/json", JsonSerializer.Serialize(responseContent, _jsonOptions));

            // Act
            var result = await _sut.SubmitVideoJobAsync(
                prompt: "A beautiful sunset",
                width: 1280,
                height: 720,
                durationInSeconds: 10);

            // Assert
            result.Should().Be(expectedJobId);
        }

        [Fact]
        public async Task SubmitVideoJobAsync_WithInvalidDimensions_ThrowsValidationException()
        {
            // Act & Assert
            var act = async () => await _sut.SubmitVideoJobAsync(
                prompt: "Test",
                width: 127, // Invalid - not divisible by 8
                height: 720,
                durationInSeconds: 10);

            await act.Should().ThrowAsync<SoraValidationException>();
        }

        [Fact]
        public async Task SubmitVideoJobAsync_WithEmptyPrompt_ThrowsValidationException()
        {
            // Act & Assert
            var act = async () => await _sut.SubmitVideoJobAsync(
                prompt: "",
                width: 1280,
                height: 720,
                durationInSeconds: 10);

            await act.Should().ThrowAsync<SoraValidationException>();
        }

        [Fact]
        public async Task SubmitVideoJobAsync_WithUnauthorizedResponse_ThrowsAuthenticationException()
        {
            // Arrange
            _mockHttp.When(HttpMethod.Post, "*/video/jobs*")
                .Respond(HttpStatusCode.Unauthorized, "application/json", 
                    "{\"error\": {\"message\": \"Invalid API key\"}}");

            // Act & Assert
            var act = async () => await _sut.SubmitVideoJobAsync(
                prompt: "Test",
                width: 1280,
                height: 720,
                durationInSeconds: 10);

            await act.Should().ThrowAsync<SoraAuthenticationException>()
                .WithMessage("*Authentication failed*");
        }

        [Fact]
        public async Task SubmitVideoJobAsync_WithRateLimitExceeded_ThrowsRateLimitException()
        {
            // Arrange
            _mockHttp.When(HttpMethod.Post, "*/video/jobs*")
                .Respond(HttpStatusCode.TooManyRequests, "application/json", 
                    "{\"error\": {\"message\": \"Rate limit exceeded\"}}");

            // Act & Assert
            var act = async () => await _sut.SubmitVideoJobAsync(
                prompt: "Test",
                width: 1280,
                height: 720,
                durationInSeconds: 10);

            await act.Should().ThrowAsync<SoraRateLimitException>();
        }

        [Fact]
        public async Task GetJobStatusAsync_WithValidJobId_ReturnsJobDetails()
        {
            // Arrange
            const string jobId = "job-123";
            var responseContent = new
            {
                status = "running",
                videoUrl = (string?)null,
                progressPercentage = 50,
                createdAt = DateTimeOffset.UtcNow
            };

            _mockHttp.When(HttpMethod.Get, $"*/video/jobs/{jobId}*")
                .Respond("application/json", JsonSerializer.Serialize(responseContent, _jsonOptions));

            // Act
            var result = await _sut.GetJobStatusAsync(jobId);

            // Assert
            result.Should().NotBeNull();
            result.JobId.Should().Be(jobId);
            result.Status.Should().Be(JobStatus.Running);
            result.ProgressPercentage.Should().Be(50);
        }

        [Fact]
        public async Task GetJobStatusAsync_WithEmptyJobId_ThrowsArgumentException()
        {
            // Act & Assert
            var act = async () => await _sut.GetJobStatusAsync("");
            await act.Should().ThrowAsync<ArgumentException>()
                .WithParameterName("jobId");
        }

        [Fact]
        public async Task GetJobStatusAsync_WithNotFoundResponse_ThrowsNotFoundException()
        {
            // Arrange
            const string jobId = "job-notfound";
            _mockHttp.When(HttpMethod.Get, $"*/video/jobs/{jobId}*")
                .Respond(HttpStatusCode.NotFound);

            // Act & Assert
            var act = async () => await _sut.GetJobStatusAsync(jobId);
            await act.Should().ThrowAsync<SoraNotFoundException>()
                .Where(ex => ex.ResourceId == jobId);
        }

        [Fact]
        public async Task WaitForCompletionAsync_WithSuccessfulJob_ReturnsVideoUri()
        {
            // Arrange
            const string jobId = "job-123";
            const string videoUrl = "https://example.com/video.mp4";
            var pendingResponse = new { status = "running" };
            var successResponse = new { status = "succeeded", videoUrl };

            var callCount = 0;
            _mockHttp.When(HttpMethod.Get, $"*/video/jobs/{jobId}*")
                .Respond(() =>
                {
                    callCount++;
                    return callCount <= 2
                        ? new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                JsonSerializer.Serialize(pendingResponse, _jsonOptions),
                                Encoding.UTF8,
                                "application/json")
                        }
                        : new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                JsonSerializer.Serialize(successResponse, _jsonOptions),
                                Encoding.UTF8,
                                "application/json")
                        };
                });

            // Act
            var result = await _sut.WaitForCompletionAsync(jobId, pollIntervalSeconds: 1);

            // Assert
            result.Should().NotBeNull();
            result.ToString().Should().Be(videoUrl);
        }

        [Fact]
        public async Task WaitForCompletionAsync_WithFailedJob_ThrowsSoraException()
        {
            // Arrange
            const string jobId = "job-123";
            var failedResponse = new
            {
                status = "failed",
                error = new { message = "Video generation failed", code = "VID_FAIL" }
            };

            _mockHttp.When(HttpMethod.Get, $"*/video/jobs/{jobId}*")
                .Respond("application/json", JsonSerializer.Serialize(failedResponse, _jsonOptions));

            // Act & Assert
            var act = async () => await _sut.WaitForCompletionAsync(jobId);
            await act.Should().ThrowAsync<SoraException>()
                .WithMessage("*Video generation failed*");
        }

        [Fact]
        public async Task WaitForCompletionAsync_WithTimeout_ThrowsTimeoutException()
        {
            // Arrange
            const string jobId = "job-123";
            var runningResponse = new { status = "running" };

            _mockHttp.When(HttpMethod.Get, $"*/video/jobs/{jobId}*")
                .Respond("application/json", JsonSerializer.Serialize(runningResponse, _jsonOptions));

            // Act & Assert
            var act = async () => await _sut.WaitForCompletionAsync(
                jobId,
                pollIntervalSeconds: 1,
                maxWaitTime: TimeSpan.FromSeconds(2));

            await act.Should().ThrowAsync<SoraTimeoutException>();
        }

        [Fact]
        public async Task WaitForCompletionAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            const string jobId = "job-123";
            var runningResponse = new { status = "running" };
            var cts = new CancellationTokenSource();

            _mockHttp.When(HttpMethod.Get, $"*/video/jobs/{jobId}*")
                .Respond("application/json", JsonSerializer.Serialize(runningResponse, _jsonOptions));

            // Act & Assert
            cts.CancelAfter(100);
            var act = async () => await _sut.WaitForCompletionAsync(
                jobId,
                pollIntervalSeconds: 1,
                cancellationToken: cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task DownloadVideoAsync_WithValidUri_CreatesFile()
        {
            // Arrange
            var videoUri = new Uri("https://example.com/video.mp4");
            var videoContent = Encoding.UTF8.GetBytes("fake video content");
            var tempFile = System.IO.Path.GetTempFileName();

            _mockHttp.When(HttpMethod.Get, videoUri.ToString())
                .Respond("video/mp4", new ByteArrayContent(videoContent));

            try
            {
                // Act
                await _sut.DownloadVideoAsync(videoUri, tempFile);

                // Assert
                System.IO.File.Exists(tempFile).Should().BeTrue();
                var fileContent = await System.IO.File.ReadAllBytesAsync(tempFile);
                fileContent.Should().BeEquivalentTo(videoContent);
            }
            finally
            {
                // Cleanup
                if (System.IO.File.Exists(tempFile))
                    System.IO.File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task DownloadVideoAsync_WithNullUri_ThrowsArgumentNullException()
        {
            // Act & Assert
            var act = async () => await _sut.DownloadVideoAsync(null!, "test.mp4");
            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("videoUri");
        }

        [Fact]
        public async Task DownloadVideoAsync_WithEmptyFilePath_ThrowsArgumentException()
        {
            // Arrange
            var videoUri = new Uri("https://example.com/video.mp4");

            // Act & Assert
            var act = async () => await _sut.DownloadVideoAsync(videoUri, "");
            await act.Should().ThrowAsync<ArgumentException>()
                .WithParameterName("filePath");
        }

        [Fact]
        public async Task DownloadVideoAsync_WithHttpError_ThrowsSoraException()
        {
            // Arrange
            var videoUri = new Uri("https://example.com/video.mp4");
            
            _mockHttp.When(HttpMethod.Get, videoUri.ToString())
                .Respond(HttpStatusCode.NotFound);

            // Act & Assert
            var act = async () => await _sut.DownloadVideoAsync(videoUri, "test.mp4");
            await act.Should().ThrowAsync<SoraNotFoundException>();
        }

        [Fact]
        public async Task LegacyConstructor_CreatesValidClient()
        {
            // Arrange & Act
            using var client = new SoraClient(
                "https://test.openai.azure.com",
                "test-api-key",
                "test-deployment");

            // Assert
            client.Should().NotBeNull();
            // Verify it doesn't throw when used
            var act = () => client.Dispose();
            act.Should().NotThrow();
        }

        [Fact]
        public async Task DisposeAsync_DisposesResourcesCorrectly()
        {
            // Arrange
            var client = new SoraClient(_httpClient, _options);

            // Act
            await client.DisposeAsync();
            
            // Assert - should not throw when disposing again
            var act = () => client.Dispose();
            act.Should().NotThrow();
        }
    }
} 