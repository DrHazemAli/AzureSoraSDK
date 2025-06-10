# Error Handling

This guide covers how to properly handle errors when using the AzureSoraSDK.

## Exception Hierarchy

The SDK uses a structured exception hierarchy for different error scenarios:

```
Exception
└── SoraException (base SDK exception)
    ├── SoraAuthenticationException
    ├── SoraValidationException
    ├── SoraRateLimitException
    ├── SoraTimeoutException
    └── SoraNotFoundException
```

## Exception Types

### SoraException

Base exception for all SDK-specific errors.

```csharp
public class SoraException : Exception
{
    public string? ErrorCode { get; }
    public HttpStatusCode? StatusCode { get; }
}
```

**When thrown:** Base class for all SDK exceptions
**Action:** Check the specific exception type

### SoraAuthenticationException

Authentication or authorization failures.

```csharp
catch (SoraAuthenticationException ex)
{
    // Invalid API key or insufficient permissions
    Console.WriteLine("Authentication failed: " + ex.Message);
    
    // Actions:
    // 1. Verify API key is correct
    // 2. Check Azure OpenAI permissions
    // 3. Ensure the deployment exists
}
```

**Common causes:**
- Invalid or expired API key
- Incorrect endpoint URL
- Missing permissions on the Azure resource
- Deployment doesn't exist or is inaccessible

### SoraValidationException

Request validation failures.

```csharp
catch (SoraValidationException ex)
{
    Console.WriteLine("Validation failed: " + ex.Message);
    
    // Check specific validation errors
    if (ex.ValidationErrors != null)
    {
        foreach (var error in ex.ValidationErrors)
        {
            Console.WriteLine($"{error.Key}: {string.Join(", ", error.Value)}");
        }
    }
}
```

**Common validation errors:**
- Width/height not divisible by 8
- Duration outside allowed range (1-60 seconds)
- Empty or null prompt
- Invalid aspect ratio format
- Frame rate outside allowed range

### SoraRateLimitException

Rate limiting errors with retry information.

```csharp
catch (SoraRateLimitException ex)
{
    Console.WriteLine("Rate limit exceeded");
    
    if (ex.RetryAfter.HasValue)
    {
        Console.WriteLine($"Retry after: {ex.RetryAfter.Value}");
        await Task.Delay(ex.RetryAfter.Value);
        
        // Retry the operation
    }
}
```

**Handling strategies:**
- Implement exponential backoff
- Queue requests for later processing
- Use the `RetryAfter` value when provided

### SoraTimeoutException

Operation timeout errors.

```csharp
catch (SoraTimeoutException ex)
{
    Console.WriteLine($"Operation timed out after {ex.Timeout}");
    
    // Actions:
    // 1. Check job status manually
    // 2. Increase timeout configuration
    // 3. Implement retry logic
}
```

**Common scenarios:**
- Job takes longer than expected
- Network connectivity issues
- Azure service delays

### SoraNotFoundException

Resource not found errors.

```csharp
catch (SoraNotFoundException ex)
{
    Console.WriteLine($"Resource not found: {ex.ResourceId}");
    
    // The job ID doesn't exist or has expired
}
```

## Error Handling Patterns

### Basic Try-Catch

```csharp
public async Task<string> GenerateVideoBasic(string prompt)
{
    try
    {
        var jobId = await _soraClient.SubmitVideoJobAsync(
            prompt, 1920, 1080, 10
        );
        
        return await _soraClient.WaitForCompletionAsync(jobId);
    }
    catch (SoraException ex)
    {
        _logger.LogError(ex, "Video generation failed");
        throw new ApplicationException("Failed to generate video", ex);
    }
}
```

### Specific Exception Handling

```csharp
public async Task<VideoResult> GenerateVideoWithHandling(VideoRequest request)
{
    try
    {
        var jobId = await _soraClient.SubmitVideoJobAsync(
            request.Prompt,
            request.Width,
            request.Height,
            request.Duration
        );
        
        var videoUrl = await _soraClient.WaitForCompletionAsync(jobId);
        
        return new VideoResult 
        { 
            Success = true, 
            VideoUrl = videoUrl.ToString() 
        };
    }
    catch (SoraValidationException ex)
    {
        return new VideoResult 
        { 
            Success = false, 
            Error = "Invalid request parameters",
            Details = ex.ValidationErrors
        };
    }
    catch (SoraAuthenticationException)
    {
        return new VideoResult 
        { 
            Success = false, 
            Error = "Authentication failed. Please check your credentials."
        };
    }
    catch (SoraRateLimitException ex)
    {
        return new VideoResult 
        { 
            Success = false, 
            Error = "Too many requests. Please try again later.",
            RetryAfter = ex.RetryAfter
        };
    }
    catch (SoraTimeoutException)
    {
        return new VideoResult 
        { 
            Success = false, 
            Error = "The operation timed out. The video may still be processing."
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error");
        return new VideoResult 
        { 
            Success = false, 
            Error = "An unexpected error occurred."
        };
    }
}
```

### Retry Pattern with Polly

