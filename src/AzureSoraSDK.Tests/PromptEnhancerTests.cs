using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AzureSoraSDK;
using AzureSoraSDK.Configuration;
using AzureSoraSDK.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;
using Xunit;

namespace AzureSoraSDK.Tests
{
    public class PromptEnhancerTests : IDisposable
    {
        private readonly MockHttpMessageHandler _mockHttp;
        private readonly HttpClient _httpClient;
        private readonly SoraClientOptions _options;
        private readonly Mock<ILogger<PromptEnhancer>> _mockLogger;
        private readonly PromptEnhancer _sut;
        private readonly JsonSerializerOptions _jsonOptions;

        public PromptEnhancerTests()
        {
            _mockHttp = new MockHttpMessageHandler();
            _httpClient = new HttpClient(_mockHttp) { BaseAddress = new Uri("https://test.openai.azure.com") };
            _httpClient.DefaultRequestHeaders.Add("api-key", "test-api-key");
            
            _options = new SoraClientOptions
            {
                Endpoint = "https://test.openai.azure.com",
                ApiKey = "test-api-key",
                DeploymentName = "test-deployment",
                ApiVersion = "2024-10-21"
            };
            
            _mockLogger = new Mock<ILogger<PromptEnhancer>>();
            _sut = new PromptEnhancer(_httpClient, _options, _mockLogger.Object);
            _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            _mockHttp?.Dispose();
        }

        [Fact]
        public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
        {
            // Act & Assert
            var act = () => new PromptEnhancer(null!, _options);
            act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
        }

        [Fact]
        public void Constructor_WithNullOptions_ThrowsArgumentNullException()
        {
            // Act & Assert
            var act = () => new PromptEnhancer(_httpClient, (SoraClientOptions)null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("options");
        }

        [Fact]
        public async Task SuggestPromptsAsync_WithEmptyPrompt_ReturnsEmptyArray()
        {
            // Act
            var result = await _sut.SuggestPromptsAsync("");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task SuggestPromptsAsync_WithWhitespacePrompt_ReturnsEmptyArray()
        {
            // Act
            var result = await _sut.SuggestPromptsAsync("   ");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task SuggestPromptsAsync_WithInvalidMaxSuggestions_ThrowsArgumentException()
        {
            // Act & Assert
            var act = async () => await _sut.SuggestPromptsAsync("test", maxSuggestions: 0);
            await act.Should().ThrowAsync<ArgumentException>()
                .WithParameterName("maxSuggestions");

            act = async () => await _sut.SuggestPromptsAsync("test", maxSuggestions: 11);
            await act.Should().ThrowAsync<ArgumentException>()
                .WithParameterName("maxSuggestions");
        }

        [Fact]
        public async Task SuggestPromptsAsync_WithValidPrompt_ReturnsSuggestions()
        {
            // Arrange
            const string prompt = "A sunset";
            var responseContent = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            content = @"1. A vibrant sunset over the ocean with golden rays reflecting on calm waters
2. A dramatic sunset behind mountain silhouettes with purple and orange clouds
3. A peaceful sunset in a meadow with warm light casting long shadows"
                        }
                    }
                }
            };

            _mockHttp.When(HttpMethod.Post, "*/chat/completions*")
                .Respond("application/json", JsonSerializer.Serialize(responseContent, _jsonOptions));

            // Act
            var result = await _sut.SuggestPromptsAsync(prompt, maxSuggestions: 3);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result[0].Should().Contain("sunset");
            result.All(s => s.Length > 10).Should().BeTrue();
        }

