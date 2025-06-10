# API Reference

Complete API documentation for AzureSoraSDK.

## ISoraClient Interface

The main interface for video generation operations.

### Methods

#### SubmitVideoJobAsync

Submits a video generation job to Azure OpenAI.

```csharp
Task<string> SubmitVideoJobAsync(
    string prompt,
    int width,
    int height,
    int durationInSeconds,
    string? aspectRatio = null,
    int? frameRate = null,
    int? seed = null,
    CancellationToken cancellationToken = default)
```

**Parameters:**
- `prompt` (string, required): The text prompt describing the video to generate
- `width` (int, required): Video width in pixels (must be divisible by 8)
- `height` (int, required): Video height in pixels (must be divisible by 8)
- `durationInSeconds` (int, required): Video duration (1-60 seconds)
- `aspectRatio` (string, optional): Aspect ratio (e.g., "16:9", "4:3")
- `frameRate` (int, optional): Frame rate (15-60 fps)
- `seed` (int, optional): Seed for reproducible generation
- `cancellationToken` (CancellationToken, optional): Cancellation token

**Returns:** Task<string> - The job ID

**Throws:**
- `SoraValidationException`: Invalid parameters
- `SoraAuthenticationException`: Authentication failed
- `SoraRateLimitException`: Rate limit exceeded

#### GetJobStatusAsync

Gets the current status of a video generation job.

```csharp
Task<JobDetails> GetJobStatusAsync(
    string jobId,
    CancellationToken cancellationToken = default)
```

**Parameters:**
- `jobId` (string, required): The job ID to check
- `cancellationToken` (CancellationToken, optional): Cancellation token

**Returns:** Task<JobDetails> - Job status details

**Throws:**
- `ArgumentException`: Invalid job ID
- `SoraNotFoundException`: Job not found

#### WaitForCompletionAsync

Waits for a job to complete, polling at regular intervals.

```csharp
Task<Uri> WaitForCompletionAsync(
    string jobId,
    int pollIntervalSeconds = 5,
    TimeSpan? maxWaitTime = null,
    CancellationToken cancellationToken = default)
```

**Parameters:**
- `jobId` (string, required): The job ID to wait for
- `pollIntervalSeconds` (int, optional): Polling interval (default: 5)
- `maxWaitTime` (TimeSpan?, optional): Maximum wait time (default: from config)
- `cancellationToken` (CancellationToken, optional): Cancellation token

**Returns:** Task<Uri> - The video download URL

**Throws:**
- `SoraTimeoutException`: Job didn't complete within timeout
- `SoraException`: Job failed or was cancelled

#### DownloadVideoAsync

Downloads a generated video to a local file.

```csharp
Task DownloadVideoAsync(
    Uri videoUri,
    string filePath,
    CancellationToken cancellationToken = default)
```

**Parameters:**
- `videoUri` (Uri, required): The video URL to download
- `filePath` (string, required): Local file path to save the video
- `cancellationToken` (CancellationToken, optional): Cancellation token

**Throws:**
- `ArgumentNullException`: Null parameters
- `SoraException`: Download failed

## IPromptEnhancer Interface

Interface for prompt enhancement functionality.

### Methods

#### SuggestPromptsAsync

Generates enhanced prompt suggestions.

```csharp
Task<List<string>> SuggestPromptsAsync(
    string basePrompt,
    int maxSuggestions = 5,
    CancellationToken cancellationToken = default)
```

**Parameters:**
- `basePrompt` (string, required): The base prompt to enhance
- `maxSuggestions` (int, optional): Maximum suggestions (default: 5)
- `cancellationToken` (CancellationToken, optional): Cancellation token

**Returns:** Task<List<string>> - List of enhanced prompts

## Models

### JobDetails

Represents the detailed status of a video generation job.

```csharp
public class JobDetails
{
    public string JobId { get; set; }
    public JobStatus Status { get; set; }
    public string? VideoUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int? ProgressPercentage { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
```

### JobStatus Enum

```csharp
public enum JobStatus
{
    Unknown,
    Pending,
    Running,
    Succeeded,
    Failed,
    Cancelled
}
```

### VideoGenerationRequest

Request model for video generation.

```csharp
public class VideoGenerationRequest
{
    public string Prompt { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int DurationInSeconds { get; set; }
    public string? AspectRatio { get; set; }
    public int? FrameRate { get; set; }
    public string? Style { get; set; }
    public string? Quality { get; set; }
    public int? Seed { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
```

## Exceptions

### Exception Hierarchy

```
Exception
└── SoraException
    ├── SoraAuthenticationException
    ├── SoraValidationException
    ├── SoraRateLimitException
    ├── SoraTimeoutException
    └── SoraNotFoundException
```

### SoraException

Base exception for all SDK-specific errors.

```csharp
public class SoraException : Exception
{
    public string? ErrorCode { get; }
    public HttpStatusCode? StatusCode { get; }
}
```

### SoraValidationException

Thrown when request validation fails.

```csharp
public class SoraValidationException : SoraException
{
    public Dictionary<string, string[]>? ValidationErrors { get; }
}
```

### SoraRateLimitException

Thrown when rate limits are exceeded.

```csharp
public class SoraRateLimitException : SoraException
{
    public TimeSpan? RetryAfter { get; }
}
```

## Extension Methods

### ServiceCollectionExtensions

```csharp
public static IServiceCollection AddAzureSoraSDK(
    this IServiceCollection services,
    Action<SoraClientOptions> configureOptions)

public static IServiceCollection AddAzureSoraSDK(
    this IServiceCollection services,
    IConfiguration configuration)
```

## Usage Examples

### Basic Video Generation

```csharp
// Submit job
var jobId = await soraClient.SubmitVideoJobAsync(
    "A beautiful sunset over the ocean",
    1920, 1080, 10
);

// Check status
var details = await soraClient.GetJobStatusAsync(jobId);
Console.WriteLine($"Status: {details.Status}");

// Wait for completion
var videoUrl = await soraClient.WaitForCompletionAsync(jobId);

// Download
await soraClient.DownloadVideoAsync(videoUrl, "sunset.mp4");
```

### With Error Handling

```csharp
try
{
    var jobId = await soraClient.SubmitVideoJobAsync(
        prompt, width, height, duration
    );
    
    var videoUrl = await soraClient.WaitForCompletionAsync(
        jobId, 
        maxWaitTime: TimeSpan.FromMinutes(20)
    );
    
    await soraClient.DownloadVideoAsync(videoUrl, outputPath);
}
catch (SoraValidationException ex)
{
    // Handle validation errors
    foreach (var error in ex.ValidationErrors ?? new())
    {
        Console.WriteLine($"{error.Key}: {string.Join(", ", error.Value)}");
    }
}
catch (SoraRateLimitException ex)
{
    // Handle rate limiting
    if (ex.RetryAfter.HasValue)
    {
        await Task.Delay(ex.RetryAfter.Value);
        // Retry...
    }
}
catch (SoraTimeoutException ex)
{
    // Handle timeout
    Console.WriteLine($"Operation timed out after {ex.Timeout}");
}
```

## Next Steps

- [Examples](Examples) - More code examples
- [Error Handling](Error-Handling) - Detailed error handling guide
- [Configuration](Configuration) - Configuration reference 