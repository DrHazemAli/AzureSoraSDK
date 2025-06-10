using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AzureSoraSDK.Configuration;
using AzureSoraSDK.Exceptions;
using AzureSoraSDK.Interfaces;
using AzureSoraSDK.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;
using Polly.Extensions.Http;

namespace AzureSoraSDK
{
    /// <summary>
    /// Client for interacting with Azure OpenAI Sora video generation service
    /// </summary>
    public class SoraClient : ISoraClient
    {
        private readonly HttpClient _httpClient;
        private readonly SoraClientOptions _options;
        private readonly ILogger<SoraClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;

        /// <summary>
        /// Creates a new instance of SoraClient
        /// </summary>
        /// <param name="httpClient">HttpClient instance (should be managed by IHttpClientFactory)</param>
        /// <param name="options">Client configuration options</param>
        /// <param name="logger">Logger instance</param>
        public SoraClient(HttpClient httpClient, SoraClientOptions options, ILogger<SoraClient>? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _options.Validate();
            
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? NullLogger<SoraClient>.Instance;
            _semaphore = new SemaphoreSlim(1, 1);

            // Configure HttpClient
            _httpClient.BaseAddress = new Uri(_options.Endpoint);
            _httpClient.Timeout = _options.HttpTimeout;
            _httpClient.DefaultRequestHeaders.Add("api-key", _options.ApiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Configure JSON options
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false
            };

            // Configure retry policy
            _retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(
                    _options.MaxRetryAttempts,
                    retryAttempt => TimeSpan.FromMilliseconds(_options.RetryDelay.TotalMilliseconds * Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        var statusCode = outcome.Result?.StatusCode;
                        _logger.LogWarning(
                            "Retry {RetryCount} after {Delay}ms due to {StatusCode}",
                            retryCount, timespan.TotalMilliseconds, statusCode);
                    });

            _logger.LogInformation("SoraClient initialized with endpoint: {Endpoint}", _options.Endpoint);
        }

        /// <summary>
        /// Legacy constructor for backward compatibility
        /// </summary>
        public SoraClient(string endpoint, string apiKey, string deploymentName, string apiVersion = "2024-10-21")
            : this(new HttpClient(), new SoraClientOptions 
            { 
                Endpoint = endpoint, 
                ApiKey = apiKey, 
                DeploymentName = deploymentName,
                ApiVersion = apiVersion 
            })
        {
        }

        /// <inheritdoc/>
        public async Task<string> SubmitVideoJobAsync(
            string prompt,
            int width,
            int height,
            int durationInSeconds,
            string? aspectRatio = null,
            int? frameRate = null,
            int? seed = null,
            CancellationToken cancellationToken = default)
        {
            // Create and validate request
            var request = new VideoGenerationRequest
            {
                Prompt = prompt,
                Width = width,
                Height = height,
                DurationInSeconds = durationInSeconds,
                AspectRatio = aspectRatio,
                FrameRate = frameRate,
                Seed = seed
            };

            try
            {
                request.Validate();
            }
            catch (Exception ex)
            {
                throw new SoraValidationException("Invalid video generation parameters", 
                    new Dictionary<string, string[]> { ["validation"] = new[] { ex.Message } });
            }

            var jobRequest = new
            {
                model = _options.DeploymentName,
                prompt = request.Prompt,
                width = request.Width,
                height = request.Height,
                durationInSeconds = request.DurationInSeconds,
                aspectRatio = request.AspectRatio,
                frameRate = request.FrameRate,
                seed = request.Seed
            };

            var url = $"/openai/v1/video/generations/jobs?api-version={_options.ApiVersion}";
            
            _logger.LogInformation(
                "Submitting video job: {Width}x{Height}, {Duration}s, prompt length: {PromptLength}",
                width, height, durationInSeconds, prompt.Length);

            using var content = new StringContent(
                JsonSerializer.Serialize(jobRequest, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await ExecuteWithRetryAsync(
                () => _httpClient.PostAsync(url, content, cancellationToken),
                cancellationToken);

            await EnsureSuccessStatusCodeAsync(response, cancellationToken);

            var responseData = await DeserializeResponseAsync<VideoJobCreationResponse>(
                response, cancellationToken);

            if (string.IsNullOrEmpty(responseData?.JobId))
            {
                throw new SoraException("Invalid response: missing job ID");
            }

            _logger.LogInformation("Video job submitted successfully: {JobId}", responseData.JobId);
            return responseData.JobId;
        }

        /// <inheritdoc/>
        public async Task<JobDetails> GetJobStatusAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jobId))
                throw new ArgumentException("Job ID is required", nameof(jobId));

