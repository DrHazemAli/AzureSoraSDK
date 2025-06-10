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
- **Customizable**: Width, height, and duration settings
- **Job Management**: Check job status with detailed progress tracking
- **Enhanced Prompt**: Get real-time AI-powered prompt improvement suggestions
- **API Versioning**: Specify Azure OpenAI API version (default: preview)
- **Separate Configuration**: Independent endpoint and API version for video generation and prompt enhancement
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
- **Production-Ready**: Built with enterprise requirements in mind
- **Type-Safe**: Strongly typed interfaces and comprehensive error handling
- **Async/Await**: Modern asynchronous programming patterns throughout
- **Configurable**: Flexible configuration options for different environments
- **Testable**: Designed with dependency injection and testing in mind
- **Customizable**: Width, height, and duration settings
- **Robust Error Handling**: Comprehensive exception types for different scenarios

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
    nSeconds: 10
);

// Wait for completion
var videoUri = await client.WaitForCompletionAsync(jobId);

// Download the video
await client.DownloadVideoAsync(videoUri, "output.mp4");
```

### Using Aspect Ratio (New in v1.0.2)

```csharp
using AzureSoraSDK;

// Generate video with aspect ratio and quality presets
var jobId = await client.SubmitVideoJobAsync(
    prompt: "A beautiful sunset over mountains",
    aspectRatio: "16:9",
    quality: "high",
    nSeconds: 10
);

// Calculate custom dimensions
var (width, height) = SoraClient.CalculateDimensionsFromAspectRatio("21:9", 2048);
// Returns: (2048, 880) - ultrawide cinema format

