# Configuration

This page provides detailed information about configuring the AzureSoraSDK.

## Configuration Classes

### SoraClientOptions

The main configuration class for video generation with all available options:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Endpoint` | string | Required | Azure OpenAI endpoint URL for video generation |
| `ApiKey` | string | Required | API key for video generation authentication |
| `DeploymentName` | string | Required | Name of your Sora deployment |
| `ApiVersion` | string | "preview" | Azure OpenAI API version for video generation |
| `HttpTimeout` | TimeSpan | 5 minutes | HTTP request timeout |
| `MaxRetryAttempts` | int | 3 | Maximum retry attempts for failed requests |
| `RetryDelay` | TimeSpan | 2 seconds | Base delay between retries (exponential backoff) |
| `DefaultPollingInterval` | TimeSpan | 5 seconds | Default interval for job status polling |
| `MaxWaitTime` | TimeSpan | 1 hour | Maximum time to wait for job completion |

### PromptEnhancerOptions

The configuration class for prompt enhancement with chat completion models:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Endpoint` | string | Required | Azure OpenAI endpoint URL for chat completions |
| `ApiKey` | string | Required | API key for chat completions authentication |
| `DeploymentName` | string | Required | Name of your chat completion deployment (e.g., gpt-4) |
| `ApiVersion` | string | "2024-02-15-preview" | Azure OpenAI API version for chat completions |
| `HttpTimeout` | TimeSpan | 2 minutes | HTTP request timeout |
| `MaxRetryAttempts` | int | 3 | Maximum retry attempts for failed requests |
| `RetryDelay` | TimeSpan | 1 second | Base delay between retries (exponential backoff) |
| `DefaultTemperature` | double | 0.7 | Temperature for prompt enhancement completions |
| `DefaultTopP` | double | 0.9 | Top-p value for prompt enhancement completions |
| `MaxTokensPerRequest` | int | 1000 | Maximum tokens per prompt enhancement request |

## Configuration Methods

### 1. Separate Configuration (Recommended)

Configure video generation and prompt enhancement with different endpoints and settings:

#### Using appsettings.json

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
    "MaxTokensPerRequest": 1500
  }
}
```

Load in Program.cs:

```csharp
builder.Services.AddAzureSoraSDK(
    builder.Configuration.GetSection("AzureSora"));
```

#### Using Code Configuration

```csharp
builder.Services.AddAzureSoraSDK(
    configureSoraOptions: options =>
    {
        options.Endpoint = "https://your-sora-endpoint.openai.azure.com";
        options.ApiKey = "your-sora-api-key";
        options.DeploymentName = "sora";
        options.ApiVersion = "preview";
        options.HttpTimeout = TimeSpan.FromMinutes(10);
        options.MaxRetryAttempts = 5;
        options.RetryDelay = TimeSpan.FromSeconds(3);
        options.DefaultPollingInterval = TimeSpan.FromSeconds(10);
        options.MaxWaitTime = TimeSpan.FromMinutes(30);
    },
    configurePromptEnhancerOptions: options =>
    {
        options.Endpoint = "https://your-chat-endpoint.openai.azure.com";
        options.ApiKey = "your-chat-api-key";
        options.DeploymentName = "gpt-4";
        options.ApiVersion = "2024-02-15-preview";
        options.HttpTimeout = TimeSpan.FromMinutes(3);
        options.MaxRetryAttempts = 3;
        options.RetryDelay = TimeSpan.FromSeconds(1);
        options.DefaultTemperature = 0.5;
        options.DefaultTopP = 0.8;
        options.MaxTokensPerRequest = 2000;
    });
