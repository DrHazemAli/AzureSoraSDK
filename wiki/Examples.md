# Examples

This page contains comprehensive examples of using the AzureSoraSDK.

## Table of Contents

- [Basic Examples](#basic-examples)
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
    prompt: "A peaceful forest with sunlight filtering through trees",
    width: 1920,
    height: 1080,
    durationInSeconds: 10
);

// Wait and download
var videoUrl = await client.WaitForCompletionAsync(jobId);
await client.DownloadVideoAsync(videoUrl, "forest.mp4");
```

### Custom Video Parameters

```csharp
var jobId = await client.SubmitVideoJobAsync(
    prompt: "Time-lapse of clouds moving across a blue sky",
    width: 3840,
    height: 2160,
    durationInSeconds: 30,
    aspectRatio: "16:9",
    frameRate: 60,
    seed: 12345  // For reproducible results
);
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

### Using Prompt Enhancement

```csharp
public class VideoService
{
    private readonly ISoraClient _soraClient;
    private readonly IPromptEnhancer _promptEnhancer;
    
    public VideoService(ISoraClient soraClient, IPromptEnhancer promptEnhancer)
    {
        _soraClient = soraClient;
        _promptEnhancer = promptEnhancer;
    }
    
    public async Task<string> GenerateEnhancedVideo(string userPrompt)
    {
        // Get enhanced prompts
        var suggestions = await _promptEnhancer.SuggestPromptsAsync(
            userPrompt, 
            maxSuggestions: 5
        );
        
        // Let user choose or auto-select best one
        Console.WriteLine("Enhanced prompt suggestions:");
        for (int i = 0; i < suggestions.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {suggestions[i]}");
        }
        
        // Use the first suggestion
        var enhancedPrompt = suggestions.FirstOrDefault() ?? userPrompt;
        
        // Generate video with enhanced prompt
        var jobId = await _soraClient.SubmitVideoJobAsync(
            enhancedPrompt,
            1920,
            1080,
            15
        );
        
        return await _soraClient.WaitForCompletionAsync(jobId);
    }
}
```

## Integration Examples

### ASP.NET Core Web API

```csharp
[ApiController]
[Route("api/[controller]")]
public class VideoGenerationController : ControllerBase
{
    private readonly ISoraClient _soraClient;
    private readonly ILogger<VideoGenerationController> _logger;
    
    public VideoGenerationController(
        ISoraClient soraClient,
        ILogger<VideoGenerationController> logger)
    {
        _soraClient = soraClient;
        _logger = logger;
    }
    
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateVideo([FromBody] VideoRequest request)
    {
        try
        {
            // Validate request
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            // Submit job
            var jobId = await _soraClient.SubmitVideoJobAsync(
                request.Prompt,
                request.Width ?? 1920,
                request.Height ?? 1080,
                request.Duration ?? 10,
                request.AspectRatio,
                request.FrameRate
            );
            
            _logger.LogInformation("Video job submitted: {JobId}", jobId);
            
            return Ok(new
            {
                jobId,
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
    }
    
    [HttpGet("{jobId}/status")]
    public async Task<IActionResult> GetStatus(string jobId)
    {
        var status = await _soraClient.GetJobStatusAsync(jobId);
        
        return Ok(new
        {
            jobId = status.JobId,
            status = status.Status.ToString(),
            progress = status.ProgressPercentage,
            videoUrl = status.VideoUrl,
            error = status.ErrorMessage
        });
    }
    
    [HttpGet("{jobId}/download")]
    public async Task<IActionResult> DownloadVideo(string jobId)
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
}

public class VideoRequest
{
    [Required]
    public string Prompt { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Duration { get; set; }
    public string? AspectRatio { get; set; }
    public int? FrameRate { get; set; }
}
```

### Background Job Processing

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
        
        // Submit job
        var jobId = await soraClient.SubmitVideoJobAsync(
            job.Prompt,
            job.Width,
            job.Height,
            job.Duration,
            cancellationToken: cancellationToken
        );
        
        // Update job status in database
        job.AzureJobId = jobId;
        job.Status = "Processing";
        // Save to database...
        
        // Wait for completion
        try
        {
            var videoUrl = await soraClient.WaitForCompletionAsync(
                jobId,
                cancellationToken: cancellationToken
            );
            
            // Download to storage
            var filePath = Path.Combine("videos", $"{job.Id}.mp4");
            await soraClient.DownloadVideoAsync(videoUrl, filePath, cancellationToken);
            
            job.Status = "Completed";
            job.VideoPath = filePath;
        }
        catch (SoraException ex)
        {
            job.Status = "Failed";
            job.Error = ex.Message;
        }
        
        // Update database...
        _logger.LogInformation("Video job completed: {JobId}", job.Id);
    }
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
                    request.Duration
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