        [Fact]
        public async Task SuggestPromptsAsync_ParsesSuggestionsWithVariousFormats()
        {
            // Arrange
            const string prompt = "A forest";
            var responseContent = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            content = @"- A mystical forest with fog and ancient trees
* A dense rainforest with exotic wildlife
• A autumn forest with colorful falling leaves
A spring forest with blooming wildflowers
1. A dark forest at night with moonlight
2) A magical forest with glowing mushrooms"
                        }
                    }
                }
            };

            _mockHttp.When(HttpMethod.Post, "*/chat/completions*")
                .Respond("application/json", JsonSerializer.Serialize(responseContent, _jsonOptions));

            // Act
            var result = await _sut.SuggestPromptsAsync(prompt, maxSuggestions: 10);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(6);
            result.All(s => !s.StartsWith("-") && !s.StartsWith("*") && !s.StartsWith("•")).Should().BeTrue();
            result.All(s => s.Contains("forest")).Should().BeTrue();
        }

        [Fact]
        public async Task SuggestPromptsAsync_WithUnauthorizedResponse_ThrowsAuthenticationException()
        {
            // Arrange
            _mockHttp.When(HttpMethod.Post, "*/chat/completions*")
                .Respond(HttpStatusCode.Unauthorized, "application/json", 
                    "{\"error\": {\"message\": \"Invalid API key\"}}");

            // Act & Assert
            var act = async () => await _sut.SuggestPromptsAsync("test");
            await act.Should().ThrowAsync<SoraAuthenticationException>();
        }

        [Fact]
        public async Task SuggestPromptsAsync_WithHttpError_ThrowsSoraException()
        {
            // Arrange
            _mockHttp.When(HttpMethod.Post, "*/chat/completions*")
                .Respond(HttpStatusCode.InternalServerError, "application/json", 
                    "{\"error\": {\"message\": \"Internal server error\"}}");

            // Act & Assert
            var act = async () => await _sut.SuggestPromptsAsync("test");
            await act.Should().ThrowAsync<SoraException>()
                .WithMessage("*Prompt enhancement failed*");
        }

        [Fact]
        public async Task SuggestPromptsAsync_WithEmptyResponse_ReturnsEmptyArray()
        {
            // Arrange
            var responseContent = new
            {
                choices = Array.Empty<object>()
            };

            _mockHttp.When(HttpMethod.Post, "*/chat/completions*")
                .Respond("application/json", JsonSerializer.Serialize(responseContent, _jsonOptions));

            // Act
            var result = await _sut.SuggestPromptsAsync("test");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task SuggestPromptsAsync_WithNullContent_ReturnsEmptyArray()
        {
            // Arrange
            var responseContent = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            content = (string?)null
                        }
                    }
                }
            };

            _mockHttp.When(HttpMethod.Post, "*/chat/completions*")
                .Respond("application/json", JsonSerializer.Serialize(responseContent, _jsonOptions));

            // Act
            var result = await _sut.SuggestPromptsAsync("test");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task SuggestPromptsAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            _mockHttp.When(HttpMethod.Post, "*/chat/completions*")
                .Respond(async () =>
                {
                    await Task.Delay(1000);
                    return new HttpResponseMessage(HttpStatusCode.OK);
                });

            // Act & Assert
            var act = async () => await _sut.SuggestPromptsAsync("test", cancellationToken: cts.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task SuggestPromptsAsync_FiltersShortSuggestions()
        {
            // Arrange
            var responseContent = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            content = @"1. A beautiful and serene mountain landscape at dawn
2. Short
3. Another wonderful scene with detailed description
4. Too brief
5. ""
6. A majestic waterfall in a tropical rainforest"
                        }
                    }
                }
            };

            _mockHttp.When(HttpMethod.Post, "*/chat/completions*")
                .Respond("application/json", JsonSerializer.Serialize(responseContent, _jsonOptions));

            // Act
            var result = await _sut.SuggestPromptsAsync("nature", maxSuggestions: 5);

            // Assert
            result.Should().HaveCount(3);
            result.All(s => s.Length > 10).Should().BeTrue();
        }

        [Fact]
        public void LegacyConstructor_WithValidHttpClient_CreatesEnhancer()
        {
            // Arrange
            var httpClient = new HttpClient { BaseAddress = new Uri("https://test.openai.azure.com") };
            httpClient.DefaultRequestHeaders.Add("api-key", "test-key");

            // Act & Assert
            var act = () => new PromptEnhancer(httpClient, "test-deployment");
            act.Should().NotThrow();
        }

        [Fact]
        public void LegacyConstructor_WithoutBaseAddress_ThrowsArgumentException()
        {
            // Arrange
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("api-key", "test-key");

            // Act & Assert
            var act = () => new PromptEnhancer(httpClient, "test-deployment");
            act.Should().Throw<ArgumentException>();
        }
    }
} 