```

### 2. Shared Configuration (Backward Compatible)

Use the same endpoint and configuration for both services:

#### Using appsettings.json

```json
{
  "AzureSora": {
    "Endpoint": "https://your-resource.openai.azure.com",
    "ApiKey": "your-api-key",
    "DeploymentName": "sora",
    "ApiVersion": "preview",
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

#### Using Code Configuration

```csharp
builder.Services.AddAzureSoraSDK(options =>
{
    options.Endpoint = "https://your-resource.openai.azure.com";
    options.ApiKey = "your-api-key";
    options.DeploymentName = "sora";
    options.ApiVersion = "preview";
    options.HttpTimeout = TimeSpan.FromMinutes(10);
    options.MaxRetryAttempts = 5;
    options.RetryDelay = TimeSpan.FromSeconds(3);
    options.DefaultPollingInterval = TimeSpan.FromSeconds(10);
    options.MaxWaitTime = TimeSpan.FromMinutes(30);
});
```

### 3. Direct Constructor Usage

Create clients directly with specific options:

```csharp
using AzureSoraSDK.Configuration;

// Create SoraClient with video generation options
var soraOptions = new SoraClientOptions
{
    Endpoint = "https://your-sora-endpoint.openai.azure.com",
    ApiKey = "your-sora-api-key",
    DeploymentName = "sora",
    ApiVersion = "preview"
};

var soraClient = new SoraClient(httpClient, soraOptions, logger);

// Create PromptEnhancer with separate options
var promptEnhancerOptions = new PromptEnhancerOptions
{
    Endpoint = "https://your-chat-endpoint.openai.azure.com",
    ApiKey = "your-chat-api-key",
    DeploymentName = "gpt-4",
    ApiVersion = "2024-02-15-preview",
    DefaultTemperature = 0.6,
    MaxTokensPerRequest = 1500
};

var promptEnhancer = new PromptEnhancer(httpClient, promptEnhancerOptions, logger);
```

### 4. Using Environment Variables

Set environment variables for separate services:

```bash
# Linux/macOS - Video Generation
export AZURE_OPENAI_SORA_ENDPOINT="https://your-sora-endpoint.openai.azure.com"
export AZURE_OPENAI_SORA_KEY="your-sora-api-key"
export AZURE_OPENAI_SORA_DEPLOYMENT="sora"

# Linux/macOS - Prompt Enhancement
export AZURE_OPENAI_CHAT_ENDPOINT="https://your-chat-endpoint.openai.azure.com"
export AZURE_OPENAI_CHAT_KEY="your-chat-api-key"
export AZURE_OPENAI_CHAT_DEPLOYMENT="gpt-4"

# Windows PowerShell - Video Generation
$env:AZURE_OPENAI_SORA_ENDPOINT="https://your-sora-endpoint.openai.azure.com"
$env:AZURE_OPENAI_SORA_KEY="your-sora-api-key"
$env:AZURE_OPENAI_SORA_DEPLOYMENT="sora"

# Windows PowerShell - Prompt Enhancement
$env:AZURE_OPENAI_CHAT_ENDPOINT="https://your-chat-endpoint.openai.azure.com"
$env:AZURE_OPENAI_CHAT_KEY="your-chat-api-key"
$env:AZURE_OPENAI_CHAT_DEPLOYMENT="gpt-4"
```

Use in code:

```csharp
var soraEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_SORA_ENDPOINT");
var soraApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_SORA_KEY");
var soraDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_SORA_DEPLOYMENT");

var chatEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_CHAT_ENDPOINT");
var chatApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_CHAT_KEY");
var chatDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_CHAT_DEPLOYMENT");

builder.Services.AddAzureSoraSDK(
    configureSoraOptions: options =>
    {
        options.Endpoint = soraEndpoint;
        options.ApiKey = soraApiKey;
        options.DeploymentName = soraDeployment;
    },
    configurePromptEnhancerOptions: options =>
    {
        options.Endpoint = chatEndpoint;
        options.ApiKey = chatApiKey;
        options.DeploymentName = chatDeployment;
    });
```

### 5. Using Azure Key Vault

```csharp
// Add Azure Key Vault
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());

// Configure SDK with separate secrets
builder.Services.AddAzureSoraSDK(
    configureSoraOptions: options =>
    {
        options.Endpoint = builder.Configuration["AzureOpenAI-Sora-Endpoint"];
        options.ApiKey = builder.Configuration["AzureOpenAI-Sora-ApiKey"];
        options.DeploymentName = builder.Configuration["AzureOpenAI-Sora-DeploymentName"];
    },
    configurePromptEnhancerOptions: options =>
    {
        options.Endpoint = builder.Configuration["AzureOpenAI-Chat-Endpoint"];
        options.ApiKey = builder.Configuration["AzureOpenAI-Chat-ApiKey"];
        options.DeploymentName = builder.Configuration["AzureOpenAI-Chat-DeploymentName"];
    });
```

## Advanced Configuration

### Custom HttpClient Configuration

Configure separate HttpClients for each service:

```csharp
// Configure SoraClient HttpClient
builder.Services.AddHttpClient<ISoraClient, SoraClient>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "MyApp-SoraClient/1.0");
    client.DefaultRequestHeaders.Add("X-Video-Service", "sora");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
    MaxConnectionsPerServer = 5
});

// Configure PromptEnhancer HttpClient
builder.Services.AddHttpClient<IPromptEnhancer, PromptEnhancer>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "MyApp-PromptEnhancer/1.0");
    client.DefaultRequestHeaders.Add("X-Chat-Service", "completion");
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
    
    // Set specific levels for different services
    logging.AddFilter("AzureSoraSDK.SoraClient", LogLevel.Debug);
    logging.AddFilter("AzureSoraSDK.PromptEnhancer", LogLevel.Information);
});
```

### Polly Retry Policies

The SDK uses separate Polly policies for each service. You can customize them:

```csharp
// Custom retry policy for SoraClient (longer delays for video generation)
builder.Services.AddHttpClient<ISoraClient, SoraClient>()
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            5,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retry, context) =>
            {
                var logger = context.Values["logger"] as ILogger;
                logger?.LogWarning($"SoraClient retry {retry} after {timespan}");
            }));

// Custom retry policy for PromptEnhancer (faster retries for chat completions)
builder.Services.AddHttpClient<IPromptEnhancer, PromptEnhancer>()
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            3,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(1.5, retryAttempt)),
            onRetry: (outcome, timespan, retry, context) =>
            {
                var logger = context.Values["logger"] as ILogger;
                logger?.LogWarning($"PromptEnhancer retry {retry} after {timespan}");
            }));
```

## Configuration Validation

The SDK validates both configuration classes on startup. Common validation rules:

### SoraClientOptions Validation
- `Endpoint` must be a valid HTTPS URL
- `ApiKey` must not be empty
- `DeploymentName` must not be empty
- `HttpTimeout` must be between 1 second and 30 minutes
- `MaxRetryAttempts` must be between 0 and 10
- `RetryDelay` must be between 100ms and 1 minute

### PromptEnhancerOptions Validation
- `Endpoint` must be a valid HTTPS URL
- `ApiKey` must not be empty
- `DeploymentName` must not be empty
- `ApiVersion` must not be empty
- `HttpTimeout` must be positive
- `MaxRetryAttempts` must be between 0 and 10
- `RetryDelay` must be positive
- `DefaultTemperature` must be between 0.0 and 2.0
- `DefaultTopP` must be between 0.0 and 1.0
- `MaxTokensPerRequest` must be between 1 and 4096

### Example Validation Error

```csharp
try
{
    builder.Services.AddAzureSoraSDK(
        configureSoraOptions: options =>
        {
            options.Endpoint = ""; // Invalid
            options.ApiKey = "key";
            options.DeploymentName = "sora";
        },
        configurePromptEnhancerOptions: options =>
        {
            options.Endpoint = "https://valid.endpoint.com";
            options.ApiKey = "key";
            options.DeploymentName = "gpt-4";
            options.DefaultTemperature = 3.0; // Invalid - must be <= 2.0
        });
}
catch (ValidationException ex)
{
    Console.WriteLine($"Configuration error: {ex.Message}");
    // Output: Multiple validation errors
}
```

## Best Practices

1. **Use separate endpoints**: Configure different endpoints for video generation and chat completions for optimal performance
2. **Store secrets securely**: Use Azure Key Vault or environment variables for sensitive data
3. **Use appropriate timeouts**: Video generation typically needs longer timeouts than chat completions
4. **Configure retry logic**: Set different retry strategies for different services
5. **Monitor configuration**: Log configuration values (except secrets) on startup
6. **Use dependency injection**: Prefer DI over manual instantiation for better testability
7. **API versions**: Use the latest stable API versions for each service

## Configuration for Different Environments

### Development
Optimized for faster feedback and debugging:

```json
{
  "AzureSora": {
    "HttpTimeout": "00:15:00",
    "MaxRetryAttempts": 5,
    "DefaultPollingInterval": "00:00:02"
  },
  "PromptEnhancer": {
    "HttpTimeout": "00:03:00",
    "MaxRetryAttempts": 5,
    "RetryDelay": "00:00:01",
    "DefaultTemperature": 0.8
  }
}
```

### Production
Optimized for reliability and cost:

```json
{
  "AzureSora": {
    "HttpTimeout": "00:05:00",
    "MaxRetryAttempts": 3,
    "DefaultPollingInterval": "00:00:05"
  },
  "PromptEnhancer": {
    "HttpTimeout": "00:02:00",
    "MaxRetryAttempts": 3,
    "RetryDelay": "00:00:01",
    "DefaultTemperature": 0.7
  }
}
```

## Migration Guide

### From Single Configuration to Separate Configuration

If you're upgrading from a previous version, your existing configuration will continue to work:

```csharp
// Old way (still works)
builder.Services.AddAzureSoraSDK(options =>
{
    options.Endpoint = "https://your-endpoint.openai.azure.com";
    options.ApiKey = "your-api-key";
    options.DeploymentName = "sora";
});

// New way (recommended)
builder.Services.AddAzureSoraSDK(
    configureSoraOptions: options =>
    {
        options.Endpoint = "https://your-sora-endpoint.openai.azure.com";
        options.ApiKey = "your-sora-api-key";
        options.DeploymentName = "sora";
    },
    configurePromptEnhancerOptions: options =>
    {
        options.Endpoint = "https://your-chat-endpoint.openai.azure.com";
        options.ApiKey = "your-chat-api-key";
        options.DeploymentName = "gpt-4";
    });
```

## Next Steps

- [API Reference](API-Reference) - Detailed API documentation
- [Error Handling](Error-Handling) - Configure error handling
- [Examples](Examples) - See configuration examples in action 