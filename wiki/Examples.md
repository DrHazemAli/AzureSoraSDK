# Examples

This page contains comprehensive examples of using the AzureSoraSDK.

## Table of Contents

- [Basic Examples](#basic-examples)
- [Configuration Examples](#configuration-examples)
- [Advanced Examples](#advanced-examples)
- [Integration Examples](#integration-examples)
- [Error Handling Examples](#error-handling-examples)

## Basic Examples

### Simple Video Generation

```csharp
using AzureSoraSDK;

// Create client
var client = new SoraClient(
    "https://your-resource.openai.azure.com",
    "your-api-key",
    "sora"
);

// Generate a video
var jobId = await client.SubmitVideoJobAsync(
    prompt: "A peaceful sunrise over mountain peaks",
    width: 1280,
    height: 720,
    nSeconds: 10
);

// Wait and download
var videoUrl = await client.WaitForCompletionAsync(jobId);
await client.DownloadVideoAsync(videoUrl, "forest.mp4");
```

### Custom Video Parameters

```csharp
// Generate longer video with higher resolution
var jobId = await soraClient.SubmitVideoJobAsync(
    prompt: "Epic space battle with laser effects",
    width: 1920,
    height: 1080,
    nSeconds: 30
);
```

### Using Aspect Ratio and Quality

```csharp
// Generate HD widescreen video
var jobId = await soraClient.SubmitVideoJobAsync(
    prompt: "A cinematic sunset over mountains",
    aspectRatio: "16:9",
    quality: "high",
    nSeconds: 15
);

// Generate vertical video for social media
var jobId = await soraClient.SubmitVideoJobAsync(
    prompt: "Product showcase with dynamic transitions",
    aspectRatio: "9:16",
    quality: "medium",
    nSeconds: 30
);

// Generate square video for Instagram
var jobId = await soraClient.SubmitVideoJobAsync(
    prompt: "Artistic abstract animation",
    aspectRatio: "1:1",
    quality: "ultra",
    nSeconds: 10
);
```

### Calculating Custom Dimensions

```csharp
// Calculate dimensions for custom aspect ratio
var (width, height) = SoraClient.CalculateDimensionsFromAspectRatio("2.35:1", 2048);
Console.WriteLine($"Cinema dimensions: {width}x{height}");
// Output: Cinema dimensions: 2048x872

// Calculate dimensions with height preference
var (w, h) = SoraClient.CalculateDimensionsFromAspectRatio("3:2", 1200, preferWidth: false);
Console.WriteLine($"Portrait dimensions: {w}x{h}");
// Output: Portrait dimensions: 1800x1200
```

### Dynamic Quality Selection

```csharp
public async Task<string> GenerateVideoWithDynamicQuality(
    string prompt,
    string aspectRatio,
    bool prioritizeSpeed)
{
    // Choose quality based on priority
    string quality = prioritizeSpeed ? "low" : "high";
    
    // Get dimensions for the selected quality
    var (width, height) = SoraClient.GetCommonDimensions(aspectRatio, quality);
    
    _logger.LogInformation(
        "Generating {AspectRatio} video at {Quality} quality: {Width}x{Height}",
        aspectRatio, quality, width, height
    );
    
    return await soraClient.SubmitVideoJobAsync(
        prompt,
        aspectRatio,
        quality,
        nSeconds: 10
    );
}
```

## Configuration Examples

### Separate Configuration for Video Generation and Prompt Enhancement

```csharp
using Microsoft.Extensions.DependencyInjection;
using AzureSoraSDK.Extensions;
using AzureSoraSDK.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configure services with separate endpoints and settings
builder.Services.AddAzureSoraSDK(
    configureSoraOptions: options =>
    {
        options.Endpoint = "https://your-sora-endpoint.openai.azure.com";
        options.ApiKey = "your-sora-api-key";
        options.DeploymentName = "sora";
        options.ApiVersion = "preview";
        options.HttpTimeout = TimeSpan.FromMinutes(10);
        options.MaxRetryAttempts = 5;
        options.DefaultPollingInterval = TimeSpan.FromSeconds(3);
    },
    configurePromptEnhancerOptions: options =>
    {
        options.Endpoint = "https://your-chat-endpoint.openai.azure.com";
        options.ApiKey = "your-chat-api-key";
        options.DeploymentName = "gpt-4";
        options.ApiVersion = "2024-02-15-preview";
        options.HttpTimeout = TimeSpan.FromMinutes(3);
        options.DefaultTemperature = 0.6;
        options.DefaultTopP = 0.8;
        options.MaxTokensPerRequest = 2000;
    });

var app = builder.Build();
```

### Direct Configuration with PromptEnhancerOptions

```csharp
using AzureSoraSDK.Configuration;

// Configure prompt enhancer separately
var promptEnhancerOptions = new PromptEnhancerOptions
{
    Endpoint = "https://your-chat-endpoint.openai.azure.com",
    ApiKey = "your-chat-api-key",
    DeploymentName = "gpt-4-turbo",
    ApiVersion = "2024-02-15-preview",
    DefaultTemperature = 0.5,
    DefaultTopP = 0.9,
    MaxTokensPerRequest = 1500,
    HttpTimeout = TimeSpan.FromMinutes(2)
};

var promptEnhancer = new PromptEnhancer(httpClient, promptEnhancerOptions, logger);

// Configure video generation separately
var soraOptions = new SoraClientOptions
{
    Endpoint = "https://your-sora-endpoint.openai.azure.com",
    ApiKey = "your-sora-api-key",
    DeploymentName = "sora",
    ApiVersion = "preview"
};

var soraClient = new SoraClient(httpClient, soraOptions, logger);
```

### Configuration from appsettings.json with Separate Sections

```json
{
  "AzureSora": {
    "Endpoint": "https://your-sora-endpoint.openai.azure.com",
    "ApiKey": "your-sora-api-key",
    "DeploymentName": "sora",
    "ApiVersion": "preview",
    "HttpTimeout": "00:05:00",
    "MaxRetryAttempts": 3,
    "DefaultPollingInterval": "00:00:03"
  },
  "PromptEnhancer": {
    "Endpoint": "https://your-chat-endpoint.openai.azure.com",
    "ApiKey": "your-chat-api-key",
    "DeploymentName": "gpt-4",
    "ApiVersion": "2024-02-15-preview",
    "HttpTimeout": "00:02:00",
    "DefaultTemperature": 0.7,
    "MaxTokensPerRequest": 1500
  }
}
```

```csharp
// Load configuration from appsettings.json
builder.Services.AddAzureSoraSDK(builder.Configuration.GetSection("AzureSora"));
```

## Advanced Examples

### Progress Monitoring

```csharp
public async Task GenerateVideoWithProgress(ISoraClient client, string prompt)
{
    var jobId = await client.SubmitVideoJobAsync(prompt, 1920, 1080, 15);
    
    var lastProgress = 0;
    while (true)
    {
        var status = await client.GetJobStatusAsync(jobId);
        
        // Show progress
        if (status.ProgressPercentage.HasValue && 
            status.ProgressPercentage > lastProgress)
        {
            lastProgress = status.ProgressPercentage.Value;
            Console.WriteLine($"Progress: {lastProgress}%");
            
            if (status.EstimatedTimeRemaining.HasValue)
            {
                Console.WriteLine($"ETA: {status.EstimatedTimeRemaining}");
            }
        }
        
        // Check completion
        if (status.Status == JobStatus.Succeeded)
        {
            Console.WriteLine("Video generation completed!");
            return status.VideoUrl;
        }
        else if (status.Status == JobStatus.Failed)
        {
            throw new Exception($"Generation failed: {status.ErrorMessage}");
        }
        
        await Task.Delay(TimeSpan.FromSeconds(3));
    }
}
```

### Batch Video Generation

```csharp
public async Task<List<string>> GenerateMultipleVideos(
    ISoraClient client,
    List<string> prompts)
{
    var tasks = new List<Task<string>>();
    
    // Submit all jobs
    foreach (var prompt in prompts)
    {
        var task = SubmitAndWaitAsync(client, prompt);
        tasks.Add(task);
    }
    
    // Wait for all to complete
    var jobIds = await Task.WhenAll(tasks);
    return jobIds.ToList();
}

private async Task<string> SubmitAndWaitAsync(ISoraClient client, string prompt)
{
    var jobId = await client.SubmitVideoJobAsync(prompt, 1280, 720, 10);
    var videoUrl = await client.WaitForCompletionAsync(jobId);
    
    // Download with unique filename
    var filename = $"video_{jobId}.mp4";
    await client.DownloadVideoAsync(videoUrl, filename);
    
    return filename;
}
```

### Enhanced Prompt Generation with Separate Configuration

```csharp
public class VideoService
{
    private readonly ISoraClient _soraClient;
    private readonly IPromptEnhancer _promptEnhancer;
    private readonly ILogger<VideoService> _logger;
    
    public VideoService(ISoraClient soraClient, IPromptEnhancer promptEnhancer, ILogger<VideoService> logger)
    {
        _soraClient = soraClient;
        _promptEnhancer = promptEnhancer;
        _logger = logger;
    }
    
    public async Task<string> GenerateEnhancedVideo(string userPrompt)
    {
        try
        {
            // Get enhanced prompts using separate configuration
            var suggestions = await _promptEnhancer.SuggestPromptsAsync(
                userPrompt, 
                maxSuggestions: 5
            );
            
            _logger.LogInformation("Generated {Count} prompt suggestions", suggestions.Length);
            
            // Let user choose or auto-select best one
            Console.WriteLine("Enhanced prompt suggestions:");
            for (int i = 0; i < suggestions.Length; i++)
            {
                Console.WriteLine($"{i + 1}. {suggestions[i]}");
            }
            
            // Use the first suggestion or original prompt
            var enhancedPrompt = suggestions.FirstOrDefault() ?? userPrompt;
            _logger.LogInformation("Selected prompt: {Prompt}", enhancedPrompt);
            
            // Generate video with enhanced prompt using separate video endpoint
            var jobId = await _soraClient.SubmitVideoJobAsync(
                enhancedPrompt,
                1920,
                1080,
                15
            );
            
            var videoUrl = await _soraClient.WaitForCompletionAsync(jobId);
            return videoUrl.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating enhanced video");
            throw;
        }
    }
    
    public async Task<VideoGenerationResult> GenerateMultipleVariations(string basePrompt)
    {
        // Get multiple enhanced variations
        var suggestions = await _promptEnhancer.SuggestPromptsAsync(basePrompt, 3);
        var results = new List<VideoJobResult>();
        
        // Generate videos for each suggestion
        foreach (var suggestion in suggestions)
        {
            try
            {
                var jobId = await _soraClient.SubmitVideoJobAsync(
                    suggestion,
                    1280,
                    720,
                    10
                );
                
                results.Add(new VideoJobResult
                {
                    JobId = jobId,
                    Prompt = suggestion,
                    Status = "Submitted"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to submit job for prompt: {Prompt}", suggestion);
                results.Add(new VideoJobResult
                {
                    Prompt = suggestion,
                    Status = "Failed",
                    Error = ex.Message
                });
            }
        }
        
        return new VideoGenerationResult
        {
            BasePrompt = basePrompt,
            Jobs = results
        };
    }
}

public class VideoGenerationResult
{
    public string BasePrompt { get; set; } = string.Empty;
    public List<VideoJobResult> Jobs { get; set; } = new();
}

public class VideoJobResult
{
    public string? JobId { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? VideoUrl { get; set; }
    public string? Error { get; set; }
}
```

### Dynamic Prompt Enhancement Settings

```csharp
public class AdaptivePromptEnhancer
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AdaptivePromptEnhancer> _logger;
    
    public AdaptivePromptEnhancer(HttpClient httpClient, ILogger<AdaptivePromptEnhancer> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    
    public async Task<string[]> GetCreativePrompts(string basePrompt)
    {
        var creativeOptions = new PromptEnhancerOptions
        {
            Endpoint = "https://your-chat-endpoint.openai.azure.com",
            ApiKey = "your-chat-api-key",
            DeploymentName = "gpt-4",
            ApiVersion = "2024-02-15-preview",
            DefaultTemperature = 0.9,  // High creativity
            DefaultTopP = 0.95,
            MaxTokensPerRequest = 1500
        };
        
        var enhancer = new PromptEnhancer(_httpClient, creativeOptions, _logger);
        return await enhancer.SuggestPromptsAsync(basePrompt, 5);
    }
    
    public async Task<string[]> GetPrecisePrompts(string basePrompt)
    {
        var preciseOptions = new PromptEnhancerOptions
        {
            Endpoint = "https://your-chat-endpoint.openai.azure.com",
            ApiKey = "your-chat-api-key",
            DeploymentName = "gpt-4",
            ApiVersion = "2024-02-15-preview",
            DefaultTemperature = 0.3,  // Low creativity, high precision
            DefaultTopP = 0.7,
            MaxTokensPerRequest = 1000
        };
        
        var enhancer = new PromptEnhancer(_httpClient, preciseOptions, _logger);
        return await enhancer.SuggestPromptsAsync(basePrompt, 3);
    }
}
```

## Integration Examples

### ASP.NET Core Web API with Separate Configuration

```csharp
[ApiController]
[Route("api/[controller]")]
public class VideoGenerationController : ControllerBase
{
    private readonly ISoraClient _soraClient;
    private readonly IPromptEnhancer _promptEnhancer;
    private readonly ILogger<VideoGenerationController> _logger;
    
    public VideoGenerationController(
        ISoraClient soraClient,
        IPromptEnhancer promptEnhancer,
        ILogger<VideoGenerationController> logger)
    {
        _soraClient = soraClient;
        _promptEnhancer = promptEnhancer;
        _logger = logger;
    }
    
    [HttpPost("enhance-prompt")]
    public async Task<IActionResult> EnhancePrompt([FromBody] PromptRequest request)
    {
        try
        {
            var suggestions = await _promptEnhancer.SuggestPromptsAsync(
                request.Prompt,
                request.MaxSuggestions ?? 3
            );
            
            return Ok(new
            {
                original = request.Prompt,
                suggestions = suggestions,
                count = suggestions.Length
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enhance prompt: {Prompt}", request.Prompt);
            return BadRequest(new { error = "Failed to enhance prompt" });
        }
    }
    
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateVideo([FromBody] VideoRequest request)
    {
        try
        {
            // Validate request
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            string finalPrompt = request.Prompt;
            
            // Enhance prompt if requested
            if (request.EnhancePrompt)
            {
                var suggestions = await _promptEnhancer.SuggestPromptsAsync(request.Prompt, 1);
                finalPrompt = suggestions.FirstOrDefault() ?? request.Prompt;
                _logger.LogInformation("Enhanced prompt from '{Original}' to '{Enhanced}'", 
                    request.Prompt, finalPrompt);
            }
            
            // Submit video job
            var jobId = await _soraClient.SubmitVideoJobAsync(
                finalPrompt,
                request.Width ?? 1920,
                request.Height ?? 1080,
                request.nSeconds ?? 10
            );
            
            _logger.LogInformation("Video job submitted: {JobId}", jobId);
            
            return Ok(new
            {
                jobId,
                originalPrompt = request.Prompt,
                finalPrompt = finalPrompt,
                enhanced = request.EnhancePrompt,
                message = "Video generation started",
                statusUrl = $"/api/video/{jobId}/status"
            });
        }
        catch (SoraValidationException ex)
        {
            return BadRequest(new { errors = ex.ValidationErrors });
        }
        catch (SoraRateLimitException)
        {
            return StatusCode(429, "Rate limit exceeded. Please try again later.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate video");
            return StatusCode(500, "Internal server error");
        }
    }
    
    [HttpGet("{jobId}/status")]
    public async Task<IActionResult> GetStatus(string jobId)
    {
        try
        {
            var status = await _soraClient.GetJobStatusAsync(jobId);
            
            return Ok(new
            {
                jobId = status.JobId,
                status = status.Status.ToString(),
                progress = status.ProgressPercentage,
                videoUrl = status.VideoUrl,
                error = status.ErrorMessage,
                createdAt = status.CreatedAt,
                updatedAt = status.UpdatedAt
            });
        }
        catch (SoraNotFoundException)
        {
            return NotFound(new { error = $"Job {jobId} not found" });
        }
    }
    
    [HttpGet("{jobId}/download")]
    public async Task<IActionResult> DownloadVideo(string jobId)
    {
        try
        {
            var status = await _soraClient.GetJobStatusAsync(jobId);
            
            if (status.Status != JobStatus.Succeeded || string.IsNullOrEmpty(status.VideoUrl))
            {
                return BadRequest("Video not ready for download");
            }
            
            // Stream the video
            var httpClient = new HttpClient();
            var stream = await httpClient.GetStreamAsync(status.VideoUrl);
            
            return File(stream, "video/mp4", $"video_{jobId}.mp4");
        }
        catch (SoraNotFoundException)
        {
            return NotFound(new { error = $"Job {jobId} not found" });
        }
    }
}

public class PromptRequest
{
    [Required]
    public string Prompt { get; set; } = string.Empty;
    public int? MaxSuggestions { get; set; }
}

public class VideoRequest
{
    [Required]
    public string Prompt { get; set; } = string.Empty;
    public bool EnhancePrompt { get; set; } = true;
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? nSeconds { get; set; }
}
```

### Background Job Processing with Separate Services

```csharp
public class VideoGenerationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<VideoGenerationBackgroundService> _logger;
    private readonly Channel<VideoJob> _queue;
    
    public VideoGenerationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<VideoGenerationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _queue = Channel.CreateUnbounded<VideoJob>();
    }
    
    public async Task EnqueueVideoJob(VideoJob job)
    {
        await _queue.Writer.WriteAsync(job);
        _logger.LogInformation("Video job queued: {JobId}", job.Id);
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessVideoJob(job, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing video job {JobId}", job.Id);
            }
        }
    }
    
    private async Task ProcessVideoJob(VideoJob job, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var soraClient = scope.ServiceProvider.GetRequiredService<ISoraClient>();
        var promptEnhancer = scope.ServiceProvider.GetRequiredService<IPromptEnhancer>();
        
        try
        {
            // Enhance prompt if requested
            string finalPrompt = job.Prompt;
            if (job.EnhancePrompt)
            {
                var suggestions = await promptEnhancer.SuggestPromptsAsync(job.Prompt, 1);
                finalPrompt = suggestions.FirstOrDefault() ?? job.Prompt;
                _logger.LogInformation("Enhanced prompt for job {JobId}", job.Id);
            }
            
            // Submit job to Azure
            var azureJobId = await soraClient.SubmitVideoJobAsync(
                finalPrompt,
                job.Width,
                job.Height,
                job.nSeconds,
                cancellationToken: cancellationToken
            );
            
            // Update job status in database
            job.AzureJobId = azureJobId;
            job.FinalPrompt = finalPrompt;
            job.Status = "Processing";
            // Save to database...
            
            // Wait for completion
            var videoUrl = await soraClient.WaitForCompletionAsync(
                azureJobId,
                cancellationToken: cancellationToken
            );
            
            // Download to storage
            var filePath = Path.Combine("videos", $"{job.Id}.mp4");
            await soraClient.DownloadVideoAsync(videoUrl, filePath, cancellationToken);
            
            job.Status = "Completed";
            job.VideoPath = filePath;
            job.VideoUrl = videoUrl.ToString();
            
            _logger.LogInformation("Video job completed: {JobId}", job.Id);
        }
        catch (SoraException ex)
        {
            job.Status = "Failed";
            job.Error = ex.Message;
            _logger.LogError(ex, "Video job failed: {JobId}", job.Id);
        }
        
        // Update database...
    }
}

public class VideoJob
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Prompt { get; set; } = string.Empty;
    public string? FinalPrompt { get; set; }
    public bool EnhancePrompt { get; set; } = true;
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public int nSeconds { get; set; } = 10;
    public string Status { get; set; } = "Queued";
    public string? AzureJobId { get; set; }
    public string? VideoPath { get; set; }
    public string? VideoUrl { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

## Error Handling Examples

### Comprehensive Error Handling

```csharp
public class ResilientVideoService
{
    private readonly ISoraClient _soraClient;
    private readonly ILogger<ResilientVideoService> _logger;
    
    public async Task<VideoResult> GenerateVideoSafely(VideoRequest request)
    {
        var result = new VideoResult();
        var retryCount = 0;
        const int maxRetries = 3;
        
        while (retryCount < maxRetries)
        {
            try
            {
                // Submit job
                result.JobId = await _soraClient.SubmitVideoJobAsync(
                    request.Prompt,
                    request.Width,
                    request.Height,
                    request.nSeconds
                );
                
                // Wait with timeout
                var videoUrl = await _soraClient.WaitForCompletionAsync(
                    result.JobId,
                    maxWaitTime: TimeSpan.FromMinutes(20)
                );
                
                result.VideoUrl = videoUrl.ToString();
                result.Success = true;
                return result;
            }
            catch (SoraValidationException ex)
            {
                // Don't retry validation errors
                result.Error = "Invalid parameters";
                result.ValidationErrors = ex.ValidationErrors;
                _logger.LogError("Validation failed: {Errors}", ex.ValidationErrors);
                return result;
            }
            catch (SoraAuthenticationException ex)
            {
                // Don't retry auth errors
                result.Error = "Authentication failed";
                _logger.LogError(ex, "Authentication error");
                return result;
            }
            catch (SoraRateLimitException ex)
            {
                // Wait and retry
                if (ex.RetryAfter.HasValue && retryCount < maxRetries - 1)
                {
                    _logger.LogWarning("Rate limited, waiting {Delay}", ex.RetryAfter);
                    await Task.Delay(ex.RetryAfter.Value);
                    retryCount++;
                    continue;
                }
                result.Error = "Rate limit exceeded";
                return result;
            }
            catch (SoraTimeoutException ex)
            {
                result.Error = $"Operation timed out after {ex.Timeout}";
                _logger.LogError("Timeout: {Timeout}", ex.Timeout);
                return result;
            }
            catch (Exception ex)
            {
                // Retry other errors
                retryCount++;
                if (retryCount >= maxRetries)
                {
                    result.Error = "Unexpected error occurred";
                    _logger.LogError(ex, "Failed after {Retries} retries", maxRetries);
                    return result;
                }
                
                var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                _logger.LogWarning("Retrying after {Delay}", delay);
                await Task.Delay(delay);
            }
        }
        
        return result;
    }
}

public class VideoResult
{
    public bool Success { get; set; }
    public string? JobId { get; set; }
    public string? VideoUrl { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
}
```

## Next Steps

- [Error Handling](Error-Handling) - Detailed error handling guide
- [Troubleshooting](Troubleshooting) - Common issues and solutions
- [API Reference](API-Reference) - Complete API documentation 