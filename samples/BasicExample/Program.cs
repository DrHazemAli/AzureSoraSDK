using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureSoraSDK;
using AzureSoraSDK.Configuration;
using AzureSoraSDK.Exceptions;
using AzureSoraSDK.Extensions;
using AzureSoraSDK.Interfaces;
using AzureSoraSDK.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BasicExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Example 1: Basic usage with legacy constructor
            await BasicUsageExample();

            // Example 2: Dependency Injection usage
            await DependencyInjectionExample();
        }

        static async Task BasicUsageExample()
        {
            Console.WriteLine("=== Basic Usage Example ===\n");

            // Get configuration from environment variables
            var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") 
                ?? "https://your-resource.openai.azure.com";
            var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY") 
                ?? "your-api-key";
            var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") 
                ?? "sora";

            try
            {
                using var client = new SoraClient(endpoint, apiKey, deploymentName);

                // Submit a video generation job
                Console.WriteLine("Submitting video generation job...");
                var jobId = await client.SubmitVideoJobAsync(
                    prompt: "A peaceful sunrise over mountain peaks with birds flying, cinematic quality, 10 seconds",
                    width: 1280,
                    height: 720,
                    durationInSeconds: 10,
                    aspectRatio: "16:9",
                    frameRate: 30
                );

                Console.WriteLine($"Job submitted with ID: {jobId}");

                // Wait for completion
                Console.WriteLine("Waiting for video generation to complete...");
                var videoUri = await client.WaitForCompletionAsync(jobId, pollIntervalSeconds: 5);
                
                Console.WriteLine($"Video generated successfully!");
                Console.WriteLine($"Video URL: {videoUri}");

                // Download the video
                var outputPath = "sunrise_mountains.mp4";
                Console.WriteLine($"Downloading video to {outputPath}...");
                await client.DownloadVideoAsync(videoUri, outputPath);
                
                Console.WriteLine($"Video downloaded to: {outputPath}");
            }
            catch (SoraException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                if (ex.ErrorCode != null)
                {
                    Console.WriteLine($"Error Code: {ex.ErrorCode}");
                }
            }

            Console.WriteLine();
        }

        static async Task DependencyInjectionExample()
        {
            Console.WriteLine("=== Dependency Injection Example ===\n");

            // Build the host
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Configure Sora SDK
                    services.AddAzureSoraSDK(options =>
                    {
                        options.Endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") 
                            ?? "https://your-resource.openai.azure.com";
                        options.ApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY") 
                            ?? "your-api-key";
                        options.DeploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") 
                            ?? "sora";
                        options.MaxRetryAttempts = 5;
                        options.HttpTimeout = TimeSpan.FromMinutes(10);
                    });

                    // Add our example service
                    services.AddTransient<VideoGenerationService>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .Build();

            // Get the service and run it
            var videoService = host.Services.GetRequiredService<VideoGenerationService>();
            await videoService.RunAsync();
        }
    }

    public class VideoGenerationService
    {
        private readonly ISoraClient _soraClient;
        private readonly IPromptEnhancer _promptEnhancer;
        private readonly ILogger<VideoGenerationService> _logger;

        public VideoGenerationService(
            ISoraClient soraClient,
            IPromptEnhancer promptEnhancer,
            ILogger<VideoGenerationService> logger)
        {
            _soraClient = soraClient;
            _promptEnhancer = promptEnhancer;
            _logger = logger;
        }

        public async Task RunAsync()
        {
            try
            {
                // Original prompt
                var originalPrompt = "A forest scene";
                _logger.LogInformation("Original prompt: {Prompt}", originalPrompt);

                // Get enhanced prompts
                _logger.LogInformation("Getting prompt suggestions...");
                var suggestions = await _promptEnhancer.SuggestPromptsAsync(originalPrompt, 3);
                
                Console.WriteLine("\nPrompt suggestions:");
                for (int i = 0; i < suggestions.Length; i++)
                {
                    Console.WriteLine($"{i + 1}. {suggestions[i]}");
                }

                // Use the first suggestion or original if none
                var enhancedPrompt = suggestions.Length > 0 ? suggestions[0] : originalPrompt;
                _logger.LogInformation("Using prompt: {Prompt}", enhancedPrompt);

                // Submit video job with detailed configuration
                var request = new VideoGenerationRequest
                {
                    Prompt = enhancedPrompt,
                    Width = 1920,
                    Height = 1080,
                    DurationInSeconds = 15,
                    AspectRatio = "16:9",
                    FrameRate = 30,
                    Quality = "high",
                    Metadata = new Dictionary<string, string>
                    {
                        ["source"] = "example-app",
                        ["version"] = "1.0"
                    }
                };

                // Validate the request
                request.Validate();

                _logger.LogInformation("Submitting video generation job...");
                var jobId = await _soraClient.SubmitVideoJobAsync(
                    request.Prompt,
                    request.Width,
                    request.Height,
                    request.DurationInSeconds,
                    request.AspectRatio,
                    request.FrameRate
                );

                _logger.LogInformation("Job submitted: {JobId}", jobId);

                // Monitor progress
                var completed = false;
                while (!completed)
                {
                    var details = await _soraClient.GetJobStatusAsync(jobId);
                    
                    _logger.LogInformation(
                        "Job {JobId} status: {Status} (Progress: {Progress}%)",
                        jobId,
                        details.Status,
                        details.ProgressPercentage ?? 0
                    );

                    switch (details.Status)
                    {
                        case JobStatus.Succeeded:
                            Console.WriteLine($"\nVideo generated successfully!");
                            Console.WriteLine($"Video URL: {details.VideoUrl}");
                            
                            // Download the video
                            var outputPath = "forest_scene.mp4";
                            await _soraClient.DownloadVideoAsync(new Uri(details.VideoUrl!), outputPath);
                            Console.WriteLine($"Video downloaded to: {outputPath}");
                            
                            completed = true;
                            break;

                        case JobStatus.Failed:
                            _logger.LogError(
                                "Job failed: {ErrorMessage} (Code: {ErrorCode})",
                                details.ErrorMessage,
                                details.ErrorCode
                            );
                            completed = true;
                            break;

                        case JobStatus.Cancelled:
                            _logger.LogWarning("Job was cancelled");
                            completed = true;
                            break;

                        default:
                            await Task.Delay(TimeSpan.FromSeconds(5));
                            break;
                    }
                }
            }
            catch (SoraValidationException ex)
            {
                _logger.LogError(ex, "Validation error occurred");
                if (ex.ValidationErrors != null)
                {
                    foreach (var error in ex.ValidationErrors)
                    {
                        Console.WriteLine($"Validation error in {error.Key}: {string.Join(", ", error.Value)}");
                    }
                }
            }
            catch (SoraRateLimitException ex)
            {
                _logger.LogWarning(ex, "Rate limit exceeded. Retry after: {RetryAfter}", ex.RetryAfter);
            }
            catch (SoraTimeoutException ex)
            {
                _logger.LogError(ex, "Operation timed out after {Timeout}", ex.Timeout);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred");
            }
        }
    }
} 