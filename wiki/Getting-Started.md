# Getting Started

This guide will help you get started with AzureSoraSDK quickly.

## Quick Start

### 1. Set up Azure OpenAI

First, ensure you have:
- An Azure OpenAI resource created
- A Sora model deployment
- Your endpoint URL and API key

### 2. Basic Usage (Console App)

```csharp
using AzureSoraSDK;
using System;

class Program
{
    static async Task Main(string[] args)
    {
        // Get credentials from environment variables
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!;
        var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY")!;
        var deployment = "sora"; // Your deployment name

        // Create client
        using var client = new SoraClient(endpoint, apiKey, deployment);

        // Submit a video generation job
        var jobId = await client.SubmitVideoJobAsync(
            prompt: "A serene sunset over mountain peaks, cinematic quality",
            width: 1920,
            height: 1080,
            durationInSeconds: 10
        );

        Console.WriteLine($"Job submitted: {jobId}");

        // Wait for completion
        var videoUri = await client.WaitForCompletionAsync(jobId);
        Console.WriteLine($"Video ready: {videoUri}");

        // Download the video
        await client.DownloadVideoAsync(videoUri, "output.mp4");
        Console.WriteLine("Video downloaded to output.mp4");
    }
}
```

### 3. Dependency Injection (ASP.NET Core)

#### Configure Services

```csharp
// Program.cs or Startup.cs
using AzureSoraSDK.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add AzureSoraSDK
builder.Services.AddAzureSoraSDK(options =>
{
    options.Endpoint = "https://your-resource.openai.azure.com";
    options.ApiKey = "your-api-key";
    options.DeploymentName = "sora";
});

// Or configure from appsettings.json
builder.Services.AddAzureSoraSDK(
    builder.Configuration.GetSection("AzureSora"));
```

#### Use in a Controller

```csharp
using AzureSoraSDK.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class VideoController : ControllerBase
{
    private readonly ISoraClient _soraClient;

    public VideoController(ISoraClient soraClient)
    {
        _soraClient = soraClient;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateVideo([FromBody] VideoRequest request)
    {
        var jobId = await _soraClient.SubmitVideoJobAsync(
            request.Prompt,
            request.Width,
            request.Height,
            request.Duration
        );

        return Ok(new { jobId });
    }

    [HttpGet("status/{jobId}")]
    public async Task<IActionResult> GetStatus(string jobId)
    {
        var status = await _soraClient.GetJobStatusAsync(jobId);
        return Ok(status);
    }
}
```

## Configuration

### Environment Variables

Set these environment variables:

```bash
export AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com"
export AZURE_OPENAI_KEY="your-api-key"
export AZURE_OPENAI_DEPLOYMENT="sora"
```

### appsettings.json

```json
{
  "AzureSora": {
    "Endpoint": "https://your-resource.openai.azure.com",
    "ApiKey": "your-api-key",
    "DeploymentName": "sora",
    "ApiVersion": "2024-10-21",
    "HttpTimeout": "00:05:00"
  }
}
```

## Basic Operations

### 1. Submit a Video Job

```csharp
var jobId = await client.SubmitVideoJobAsync(
    prompt: "Your creative prompt here",
    width: 1920,
    height: 1080,
    durationInSeconds: 15,
    aspectRatio: "16:9",
    frameRate: 30,
    seed: 42 // For reproducible results
);
```

### 2. Check Job Status

```csharp
var status = await client.GetJobStatusAsync(jobId);
Console.WriteLine($"Status: {status.Status}");
Console.WriteLine($"Progress: {status.ProgressPercentage}%");
```

### 3. Wait for Completion

```csharp
// Wait with default settings
var videoUri = await client.WaitForCompletionAsync(jobId);

// Or with custom timeout
var videoUri = await client.WaitForCompletionAsync(
    jobId,
    pollIntervalSeconds: 5,
    maxWaitTime: TimeSpan.FromMinutes(30)
);
```

### 4. Download Video

```csharp
await client.DownloadVideoAsync(videoUri, "my-video.mp4");
```

## Using Prompt Enhancement

```csharp
// Inject IPromptEnhancer
var enhancer = serviceProvider.GetRequiredService<IPromptEnhancer>();

// Get suggestions
var suggestions = await enhancer.SuggestPromptsAsync(
    "sunset scene",
    maxSuggestions: 3
);

// Use the enhanced prompt
var enhancedPrompt = suggestions.FirstOrDefault() ?? "sunset scene";
var jobId = await client.SubmitVideoJobAsync(enhancedPrompt, 1920, 1080, 10);
```

## Next Steps

- [Configuration](Configuration) - Learn about all configuration options
- [API Reference](API-Reference) - Explore the complete API
- [Examples](Examples) - See more code examples
- [Error Handling](Error-Handling) - Learn how to handle errors properly 