            var url = $"/openai/v1/video/generations/jobs/{jobId}?api-version={_options.ApiVersion}";
            
            _logger.LogDebug("Checking status for job: {JobId}", jobId);

            var response = await ExecuteWithRetryAsync(
                () => _httpClient.GetAsync(url, cancellationToken),
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new SoraNotFoundException($"Job not found: {jobId}", jobId);
            }

            await EnsureSuccessStatusCodeAsync(response, cancellationToken);

            var statusResponse = await DeserializeResponseAsync<VideoJobStatusResponse>(
                response, cancellationToken);

            if (statusResponse == null)
            {
                throw new SoraException("Invalid status response");
            }

            var details = new JobDetails
            {
                JobId = jobId,
                Status = ParseJobStatus(statusResponse.Status),
                VideoUrl = statusResponse.VideoUrl,
                ErrorMessage = statusResponse.Error?.Message,
                ErrorCode = statusResponse.Error?.Code,
                CreatedAt = statusResponse.CreatedAt,
                UpdatedAt = statusResponse.UpdatedAt,
                CompletedAt = statusResponse.CompletedAt,
                ProgressPercentage = statusResponse.ProgressPercentage,
                Metadata = statusResponse.Metadata
            };

            _logger.LogDebug("Job {JobId} status: {Status}", jobId, details.Status);
            return details;
        }