```csharp
public class ResilientVideoService
{
    private readonly ISoraClient _soraClient;
    private readonly IAsyncPolicy<string> _retryPolicy;
    
    public ResilientVideoService(ISoraClient soraClient)
    {
        _soraClient = soraClient;
        
        // Create retry policy
        _retryPolicy = Policy<string>
            .Handle<SoraException>(ex => !(ex is SoraValidationException))
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retry, context) =>
                {
                    var ex = outcome.Exception;
                    Console.WriteLine($"Retry {retry} after {timespan} - {ex?.Message}");
                });
    }
    
    public async Task<string> GenerateVideoWithRetry(string prompt)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var jobId = await _soraClient.SubmitVideoJobAsync(
                prompt, 1920, 1080, 10
            );
            
            return await _soraClient.WaitForCompletionAsync(jobId);
        });
    }
}
```

### Circuit Breaker Pattern

```csharp
public class CircuitBreakerVideoService
{
    private readonly ISoraClient _soraClient;
    private readonly IAsyncPolicy<string> _circuitBreakerPolicy;
    
    public CircuitBreakerVideoService(ISoraClient soraClient)
    {
        _soraClient = soraClient;
        
        _circuitBreakerPolicy = Policy<string>
            .Handle<SoraException>()
            .CircuitBreakerAsync(
                3, // Number of exceptions before opening circuit
                TimeSpan.FromMinutes(1), // Duration of break
                onBreak: (result, duration) =>
                {
                    Console.WriteLine($"Circuit breaker opened for {duration}");
                },
                onReset: () =>
                {
                    Console.WriteLine("Circuit breaker reset");
                });
    }
    
    public async Task<string> GenerateVideoWithCircuitBreaker(string prompt)
    {
        try
        {
            return await _circuitBreakerPolicy.ExecuteAsync(async () =>
            {
                var jobId = await _soraClient.SubmitVideoJobAsync(
                    prompt, 1920, 1080, 10
                );
                
                return await _soraClient.WaitForCompletionAsync(jobId);
            });
        }
        catch (BrokenCircuitException)
        {
            throw new ServiceUnavailableException(
                "Video service is temporarily unavailable"
            );
        }
    }
}
```

## Global Error Handling

### ASP.NET Core Middleware

```csharp
public class SoraExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SoraExceptionMiddleware> _logger;
    
    public SoraExceptionMiddleware(
        RequestDelegate next, 
        ILogger<SoraExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        _logger.LogError(ex, "An error occurred");
        
        var response = context.Response;
        response.ContentType = "application/json";
        
        var (statusCode, message) = ex switch
        {
            SoraValidationException => (400, "Invalid request parameters"),
            SoraAuthenticationException => (401, "Authentication failed"),
            SoraRateLimitException => (429, "Rate limit exceeded"),
            SoraNotFoundException => (404, "Resource not found"),
            SoraTimeoutException => (504, "Operation timed out"),
            SoraException => (500, "Video generation failed"),
            _ => (500, "An error occurred")
        };
        
        response.StatusCode = statusCode;
        
        var errorResponse = new
        {
            error = new
            {
                message = message,
                details = ex.Message,
                type = ex.GetType().Name
            }
        };
        
        await response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }
}

// Register in Program.cs
app.UseMiddleware<SoraExceptionMiddleware>();
```

## Best Practices

1. **Always catch specific exceptions first**
   ```csharp
   catch (SoraValidationException ex) { }
   catch (SoraException ex) { }
   catch (Exception ex) { }
   ```

2. **Log errors with context**
   ```csharp
   _logger.LogError(ex, "Failed to generate video for prompt: {Prompt}", prompt);
   ```

3. **Don't retry validation errors**
   ```csharp
   if (ex is SoraValidationException)
       return; // Don't retry
   ```

4. **Respect rate limits**
   ```csharp
   if (ex is SoraRateLimitException rateLimitEx && rateLimitEx.RetryAfter.HasValue)
       await Task.Delay(rateLimitEx.RetryAfter.Value);
   ```

5. **Provide meaningful error messages to users**
   ```csharp
   var userMessage = GetUserFriendlyMessage(ex);
   ```

## Monitoring and Alerting

```csharp
public class MonitoredVideoService
{
    private readonly ISoraClient _soraClient;
    private readonly ILogger<MonitoredVideoService> _logger;
    private readonly IMetrics _metrics;
    
    public async Task<string> GenerateVideoWithMonitoring(string prompt)
    {
        using var activity = Activity.StartActivity("GenerateVideo");
        
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            var jobId = await _soraClient.SubmitVideoJobAsync(
                prompt, 1920, 1080, 10
            );
            
            var videoUrl = await _soraClient.WaitForCompletionAsync(jobId);
            
            _metrics.RecordSuccess("video_generation", stopwatch.Elapsed);
            
            return videoUrl;
        }
        catch (SoraException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            _metrics.RecordFailure("video_generation", ex.GetType().Name);
            
            _logger.LogError(ex, 
                "Video generation failed. Type: {ExceptionType}, Code: {ErrorCode}",
                ex.GetType().Name, ex.ErrorCode);
            
            throw;
        }
    }
}
```

## Next Steps

- [Troubleshooting](Troubleshooting) - Common issues and solutions
- [Examples](Examples) - See error handling in action
- [Configuration](Configuration) - Configure retry and timeout settings 