// Get common dimensions for aspect ratios
var (w, h) = SoraClient.GetCommonDimensions("1:1", "medium");
// Returns: (720, 720) - medium quality square video
```

### Dependency Injection with Separate Configuration (Recommended)

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using AzureSoraSDK.Extensions;
using AzureSoraSDK.Interfaces;

// In your Startup.cs or Program.cs
var builder = WebApplication.CreateBuilder(args);

// Option 1: Separate configuration for video generation and prompt enhancement
builder.Services.AddAzureSoraSDK(
    configureSoraOptions: options =>
    {
        options.Endpoint = "https://your-sora-endpoint.openai.azure.com";
        options.ApiKey = "your-sora-api-key";
        options.DeploymentName = "sora";
        options.ApiVersion = "preview"; // Video generation API version
    },
    configurePromptEnhancerOptions: options =>
    {
        options.Endpoint = "https://your-chat-endpoint.openai.azure.com";
        options.ApiKey = "your-chat-api-key";
        options.DeploymentName = "gpt-4";
        options.ApiVersion = "2024-02-15-preview"; // Chat completions API version
        options.DefaultTemperature = 0.7;
        options.MaxTokensPerRequest = 1500;
    });

// Option 2: Configuration from appsettings.json with separate sections
builder.Services.AddAzureSoraSDK(builder.Configuration.GetSection("AzureSora"));

// Option 3: Shared configuration (backward compatible)
builder.Services.AddAzureSoraSDK(options =>
{
    options.Endpoint = "https://your-resource.openai.azure.com";
    options.ApiKey = "your-api-key";
    options.DeploymentName = "sora";
    options.ApiVersion = "preview";
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
                nSeconds: 15
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

### appsettings.json with Separate Configuration

```json
{
  "AzureSora": {
    "Endpoint": "https://your-sora-endpoint.openai.azure.com",
    "ApiKey": "your-sora-api-key",
    "DeploymentName": "sora",
    "ApiVersion": "preview",
    "HttpTimeout": "00:05:00",
    "MaxRetryAttempts": 3,
    "RetryDelay": "00:00:02",
    "DefaultPollingInterval": "00:00:05",
    "MaxWaitTime": "01:00:00"
  },
  "PromptEnhancer": {
    "Endpoint": "https://your-chat-endpoint.openai.azure.com",
    "ApiKey": "your-chat-api-key",
    "DeploymentName": "gpt-4",
    "ApiVersion": "2024-02-15-preview",
    "HttpTimeout": "00:02:00",
    "MaxRetryAttempts": 3,
    "RetryDelay": "00:00:01",
    "DefaultTemperature": 0.7,
    "DefaultTopP": 0.9,
    "MaxTokensPerRequest": 1000
  }
}
```

### appsettings.json with Shared Configuration (Backward Compatible)

```json
{
  "AzureSora": {
    "Endpoint": "https://your-resource.openai.azure.com",
    "ApiKey": "your-api-key",
    "DeploymentName": "sora",
    "ApiVersion": "preview",
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
# For Video Generation (SoraClient)
export AZURE_OPENAI_SORA_ENDPOINT="https://your-sora-endpoint.openai.azure.com"
export AZURE_OPENAI_SORA_KEY="your-sora-api-key"
export AZURE_OPENAI_SORA_DEPLOYMENT="sora"

# For Prompt Enhancement (PromptEnhancer)
export AZURE_OPENAI_CHAT_ENDPOINT="https://your-chat-endpoint.openai.azure.com"
export AZURE_OPENAI_CHAT_KEY="your-chat-api-key"
export AZURE_OPENAI_CHAT_DEPLOYMENT="gpt-4"
```

## Advanced Usage

### Direct Configuration with PromptEnhancerOptions

```csharp
using AzureSoraSDK.Configuration;

// Create prompt enhancer with separate configuration
var promptEnhancerOptions = new PromptEnhancerOptions
{
    Endpoint = "https://your-chat-endpoint.openai.azure.com",
    ApiKey = "your-chat-api-key",
    DeploymentName = "gpt-4",
    ApiVersion = "2024-02-15-preview",
    DefaultTemperature = 0.5,
    DefaultTopP = 0.8,
    MaxTokensPerRequest = 2000,
    HttpTimeout = TimeSpan.FromMinutes(3)
};

var promptEnhancer = new PromptEnhancer(httpClient, promptEnhancerOptions, logger);

// Use different settings for different scenarios
var creativeSuggestions = await promptEnhancer.SuggestPromptsAsync(
    "A sunset scene", 
    maxSuggestions: 5
);
```

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

### Video Generation with Metadata

```csharp
// Submit a video generation job with metadata
var jobId = await client.SubmitVideoJobAsync(
    prompt: "A futuristic cityscape at night with flying cars",
    width: 1920,
    height: 1080,
    nSeconds: 20
);

// The VideoGenerationRequest model also supports metadata:
var request = new VideoGenerationRequest
{
    Prompt = "A beautiful sunset over mountains",
    Width = 1920,
    Height = 1080,
    NSeconds = 15,
    Metadata = new Dictionary<string, string>
    {
        ["project"] = "marketing-campaign",
        ["version"] = "v1"
    }
};

// Validate before submission
request.Validate();
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

### SoraClientOptions (Video Generation)

- `Endpoint` - Azure OpenAI endpoint URL for video generation
- `ApiKey` - API key for video generation authentication
- `DeploymentName` - Name of your Sora deployment
- `ApiVersion` - API version for video generation (default: preview)
- `HttpTimeout` - HTTP request timeout (default: 5 minutes)
- `MaxRetryAttempts` - Max retry attempts (default: 3)
- `RetryDelay` - Base delay between retries (default: 2 seconds)
- `DefaultPollingInterval` - Job status polling interval (default: 5 seconds)
- `MaxWaitTime` - Maximum wait time for job completion (default: 1 hour)

### PromptEnhancerOptions (Prompt Enhancement)

- `Endpoint` - Azure OpenAI endpoint URL for chat completions
- `ApiKey` - API key for chat completions authentication
- `DeploymentName` - Name of your chat completion deployment (e.g., gpt-4)
- `ApiVersion` - API version for chat completions (default: 2024-02-15-preview)
- `HttpTimeout` - HTTP request timeout (default: 2 minutes)
- `MaxRetryAttempts` - Max retry attempts (default: 3)
- `RetryDelay` - Base delay between retries (default: 1 second)
- `DefaultTemperature` - Temperature for prompt enhancement (default: 0.7)
- `DefaultTopP` - Top-p value for prompt enhancement (default: 0.9)
- `MaxTokensPerRequest` - Maximum tokens per request (default: 1000)

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