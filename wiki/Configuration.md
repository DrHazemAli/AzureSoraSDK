# Configuration

This page provides detailed information about configuring the AzureSoraSDK.

## Configuration Options

### SoraClientOptions

The main configuration class with all available options:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Endpoint` | string | Required | Azure OpenAI endpoint URL |
| `ApiKey` | string | Required | API key for authentication |
| `DeploymentName` | string | Required | Name of your Sora deployment |
| `ApiVersion` | string | "2024-10-21" | Azure OpenAI API version |
| `HttpTimeout` | TimeSpan | 5 minutes | HTTP request timeout |
| `MaxRetryAttempts` | int | 3 | Maximum retry attempts for failed requests |
| `RetryDelay` | TimeSpan | 2 seconds | Base delay between retries (exponential backoff) |
| `DefaultPollingInterval` | TimeSpan | 5 seconds | Default interval for job status polling |
| `MaxWaitTime` | TimeSpan | 1 hour | Maximum time to wait for job completion |

## Configuration Methods

### 1. Using appsettings.json

```json
{
  "AzureSora": {
    "Endpoint": "https://your-resource.openai.azure.com",
    "ApiKey": "your-api-key",
    "DeploymentName": "sora",
    "ApiVersion": "2024-10-21",
    "HttpTimeout": "00:10:00",
    "MaxRetryAttempts": 5,
    "RetryDelay": "00:00:03",
    "DefaultPollingInterval": "00:00:10",
    "MaxWaitTime": "00:30:00"
  }
}
```

Load in Program.cs:

```csharp
builder.Services.AddAzureSoraSDK(
    builder.Configuration.GetSection("AzureSora"));
```

### 2. Using Code Configuration

```csharp
builder.Services.AddAzureSoraSDK(options =>
{
    options.Endpoint = "https://your-resource.openai.azure.com";
    options.ApiKey = "your-api-key";
    options.DeploymentName = "sora";
    options.ApiVersion = "2024-10-21";
    options.HttpTimeout = TimeSpan.FromMinutes(10);
    options.MaxRetryAttempts = 5;
    options.RetryDelay = TimeSpan.FromSeconds(3);
    options.DefaultPollingInterval = TimeSpan.FromSeconds(10);
    options.MaxWaitTime = TimeSpan.FromMinutes(30);
});
```

### 3. Using Environment Variables

Set environment variables:

```bash
# Linux/macOS
export AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com"
export AZURE_OPENAI_KEY="your-api-key"
export AZURE_OPENAI_DEPLOYMENT="sora"

# Windows PowerShell
$env:AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com"
$env:AZURE_OPENAI_KEY="your-api-key"
$env:AZURE_OPENAI_DEPLOYMENT="sora"
```

Use in code:

```csharp
var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
var deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT");

using var client = new SoraClient(endpoint, apiKey, deployment);
```

### 4. Using Azure Key Vault

```csharp
// Add Azure Key Vault
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());

// Configure SDK
builder.Services.AddAzureSoraSDK(options =>
{
    options.Endpoint = builder.Configuration["AzureOpenAI:Endpoint"];
    options.ApiKey = builder.Configuration["AzureOpenAI:ApiKey"];
    options.DeploymentName = builder.Configuration["AzureOpenAI:DeploymentName"];
});
```

## Advanced Configuration

### Custom HttpClient Configuration

```csharp
builder.Services.AddHttpClient<ISoraClient, SoraClient>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
    client.DefaultRequestHeaders.Add("X-Custom-Header", "value");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
    MaxConnectionsPerServer = 10
});
```

### Logging Configuration

```csharp
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Information);
    
    // Set specific level for AzureSoraSDK
    logging.AddFilter("AzureSoraSDK", LogLevel.Debug);
});
```

### Polly Retry Policies

The SDK uses Polly for retry logic. You can customize the policies:

```csharp
builder.Services.AddHttpClient<ISoraClient, SoraClient>()
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            6,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retry, context) =>
            {
                var logger = context.Values["logger"] as ILogger;
                logger?.LogWarning($"Retry {retry} after {timespan}");
            }));
```

## Configuration Validation

The SDK validates configuration on startup. Common validation rules:

- `Endpoint` must be a valid HTTPS URL
- `ApiKey` must not be empty
- `DeploymentName` must not be empty
- `HttpTimeout` must be between 1 second and 30 minutes
- `MaxRetryAttempts` must be between 0 and 10
- `RetryDelay` must be between 100ms and 1 minute

### Example Validation Error

```csharp
try
{
    builder.Services.AddAzureSoraSDK(options =>
    {
        options.Endpoint = ""; // Invalid
        options.ApiKey = "key";
        options.DeploymentName = "sora";
    });
}
catch (ValidationException ex)
{
    Console.WriteLine($"Configuration error: {ex.Message}");
    // Output: "The Endpoint field is required."
}
```

## Best Practices

1. **Store secrets securely**: Use Azure Key Vault or environment variables for sensitive data
2. **Use appropriate timeouts**: Adjust `HttpTimeout` based on your video generation needs
3. **Configure retry logic**: Set `MaxRetryAttempts` based on your reliability requirements
4. **Monitor configuration**: Log configuration values (except secrets) on startup
5. **Use dependency injection**: Prefer DI over manual instantiation for better testability

## Configuration for Different Environments

### Development

```json
{
  "AzureSora": {
    "HttpTimeout": "00:15:00",
    "MaxRetryAttempts": 5,
    "DefaultPollingInterval": "00:00:02"
  }
}
```

### Production

```json
{
  "AzureSora": {
    "HttpTimeout": "00:05:00",
    "MaxRetryAttempts": 3,
    "DefaultPollingInterval": "00:00:05"
  }
}
```

## Next Steps

- [API Reference](API-Reference) - Detailed API documentation
- [Error Handling](Error-Handling) - Configure error handling
- [Examples](Examples) - See configuration examples in action 