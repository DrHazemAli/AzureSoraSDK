using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AzureSoraSDK.Configuration;
using AzureSoraSDK.Exceptions;
using AzureSoraSDK.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;
using Polly.Extensions.Http;

namespace AzureSoraSDK
{
    /// <summary>
    /// Provides prompt enhancement functionality using Azure OpenAI
    /// </summary>
    public class PromptEnhancer : IPromptEnhancer
    {
        private readonly HttpClient _httpClient;
        private readonly SoraClientOptions _options;
        private readonly ILogger<PromptEnhancer> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

        /// <summary>
        /// Creates a new instance of PromptEnhancer
        /// </summary>
        /// <param name="httpClient">HttpClient instance (should be managed by IHttpClientFactory)</param>
        /// <param name="options">Client configuration options</param>
        /// <param name="logger">Logger instance</param>
        public PromptEnhancer(HttpClient httpClient, SoraClientOptions options, ILogger<PromptEnhancer>? logger = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _options.Validate();
            _logger = logger ?? NullLogger<PromptEnhancer>.Instance;

            // Configure HttpClient if not already configured
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri(_options.Endpoint);
                _httpClient.Timeout = _options.HttpTimeout;
                _httpClient.DefaultRequestHeaders.Add("api-key", _options.ApiKey);
            }

            // Configure JSON options
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            // Configure retry policy
            _retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(
                    _options.MaxRetryAttempts,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) * _options.RetryDelay,
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        var statusCode = outcome.Result?.StatusCode;
                        _logger.LogWarning(
                            "Retry {RetryCount} after {Delay}ms due to {StatusCode}",
                            retryCount, timespan.TotalMilliseconds, statusCode);
                    });
        }

        /// <summary>
        /// Legacy constructor for backward compatibility
        /// </summary>
        public PromptEnhancer(HttpClient httpClient, string deploymentName, string apiVersion = "2024-10-21")
            : this(httpClient, new SoraClientOptions 
            { 
                Endpoint = httpClient.BaseAddress?.ToString() ?? throw new ArgumentException("HttpClient must have BaseAddress set"),
                ApiKey = httpClient.DefaultRequestHeaders.GetValues("api-key").FirstOrDefault() ?? string.Empty,
                DeploymentName = deploymentName,
                ApiVersion = apiVersion
            })
        {
        }

        /// <inheritdoc/>
        public async Task<string[]> SuggestPromptsAsync(
            string partialPrompt,
            int maxSuggestions = 3,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(partialPrompt))
            {
                _logger.LogDebug("Empty prompt provided, returning empty suggestions");
                return Array.Empty<string>();
            }

            if (maxSuggestions < 1 || maxSuggestions > 10)
            {
                throw new ArgumentException("maxSuggestions must be between 1 and 10", nameof(maxSuggestions));
            }

            _logger.LogInformation(
                "Generating {MaxSuggestions} prompt suggestions for: {PromptLength} chars",
                maxSuggestions, partialPrompt.Length);

            var systemPrompt = @"You are an AI assistant specialized in enhancing video generation prompts. 
Your task is to improve prompts by adding specific details about:
- Visual elements and composition
- Lighting and atmosphere
- Movement and dynamics
- Style and artistic direction
- Technical specifications

Provide clear, concise suggestions that maintain the original intent while adding helpful details.";

            var userPrompt = $@"Enhance the following video generation prompt by providing {maxSuggestions} improved versions.
Each suggestion should be on a new line and be complete, self-contained, and more detailed than the original.

Original prompt: ""{partialPrompt}""