        /// <inheritdoc/>
        public async Task<Uri> WaitForCompletionAsync(
            string jobId,
            int pollIntervalSeconds = 5,
            TimeSpan? maxWaitTime = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jobId))
                throw new ArgumentException("Job ID is required", nameof(jobId));

            var effectiveMaxWaitTime = maxWaitTime ?? _options.MaxWaitTime;
            var deadline = DateTimeOffset.UtcNow.Add(effectiveMaxWaitTime);
            var pollInterval = TimeSpan.FromSeconds(Math.Max(1, pollIntervalSeconds));

            _logger.LogInformation(
                "Waiting for job {JobId} to complete (max wait: {MaxWait})",
                jobId, effectiveMaxWaitTime);

            while (DateTimeOffset.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var details = await GetJobStatusAsync(jobId, cancellationToken);

                switch (details.Status)
                {
                    case JobStatus.Succeeded:
                        if (string.IsNullOrEmpty(details.VideoUrl))
                        {
                            throw new SoraException("Job succeeded but video URL is missing");
                        }
                        _logger.LogInformation("Job {JobId} completed successfully", jobId);
                        return new Uri(details.VideoUrl);

                    case JobStatus.Failed:
                        throw new SoraException(
                            $"Job failed: {details.ErrorMessage ?? "Unknown error"}", 
                            details.ErrorCode ?? "UNKNOWN");

                    case JobStatus.Cancelled:
                        throw new SoraException("Job was cancelled", "CANCELLED");

                    case JobStatus.Running:
                    case JobStatus.Pending:
                        if (details.ProgressPercentage.HasValue)
                        {
                            _logger.LogDebug(
                                "Job {JobId} progress: {Progress}%", 
                                jobId, details.ProgressPercentage);
                        }
                        break;
                }

                await Task.Delay(pollInterval, cancellationToken);
            }

            throw new SoraTimeoutException(
                $"Job did not complete within {effectiveMaxWaitTime}", 
                effectiveMaxWaitTime);
        }

        /// <inheritdoc/>
        public async Task DownloadVideoAsync(
            Uri videoUri, 
            string filePath, 
            CancellationToken cancellationToken = default)
        {
            if (videoUri == null)
                throw new ArgumentNullException(nameof(videoUri));
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path is required", nameof(filePath));

            _logger.LogInformation("Downloading video from {Uri} to {FilePath}", videoUri, filePath);

            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Download with retry
                var response = await ExecuteWithRetryAsync(
                    () => _httpClient.GetAsync(videoUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken),
                    cancellationToken);

                await EnsureSuccessStatusCodeAsync(response, cancellationToken);

                // Stream to file
                await using var fileStream = new FileStream(
                    filePath, 
                    FileMode.Create, 
                    FileAccess.Write, 
                    FileShare.None,
                    bufferSize: 81920, // 80KB buffer
                    useAsync: true);

                await response.Content.CopyToAsync(fileStream, cancellationToken);
                
                _logger.LogInformation("Video downloaded successfully to {FilePath}", filePath);
            }
            catch (Exception ex) when (!(ex is SoraException))
            {
                throw new SoraException($"Failed to download video: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Executes an HTTP operation with retry policy
        /// </summary>
        private async Task<HttpResponseMessage> ExecuteWithRetryAsync(
            Func<Task<HttpResponseMessage>> operation,
            CancellationToken cancellationToken)
        {
            try
            {
                return await _retryPolicy.ExecuteAsync(async () => await operation());
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed");
                throw new SoraException("Network error occurred", ex);
            }
            catch (TaskCanceledException ex)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException("Operation was cancelled", ex, cancellationToken);
                }
                throw new SoraTimeoutException("Request timed out", _options.HttpTimeout);
            }
        }

        /// <summary>
        /// Ensures the response has a success status code and handles specific errors
        /// </summary>
        private async Task EnsureSuccessStatusCodeAsync(
            HttpResponseMessage response, 
            CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
                return;

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Request failed with status {StatusCode}: {Content}", 
                response.StatusCode, content);

            // Try to parse error response
            ErrorResponse? errorResponse = null;
            try
            {
                errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content, _jsonOptions);
            }
            catch
            {
                // Ignore deserialization errors
            }

            var errorMessage = errorResponse?.Error?.Message ?? content;
            var errorCode = errorResponse?.Error?.Code;

            throw response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => new SoraAuthenticationException(
                    "Authentication failed. Please check your API key."),
                HttpStatusCode.Forbidden => new SoraAuthenticationException(
                    "Access forbidden. Please check your permissions."),
                HttpStatusCode.NotFound => new SoraNotFoundException(errorMessage),
                HttpStatusCode.TooManyRequests => new SoraRateLimitException(
                    "Rate limit exceeded", GetRetryAfter(response)),
                HttpStatusCode.BadRequest => new SoraValidationException(errorMessage),
                _ => new SoraException(
                    $"Request failed: {errorMessage}", 
                    response.StatusCode, 
                    errorCode)
            };
        }

        /// <summary>
        /// Deserializes the response content
        /// </summary>
        private async Task<T?> DeserializeResponseAsync<T>(
            HttpResponseMessage response, 
            CancellationToken cancellationToken)
        {
            var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions, cancellationToken);
        }

        /// <summary>
        /// Gets the retry-after duration from response headers
        /// </summary>
        private TimeSpan? GetRetryAfter(HttpResponseMessage response)
        {
            if (response.Headers.RetryAfter != null)
            {
                if (response.Headers.RetryAfter.Delta.HasValue)
                    return response.Headers.RetryAfter.Delta.Value;
                
                if (response.Headers.RetryAfter.Date.HasValue)
                    return response.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow;
            }
            return null;
        }

        /// <summary>
        /// Parses the job status string to enum
        /// </summary>
        private JobStatus ParseJobStatus(string? status)
        {
            return status?.ToLowerInvariant() switch
            {
                "running" => JobStatus.Running,
                "succeeded" => JobStatus.Succeeded,
                "failed" => JobStatus.Failed,
                "cancelled" => JobStatus.Cancelled,
                "pending" => JobStatus.Pending,
                _ => JobStatus.Unknown
            };
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes resources
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _semaphore?.Dispose();
                // Note: HttpClient should be disposed by the factory that created it
            }

            _disposed = true;
        }

        /// <summary>
        /// Disposes resources asynchronously
        /// </summary>
        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (_disposed)
                return;

            // Perform async cleanup if needed
            await Task.CompletedTask;
        }

        // Response DTOs
        private class VideoJobCreationResponse
        {
            [JsonPropertyName("jobId")]
            public string JobId { get; set; } = string.Empty;
        }

        private class VideoJobStatusResponse
        {
            [JsonPropertyName("status")]
            public string Status { get; set; } = string.Empty;

            [JsonPropertyName("videoUrl")]
            public string? VideoUrl { get; set; }

            [JsonPropertyName("error")]
            public VideoError? Error { get; set; }

            [JsonPropertyName("createdAt")]
            public DateTimeOffset? CreatedAt { get; set; }

            [JsonPropertyName("updatedAt")]
            public DateTimeOffset? UpdatedAt { get; set; }

            [JsonPropertyName("completedAt")]
            public DateTimeOffset? CompletedAt { get; set; }

            [JsonPropertyName("progressPercentage")]
            public int? ProgressPercentage { get; set; }

            [JsonPropertyName("metadata")]
            public Dictionary<string, object>? Metadata { get; set; }
        }

        private class VideoError
        {
            [JsonPropertyName("message")]
            public string Message { get; set; } = string.Empty;

            [JsonPropertyName("code")]
            public string? Code { get; set; }
        }

        private class ErrorResponse
        {
            [JsonPropertyName("error")]
            public ErrorDetail? Error { get; set; }
        }

        private class ErrorDetail
        {
            [JsonPropertyName("message")]
            public string Message { get; set; } = string.Empty;

            [JsonPropertyName("code")]
            public string? Code { get; set; }
        }
    }
} 