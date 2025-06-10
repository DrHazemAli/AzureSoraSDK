# AzureSoraSDK

**Azure OpenAI "Sora" Video Generation & Prompt Enhancement SDK**

[![.NET 6.0+](https://img.shields.io/badge/.NET-6.0%2B-blue.svg)](https://dotnet.microsoft.com/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

![Sora SDK](https://github.com/DrHazemAli/AzureSoraSDK/blob/main/sora_sdk.jpg)

📚 **[Documentation Wiki](https://github.com/DrHazemAli/AzureSoraSDK/wiki)** | 📋 **[Changelog](CHANGELOG.md)** | 📦 **[NuGet Package](https://www.nuget.org/packages/AzureSoraSDK)**

This is a community-driven SDK for Azure OpenAI Sora video generation and prompt enhancement. The SDK provides a comprehensive .NET solution for generating high-quality videos using Azure OpenAI's Sora model, with built-in prompt enhancement capabilities to help you create more compelling video content.

## Features

### Core Features
- **Video Generation**: Submit jobs, poll status, wait for completion, download MP4
- **Customizable**: Width, height, duration, aspect ratio, frame rate, seed, quality settings
- **Job Management**: Check job status with detailed progress tracking
- **Enhanced Prompt**: Get real-time AI-powered prompt improvement suggestions
- **API Versioning**: Specify Azure OpenAI API version (default: 2024-10-21)
- **Dependency Injection**: Full support for .NET DI with `IServiceCollection` extensions
- **Robust Error Handling**: Specific exception types for different error scenarios
- **Retry Logic**: Automatic retry with exponential backoff using Polly
- **Circuit Breaker**: Prevents cascading failures with circuit breaker pattern
- **Logging**: Integrated Microsoft.Extensions.Logging support
- **HttpClient Management**: Proper HttpClient lifecycle management with IHttpClientFactory
- **Nullable Reference Types**: Full C# nullable reference type support
- **Async Disposal**: Implements IAsyncDisposable for proper resource cleanup
- **Configuration Validation**: Comprehensive validation with data annotations
- **Unit Testing**: Extensive test coverage with xUnit, Moq, and FluentAssertions
- **Thread Safety**: Thread-safe operations with proper synchronization

## Installation

```bash
# Install from NuGet
dotnet add package AzureSoraSDK

# Or via Package Manager
Install-Package AzureSoraSDK
```

## Quick Start

### Basic Usage (Legacy)

```csharp
using AzureSoraSDK;
using System;

var endpoint   = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!;
var apiKey     = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY")!;
var deployment = "sora";

using var client = new SoraClient(endpoint, apiKey, deployment);

// Submit a video job
var jobId = await client.SubmitVideoJobAsync(
    prompt: "A serene sunset over mountain peaks, 10 seconds",
    width: 1280,
    height: 720,
    durationInSeconds: 10
);

// Wait for completion
var videoUri = await client.WaitForCompletionAsync(jobId);

// Download the video
await client.DownloadVideoAsync(videoUri, "output.mp4");
```

### Dependency Injection (Recommended)

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using AzureSoraSDK.Extensions;
using AzureSoraSDK.Interfaces;

// In your Startup.cs or Program.cs
var builder = WebApplication.CreateBuilder(args);

// Configure from appsettings.json
builder.Services.AddAzureSoraSDK(builder.Configuration.GetSection("AzureSora"));

// Or configure manually
builder.Services.AddAzureSoraSDK(options =>
{
    options.Endpoint = "https://your-resource.openai.azure.com";
    options.ApiKey = "your-api-key";
    options.DeploymentName = "sora";
    options.MaxRetryAttempts = 5;
    options.HttpTimeout = TimeSpan.FromMinutes(10);
});

// In your service/controller
public class VideoService
{
    private readonly ISoraClient _soraClient;
    private readonly IPromptEnhancer _promptEnhancer;
    private readonly ILogger<VideoService> _logger;

    public VideoService(
        ISoraClient soraClient, 
        IPromptEnhancer promptEnhancer,
        ILogger<VideoService> logger)
    {
        _soraClient = soraClient;
        _promptEnhancer = promptEnhancer;
        _logger = logger;
    }

    public async Task<string> GenerateVideoAsync(string prompt)
    {
        try
        {
            // Enhance the prompt first
            var suggestions = await _promptEnhancer.SuggestPromptsAsync(prompt, 3);
            var enhancedPrompt = suggestions.FirstOrDefault() ?? prompt;
            
            _logger.LogInformation("Generating video with prompt: {Prompt}", enhancedPrompt);

            // Submit video generation job
            var jobId = await _soraClient.SubmitVideoJobAsync(
                prompt: enhancedPrompt,
                width: 1920,
                height: 1080,
                durationInSeconds: 15,
                aspectRatio: "16:9",
                frameRate: 30,
                quality: "high"
            );

            // Wait with timeout
            var videoUri = await _soraClient.WaitForCompletionAsync(
                jobId,
                pollIntervalSeconds: 3,
                maxWaitTime: TimeSpan.FromMinutes(30)
            );

            return videoUri.ToString();
        }
        catch (SoraValidationException ex)
        {
            _logger.LogError(ex, "Validation failed: {Errors}", ex.ValidationErrors);
            throw;
        }
        catch (SoraRateLimitException ex)
        {
            _logger.LogWarning("Rate limited. Retry after: {RetryAfter}", ex.RetryAfter);
            throw;
        }
    }
}
```

## Configuration

### appsettings.json

```json
{
  "AzureSora": {
    "Endpoint": "https://your-resource.openai.azure.com",
    "ApiKey": "your-api-key",
    "DeploymentName": "sora",
    "ApiVersion": "2024-10-21",
    "HttpTimeout": "00:05:00",
    "MaxRetryAttempts": 3,
    "RetryDelay": "00:00:02",
    "DefaultPollingInterval": "00:00:05",
    "MaxWaitTime": "01:00:00"
  }
}
```

### Environment Variables

```bash
export AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com"
export AZURE_OPENAI_KEY="your-api-key"
export AZURE_OPENAI_DEPLOYMENT="sora"
```

## Advanced Usage

### Error Handling

```csharp
try
{
    var jobId = await client.SubmitVideoJobAsync(...);
}
catch (SoraAuthenticationException ex)
{
    // Handle authentication failures
    Console.WriteLine($"Auth failed: {ex.Message}");
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
        // Retry the operation
    }
}
catch (SoraTimeoutException ex)
{
    // Handle timeouts
    Console.WriteLine($"Operation timed out after {ex.Timeout}");
}
catch (SoraNotFoundException ex)
{
    // Handle not found errors
    Console.WriteLine($"Resource not found: {ex.ResourceId}");
}
```

### Video Generation Options

```csharp
var request = new VideoGenerationRequest
{
    Prompt = "A futuristic cityscape at night with flying cars",
    Width = 1920,
    Height = 1080,
    DurationInSeconds = 20,
    AspectRatio = "16:9",
    FrameRate = 60,
    Quality = "ultra",      // standard, high, ultra
    Style = "cinematic",    // realistic, animated, artistic, etc.
    Seed = 42,             // For reproducible generation
    Metadata = new Dictionary<string, string>
    {
        ["project"] = "marketing-campaign",
        ["version"] = "v1"
    }
};

// Validate before submission
request.Validate();

var jobId = await client.SubmitVideoJobAsync(request);
```

### Progress Monitoring

```csharp
var jobId = await client.SubmitVideoJobAsync(...);

while (true)
{
    var details = await client.GetJobStatusAsync(jobId);
    
    Console.WriteLine($"Status: {details.Status}");
    
    if (details.ProgressPercentage.HasValue)
    {
        Console.WriteLine($"Progress: {details.ProgressPercentage}%");
    }
    
    if (details.EstimatedTimeRemaining.HasValue)
    {
        Console.WriteLine($"ETA: {details.EstimatedTimeRemaining}");
    }
    
    if (details.Status == JobStatus.Succeeded)
    {
        Console.WriteLine($"Video URL: {details.VideoUrl}");
        break;
    }
    else if (details.Status == JobStatus.Failed)
    {
        Console.WriteLine($"Failed: {details.ErrorMessage} (Code: {details.ErrorCode})");
        break;
    }
    
    await Task.Delay(TimeSpan.FromSeconds(5));
}
```

### Prompt Enhancement

```csharp
var enhancer = serviceProvider.GetRequiredService<IPromptEnhancer>();

// Get multiple suggestions
var suggestions = await enhancer.SuggestPromptsAsync(
    "A sunset scene",
    maxSuggestions: 5
);

foreach (var suggestion in suggestions)
{
    Console.WriteLine($"- {suggestion}");
}

// Output might be:
// - A vibrant sunset over the ocean with golden rays reflecting on calm waters
// - A dramatic sunset behind mountain silhouettes with purple and orange clouds  
// - A peaceful sunset in a meadow with warm light casting long shadows
// - A tropical sunset with palm trees swaying in the gentle breeze
// - An urban sunset with city skyline silhouetted against colorful sky
```

## Testing

The SDK includes comprehensive unit tests. To run tests:

```bash
cd src/AzureSoraSDK.Tests
dotnet test

# With coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific tests
dotnet test --filter "FullyQualifiedName~SoraClientTests"
```

## API Reference

### ISoraClient

- `SubmitVideoJobAsync` - Submit a video generation job
- `GetJobStatusAsync` - Get current job status and details
- `WaitForCompletionAsync` - Wait for job completion with polling
- `DownloadVideoAsync` - Download generated video to local file

### IPromptEnhancer

- `SuggestPromptsAsync` - Get AI-powered prompt suggestions

### Configuration Options

- `Endpoint` - Azure OpenAI endpoint URL
- `ApiKey` - API key for authentication
- `DeploymentName` - Name of your Sora deployment
- `ApiVersion` - API version (default: 2024-10-21)
- `HttpTimeout` - HTTP request timeout (default: 5 minutes)
- `MaxRetryAttempts` - Max retry attempts (default: 3)
- `RetryDelay` - Base delay between retries (default: 2 seconds)
- `DefaultPollingInterval` - Job status polling interval (default: 5 seconds)
- `MaxWaitTime` - Maximum wait time for job completion (default: 1 hour)

## Requirements

- .NET 6.0 or higher
- Azure OpenAI resource with Sora model deployment
- Valid API key with appropriate permissions

## Documentation

- **Video Generation Concepts**: https://learn.microsoft.com/azure/ai-services/openai/concepts/video-generation
- **API Reference**: https://learn.microsoft.com/azure/ai-services/openai/reference
- **Samples**: See the `/samples` directory for more examples

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

MIT © Hazem Ali

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for a detailed list of changes in each release.