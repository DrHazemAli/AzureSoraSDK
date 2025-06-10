# Troubleshooting

This guide helps you resolve common issues when using the AzureSoraSDK.

## Common Issues

### Authentication Errors

#### Issue: "Authentication failed. Please check your API key."

**Symptoms:**
- `SoraAuthenticationException` thrown
- HTTP 401 or 403 responses

**Solutions:**

1. **Verify API Key**
   ```csharp
   // Check if API key is set correctly
   Console.WriteLine($"API Key starts with: {apiKey.Substring(0, 4)}...");
   ```

2. **Check Endpoint URL**
   ```csharp
   // Ensure endpoint is correct format
   // Correct: https://your-resource.openai.azure.com
   // Wrong: https://your-resource.openai.azure.com/
   ```

3. **Verify Deployment Exists**
   - Log into Azure Portal
   - Navigate to your Azure OpenAI resource
   - Check that the deployment name matches

4. **Check Permissions**
   - Ensure your API key has "Cognitive Services OpenAI User" role
   - Verify resource is not in a private network

### Validation Errors

#### Issue: "Width and height must be divisible by 8"

**Solution:**
```csharp
// Ensure dimensions are divisible by 8
int width = 1920;  // ✓ Divisible by 8
int height = 1080; // ✓ Divisible by 8

// Helper function
int RoundToNearest8(int value)
{
    return (int)Math.Round(value / 8.0) * 8;
}
```

#### Issue: "Duration must be between 1 and 60 seconds"

**Solution:**
```csharp
// Validate duration
int duration = Math.Max(1, Math.Min(60, requestedDuration));
```

#### Issue: "Prompt cannot be empty"

**Solution:**
```csharp
// Validate prompt
if (string.IsNullOrWhiteSpace(prompt))
{
    throw new ArgumentException("Prompt is required");
}
```

### Rate Limiting

#### Issue: "Rate limit exceeded"

**Symptoms:**
- `SoraRateLimitException` thrown
- HTTP 429 responses

**Solutions:**

1. **Implement Exponential Backoff**
   ```csharp
   int retryCount = 0;
   while (retryCount < 3)
   {
       try
       {
           return await SubmitVideoJobAsync(...);
       }
       catch (SoraRateLimitException ex)
       {
           if (ex.RetryAfter.HasValue)
           {
               await Task.Delay(ex.RetryAfter.Value);
           }
           else
           {
               await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
           }
           retryCount++;
       }
   }
   ```

2. **Implement Request Queuing**
   ```csharp
   public class RateLimitedQueue
   {
       private readonly SemaphoreSlim _semaphore = new(5); // 5 concurrent requests
       
       public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
       {
           await _semaphore.WaitAsync();
           try
           {
               return await operation();
           }
           finally
           {
               _semaphore.Release();
           }
       }
   }
   ```

### Timeout Issues

#### Issue: "Operation timed out"

**Symptoms:**
- `SoraTimeoutException` thrown
- Jobs taking longer than expected

**Solutions:**

1. **Increase Timeout Configuration**
   ```json
   {
     "AzureSora": {
       "HttpTimeout": "00:15:00",
       "MaxWaitTime": "01:00:00"
     }
   }
   ```

2. **Check Job Status Separately**
   ```csharp
   try
   {
       var videoUrl = await WaitForCompletionAsync(jobId, 
           maxWaitTime: TimeSpan.FromMinutes(5));
   }
   catch (SoraTimeoutException)
   {
       // Continue checking status manually
       var status = await GetJobStatusAsync(jobId);
       if (status.Status == JobStatus.Running)
       {
           // Job is still processing, check again later
       }
   }
   ```

### Network Issues

#### Issue: "Network error occurred"

**Solutions:**

1. **Check Internet Connection**
   ```csharp
   try
   {
       var client = new HttpClient();
       var response = await client.GetAsync("https://www.microsoft.com");
       Console.WriteLine($"Network check: {response.IsSuccessStatusCode}");
   }
   catch (Exception ex)
   {
       Console.WriteLine($"Network issue: {ex.Message}");
   }
   ```

2. **Configure Proxy Settings**
   ```csharp
   services.AddHttpClient<ISoraClient, SoraClient>()
       .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
       {
           Proxy = new WebProxy("http://proxy.company.com:8080"),
           UseProxy = true
       });
   ```

