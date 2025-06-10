using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
                ApiKey = "test-key",
                DeploymentName = "sora",
                ApiVersion = "preview"
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
            act.Should().Throw<ValidationException>()
                .WithMessage("*Endpoint field is required*");
        }

        [Fact]
        public async Task SubmitVideoJobAsync_WithValidRequest_ReturnsJobId()
        {
            // Arrange
            const string expectedJobId = "job-123";
            var responseContent = new { id = expectedJobId, status = "queued", @object = "video.generation.job" };
            
            _mockHttp.When(HttpMethod.Post, "*/v1/video/generations/jobs*")
                .Respond("application/json", JsonSerializer.Serialize(responseContent, _jsonOptions));

            // Act
            var result = await _sut.SubmitVideoJobAsync(
                prompt: "A beautiful sunset",
                width: 1280,
                height: 720,
                nSeconds: 10);

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
                nSeconds: 10);

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
                nSeconds: 10);

            await act.Should().ThrowAsync<SoraValidationException>();
        }

        [Fact]
        public async Task SubmitVideoJobAsync_WithUnauthorizedResponse_ThrowsAuthenticationException()
        {
            // Arrange
            _mockHttp.When(HttpMethod.Post, "*/v1/video/generations/jobs*")
                .Respond(HttpStatusCode.Unauthorized, "application/json", 
                    "{\"error\": {\"message\": \"Invalid API key\"}}");

            // Act & Assert
            var act = async () => await _sut.SubmitVideoJobAsync(
                prompt: "Test",
                width: 1280,
                height: 720,
                nSeconds: 10);

            await act.Should().ThrowAsync<SoraAuthenticationException>()
                .WithMessage("*Authentication failed*");
        }

        [Fact]
        public async Task SubmitVideoJobAsync_WithRateLimitExceeded_ThrowsRateLimitException()
        {
            // Arrange
            _mockHttp.When(HttpMethod.Post, "*/v1/video/generations/jobs*")
                .Respond(HttpStatusCode.TooManyRequests, "application/json", 
                    "{\"error\": {\"message\": \"Rate limit exceeded\"}}");

            // Act & Assert
            var act = async () => await _sut.SubmitVideoJobAsync(
                prompt: "Test",
                width: 1280,
                height: 720,
                nSeconds: 10);

            await act.Should().ThrowAsync<SoraRateLimitException>();
        }

        [Fact]
        public async Task GetJobStatusAsync_WithValidJobId_ReturnsJobDetails()
        {
            // Arrange
            const string jobId = "job-123";
            var responseContent = new
            {
                id = jobId,
                @object = "video.generation.job",
                status = "running",
                created_at = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                finished_at = (long?)null,
                expires_at = (long?)null,
                generations = new object[] { },
                failure_reason = (string?)null,
                model = "sora",
                prompt = "A test prompt",
                n_variants = 1,
                n_seconds = 10,
                width = 1280,
                height = 720
            };

            _mockHttp.When(HttpMethod.Get, $"*/v1/video/generations/jobs/{jobId}*")
                .Respond("application/json", JsonSerializer.Serialize(responseContent, _jsonOptions));

            // Act
            var result = await _sut.GetJobStatusAsync(jobId);

            // Assert
            result.Should().NotBeNull();
            result.JobId.Should().Be(jobId);
            result.Status.Should().Be(JobStatus.Running);
            result.VideoUrl.Should().BeNull();
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
            _mockHttp.When(HttpMethod.Get, $"*/v1/video/generations/jobs/{jobId}*")
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
            const string generationId = "gen-456";
            var pendingResponse = new 
            { 
                id = jobId,
                status = "running",
                generations = new object[] { }
            };
            var successResponse = new 
            { 
                id = jobId,
                status = "succeeded",
                generations = new[] 
                { 
                    new { id = generationId, url = (string?)null } 
                },
                created_at = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                finished_at = DateTimeOffset.UtcNow.AddMinutes(2).ToUnixTimeSeconds()
            };

            var callCount = 0;
            _mockHttp.When(HttpMethod.Get, $"*/v1/video/generations/jobs/{jobId}*")
                .Respond(() =>
                {
                    callCount++;
                    return Task.FromResult(callCount <= 2
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
                        });
                });

            // Act
            var result = await _sut.WaitForCompletionAsync(jobId, pollIntervalSeconds: 1);

            // Assert
            result.Should().NotBeNull();
            result.ToString().Should().Contain($"/openai/v1/video/generations/{generationId}/content/video");
        }

        [Fact]
        public async Task WaitForCompletionAsync_WithFailedJob_ThrowsSoraException()
        {
            // Arrange
            const string jobId = "job-123";
            var failedResponse = new
            {
                id = jobId,
                status = "failed",
                failure_reason = "Video generation failed",
                generations = new object[] { }
            };

            _mockHttp.When(HttpMethod.Get, $"*/v1/video/generations/jobs/{jobId}*")
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

            _mockHttp.When(HttpMethod.Get, $"*/v1/video/generations/jobs/{jobId}*")
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

            _mockHttp.When(HttpMethod.Get, $"*/v1/video/generations/jobs/{jobId}*")
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
                .Respond(req => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(videoContent)
                    {
                        Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("video/mp4") }
                    }
                }));

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
        public void LegacyConstructor_CreatesValidClient()
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

        [Theory]
        [InlineData("16:9", 1920, true, 1920, 1080)]
        [InlineData("16:9", 1080, false, 1920, 1080)]
        [InlineData("4:3", 1600, true, 1600, 1200)]
        [InlineData("1:1", 1080, true, 1080, 1080)]
        [InlineData("9:16", 1080, true, 1080, 1920)]
        [InlineData("21:9", 2048, true, 2048, 880)]
        public void CalculateDimensionsFromAspectRatio_WithValidInput_ReturnsCorrectDimensions(
            string aspectRatio, int targetSize, bool preferWidth, int expectedWidth, int expectedHeight)
        {
            // Act
            var (width, height) = SoraClient.CalculateDimensionsFromAspectRatio(aspectRatio, targetSize, preferWidth);

            // Assert
            width.Should().Be(expectedWidth);
            height.Should().Be(expectedHeight);
            (width % 8).Should().Be(0, "width should be divisible by 8");
            (height % 8).Should().Be(0, "height should be divisible by 8");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void CalculateDimensionsFromAspectRatio_WithEmptyAspectRatio_ThrowsArgumentException(string aspectRatio)
        {
            // Act & Assert
            var act = () => SoraClient.CalculateDimensionsFromAspectRatio(aspectRatio);
            act.Should().Throw<ArgumentException>()
                .WithParameterName("aspectRatio");
        }

        [Theory]
        [InlineData("16-9")]
        [InlineData("16:9:4")]
        [InlineData("16")]
        [InlineData("invalid")]
        public void CalculateDimensionsFromAspectRatio_WithInvalidFormat_ThrowsArgumentException(string aspectRatio)
        {
            // Act & Assert
            var act = () => SoraClient.CalculateDimensionsFromAspectRatio(aspectRatio);
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Invalid aspect ratio format*");
        }

        [Theory]
        [InlineData("16:9", "low", 640, 360)]
        [InlineData("16:9", "medium", 1280, 720)]
        [InlineData("16:9", "high", 1920, 1080)]
        [InlineData("16:9", "ultra", 2048, 1152)]
        [InlineData("4:3", "high", 1600, 1200)]
        [InlineData("1:1", "high", 1080, 1080)]
        [InlineData("9:16", "high", 1080, 1920)]
        public void GetCommonDimensions_WithKnownAspectRatios_ReturnsCorrectDimensions(
            string aspectRatio, string quality, int expectedWidth, int expectedHeight)
        {
            // Act
            var (width, height) = SoraClient.GetCommonDimensions(aspectRatio, quality);

            // Assert
            width.Should().Be(expectedWidth);
            height.Should().Be(expectedHeight);
        }

        [Fact]
        public async Task SubmitVideoJobAsync_WithAspectRatio_CalculatesCorrectDimensions()
        {
            // Arrange
            const string expectedJobId = "job-123";
            var responseContent = new { 
                id = expectedJobId, 
                status = "queued",
                @object = "video.generation.job"
            };
            
            _mockHttp.When(HttpMethod.Post, "*/v1/video/generations/jobs*")
                .WithContent("*\"width\":1920*")
                .WithContent("*\"height\":1080*")
                .Respond("application/json", JsonSerializer.Serialize(responseContent, _jsonOptions));

            // Act
            var result = await _sut.SubmitVideoJobAsync(
                prompt: "A test video",
                aspectRatio: "16:9",
                quality: "high",
                nSeconds: 10);

            // Assert
            result.Should().Be(expectedJobId);
        }
    }
} 