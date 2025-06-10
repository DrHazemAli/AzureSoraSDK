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
    int nSeconds,
    CancellationToken cancellationToken = default)
```

**Parameters:**
- `prompt` (string, required): The text description of the video to generate
- `width` (int, required): Video width in pixels (128-2048)
- `height` (int, required): Video height in pixels (128-2048)
- `nSeconds` (int, required): Video duration (1-60 seconds)
- `cancellationToken` (CancellationToken, optional): Cancellation token

**Returns:** Task<string> - The job ID

**Throws:**
- `SoraValidationException`: Invalid parameters
- `SoraAuthenticationException`: Authentication failed
- `SoraRateLimitException`: Rate limit exceeded

#### SubmitVideoJobAsync (Aspect Ratio Overload)

Submits a video generation job using aspect ratio and quality settings.

```csharp
Task<string> SubmitVideoJobAsync(
    string prompt,
    string aspectRatio,
    string quality,
    int nSeconds,
    CancellationToken cancellationToken = default)
```

**Parameters:**
- `prompt` (string, required): The text description of the video to generate
- `aspectRatio` (string, required): Aspect ratio (e.g., "16:9", "4:3", "1:1", "9:16")
- `quality` (string, required): Quality level: "low", "medium", "high", "ultra"
- `nSeconds` (int, required): Video duration (1-60 seconds)
- `cancellationToken` (CancellationToken, optional): Cancellation token

**Returns:** Task<string> - The job ID

**Supported Aspect Ratios:**
- `16:9` - Widescreen (YouTube, TV)
- `4:3` - Standard (older TVs)
- `1:1` - Square (Instagram)
- `9:16` - Vertical/Portrait (Instagram Stories, TikTok)
- `3:4` - Portrait
- `21:9` - Ultrawide (Cinema)

**Quality Presets:**
- `low` - Lower resolution for faster generation
- `medium` - Balanced quality and speed
- `high` - High quality (default)
- `ultra` - Maximum quality

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
    public int NSeconds { get; set; }
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

## Utility Methods

### CalculateDimensionsFromAspectRatio

Calculates video dimensions from an aspect ratio string.

```csharp
public static (int width, int height) CalculateDimensionsFromAspectRatio(
    string aspectRatio, 
    int targetSize = 1920, 
    bool preferWidth = true)
```

**Parameters:**
- `aspectRatio` (string): Aspect ratio in format "width:height" (e.g., "16:9")
- `targetSize` (int): Target size for the larger dimension (default: 1920)
- `preferWidth` (bool): If true, targetSize applies to width; if false, applies to height

**Returns:** Tuple of (width, height) rounded to nearest 8 pixels

**Example:**
```csharp
// Calculate dimensions for 16:9 with width of 1920
var (width, height) = SoraClient.CalculateDimensionsFromAspectRatio("16:9", 1920, true);
// Returns: (1920, 1080)

// Calculate dimensions for 4:3 with height of 720
var (width, height) = SoraClient.CalculateDimensionsFromAspectRatio("4:3", 720, false);
// Returns: (960, 720)
```

### GetCommonDimensions

Gets common video dimensions for standard aspect ratios and quality levels.

```csharp
public static (int width, int height) GetCommonDimensions(
    string aspectRatio, 
    string quality = "high")
```

**Parameters:**
- `aspectRatio` (string): Aspect ratio (e.g., "16:9", "4:3", "1:1")
- `quality` (string): Quality level: "low", "medium", "high", "ultra"

**Returns:** Tuple of (width, height)

**Example:**
```csharp
// Get HD dimensions for 16:9
var (width, height) = SoraClient.GetCommonDimensions("16:9", "high");
// Returns: (1920, 1080)

// Get low quality square video dimensions
var (width, height) = SoraClient.GetCommonDimensions("1:1", "low");
// Returns: (480, 480)
```

**Common Dimensions Table:**

| Aspect Ratio | Low | Medium | High | Ultra |
|--------------|-----|---------|------|--------|
| 16:9 | 640×360 | 1280×720 | 1920×1080 | 2048×1152 |
| 4:3 | 640×480 | 1024×768 | 1600×1200 | 2048×1536 |
| 1:1 | 480×480 | 720×720 | 1080×1080 | 2048×2048 |
| 9:16 | 360×640 | 720×1280 | 1080×1920 | 1152×2048 |
| 3:4 | 480×640 | 768×1024 | 1200×1600 | 1536×2048 |
| 21:9 | 840×360 | 1680×720 | 2048×880 | 2048×880 |

## Next Steps

- [Examples](Examples) - More code examples
- [Error Handling](Error-Handling) - Detailed error handling guide
- [Configuration](Configuration) - Configuration reference 