### Job Not Found

#### Issue: "Job not found"

**Symptoms:**
- `SoraNotFoundException` thrown
- Job ID doesn't exist

**Possible Causes:**
1. Job ID is incorrect
2. Job has expired (older than retention period)
3. Wrong Azure OpenAI resource

**Solution:**
```csharp
// Store job IDs immediately after creation
var jobId = await SubmitVideoJobAsync(...);
await SaveJobIdToDatabase(jobId);

// Verify job ID format
if (!Regex.IsMatch(jobId, @"^[a-zA-Z0-9\-]+$"))
{
    throw new ArgumentException("Invalid job ID format");
}
```

## Debugging Tips

### Enable Detailed Logging

```csharp
// Configure logging
services.AddLogging(builder =>
{
    builder.SetMinimumLevel(LogLevel.Debug);
    builder.AddConsole();
    builder.AddDebug();
    builder.AddFilter("AzureSoraSDK", LogLevel.Trace);
});
```

### Use HTTP Logging

```csharp
services.AddHttpClient<ISoraClient, SoraClient>()
    .AddHttpMessageHandler(() => new LoggingHandler());

public class LoggingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Request: {request.Method} {request.RequestUri}");
        
        var response = await base.SendAsync(request, cancellationToken);
        
        Console.WriteLine($"Response: {response.StatusCode}");
        
        return response;
    }
}
```

### Capture Request/Response Details

```csharp
try
{
    var jobId = await client.SubmitVideoJobAsync(...);
}
catch (SoraException ex)
{
    Console.WriteLine($"Error Code: {ex.ErrorCode}");
    Console.WriteLine($"Status Code: {ex.StatusCode}");
    Console.WriteLine($"Message: {ex.Message}");
    
    // Log full exception details
    File.WriteAllText("error_log.txt", ex.ToString());
}
```

## Performance Issues

### Slow Video Generation

**Solutions:**

1. **Optimize Video Parameters**
   ```csharp
   // Lower resolution for faster generation
   var jobId = await client.SubmitVideoJobAsync(
       prompt: prompt,
       width: 1280,  // Instead of 1920
       height: 720,  // Instead of 1080
       durationInSeconds: 5  // Shorter duration
   );
   ```

2. **Parallel Processing**
   ```csharp
   var tasks = prompts.Select(prompt => 
       client.SubmitVideoJobAsync(prompt, 1280, 720, 5)
   ).ToList();
   
   var jobIds = await Task.WhenAll(tasks);
   ```

### Memory Issues

**Solution:**
```csharp
// Stream large files instead of loading into memory
await using var fileStream = new FileStream(
    filePath, 
    FileMode.Create, 
    FileAccess.Write,
    FileShare.None,
    bufferSize: 81920, // 80KB buffer
    useAsync: true);

await response.Content.CopyToAsync(fileStream);
```

## Environment-Specific Issues

### Docker Container

```dockerfile
# Ensure certificates are installed
FROM mcr.microsoft.com/dotnet/aspnet:6.0
RUN apt-get update && apt-get install -y ca-certificates

# Set environment variables
ENV AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com"
ENV AZURE_OPENAI_KEY="your-key"
```

### Azure App Service

```json
// Application Settings
{
  "AZURE_OPENAI_ENDPOINT": "https://your-resource.openai.azure.com",
  "AZURE_OPENAI_KEY": "@Microsoft.KeyVault(VaultName=myvault;SecretName=openai-key)"
}
```

## Getting Help

If you're still experiencing issues:

1. **Check the GitHub Issues**
   - Search [existing issues](https://github.com/DrHazemAli/AzureSoraSDK/issues)
   - Include error messages in your search

2. **Create a New Issue**
   Include:
   - SDK version
   - .NET version
   - Complete error message
   - Code sample (without secrets)
   - Steps to reproduce

3. **Enable Debug Logging**
   ```csharp
   var logger = LoggerFactory
       .Create(builder => builder.AddConsole())
       .CreateLogger<SoraClient>();
   
   var client = new SoraClient(httpClient, options, logger);
   ```

## Next Steps

- [Error Handling](Error-Handling) - Learn about exception types
- [Configuration](Configuration) - Adjust settings for your needs
- [Examples](Examples) - See working code examples 