Enhanced prompts:";

            var request = new CompletionRequest
            {
                Model = _options.DeploymentName,
                Messages = new[]
                {
                    new Message { Role = "system", Content = systemPrompt },
                    new Message { Role = "user", Content = userPrompt }
                },
                MaxTokens = Math.Min(150 * maxSuggestions, 1000),
                Temperature = 0.7,
                TopP = 0.9,
                N = 1,
                Stop = new[] { "\n\n" }
            };

            var url = $"/openai/deployments/{_options.DeploymentName}/chat/completions?api-version={_options.ApiVersion}";

            using var content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            try
            {
                var response = await _retryPolicy.ExecuteAsync(
                    async () => await _httpClient.PostAsync(url, content, cancellationToken));

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError(
                        "Prompt enhancement failed with status {StatusCode}: {Error}",
                        response.StatusCode, error);

                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new SoraAuthenticationException("Authentication failed for prompt enhancement");
                    }

                    throw new SoraException(
                        $"Prompt enhancement failed: {error}",
                        response.StatusCode);
                }

                var completionResponse = await JsonSerializer.DeserializeAsync<CompletionResponse>(
                    await response.Content.ReadAsStreamAsync(cancellationToken),
                    _jsonOptions,
                    cancellationToken);

                if (completionResponse?.Choices == null || completionResponse.Choices.Length == 0)
                {
                    _logger.LogWarning("No suggestions returned from API");
                    return Array.Empty<string>();
                }

                var suggestions = ParseSuggestions(
                    completionResponse.Choices[0].Message?.Content ?? string.Empty,
                    maxSuggestions);

                _logger.LogInformation(
                    "Generated {Count} prompt suggestions successfully",
                    suggestions.Length);

                return suggestions;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error during prompt enhancement");
                throw new SoraException("Network error during prompt enhancement", ex);
            }
            catch (TaskCanceledException ex)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException("Prompt enhancement was cancelled", ex, cancellationToken);
                }
                throw new SoraTimeoutException("Prompt enhancement timed out", _options.HttpTimeout);
            }
            catch (Exception ex) when (!(ex is SoraException))
            {
                _logger.LogError(ex, "Unexpected error during prompt enhancement");
                throw new SoraException("Prompt enhancement failed", ex);
            }
        }

        /// <summary>
        /// Parses suggestions from the completion response
        /// </summary>
        private string[] ParseSuggestions(string text, int maxSuggestions)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<string>();

            var lines = text
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line =>
                {
                    // Remove common prefixes like "1.", "2.", "-", "*", etc.
                    var cleaned = System.Text.RegularExpressions.Regex
                        .Replace(line, @"^(\d+\.|\-|\*|\•)\s*", "");
                    return cleaned.Trim();
                })
                .Where(line => line.Length > 10) // Filter out too short suggestions
                .Take(maxSuggestions)
                .ToArray();

            return lines;
        }

        // Request/Response models
        private class CompletionRequest
        {
            [JsonPropertyName("model")]
            public string Model { get; set; } = string.Empty;

            [JsonPropertyName("messages")]
            public Message[] Messages { get; set; } = Array.Empty<Message>();

            [JsonPropertyName("max_tokens")]
            public int MaxTokens { get; set; }

            [JsonPropertyName("temperature")]
            public double Temperature { get; set; }

            [JsonPropertyName("top_p")]
            public double TopP { get; set; }

            [JsonPropertyName("n")]
            public int N { get; set; }

            [JsonPropertyName("stop")]
            public string[]? Stop { get; set; }
        }

        private class Message
        {
            [JsonPropertyName("role")]
            public string Role { get; set; } = string.Empty;

            [JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;
        }

        private class CompletionResponse
        {
            [JsonPropertyName("choices")]
            public Choice[]? Choices { get; set; }

            [JsonPropertyName("usage")]
            public Usage? Usage { get; set; }
        }

        private class Choice
        {
            [JsonPropertyName("message")]
            public Message? Message { get; set; }

            [JsonPropertyName("finish_reason")]
            public string? FinishReason { get; set; }

            [JsonPropertyName("index")]
            public int Index { get; set; }
        }

        private class Usage
        {
            [JsonPropertyName("prompt_tokens")]
            public int PromptTokens { get; set; }

            [JsonPropertyName("completion_tokens")]
            public int CompletionTokens { get; set; }

            [JsonPropertyName("total_tokens")]
            public int TotalTokens { get; set; }
        }
    }
} 