# Changelog

All notable changes to the AzureSoraSDK project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.1] - 2025-06-10
### Added
- Wiki documentation
- Changelog
### Fixed
- Fixed incorrect API endpoint URLs for video generation [#1](https://github.com/DrHazemAli/AzureSoraSDK/issues/1)
  - Changed from `/openai/deployments/{deploymentName}/video/jobs` to `/openai/v1/video/generations/jobs`
  - Updated both job submission and status check endpoints to match Azure OpenAI documentation
- Updated unit tests to validate the correct API endpoint format

## [1.0.0]

### Added
- Initial release of AzureSoraSDK
- Core video generation functionality
  - Submit video generation jobs
  - Poll job status with progress tracking
  - Wait for job completion with configurable timeout
  - Download generated videos
- Prompt enhancement capabilities using Azure OpenAI
- Comprehensive error handling with specific exception types
  - `SoraAuthenticationException` for auth failures
  - `SoraValidationException` for invalid parameters
  - `SoraRateLimitException` for rate limiting
  - `SoraTimeoutException` for timeout scenarios
  - `SoraNotFoundException` for missing resources
- Dependency injection support with `IServiceCollection` extensions
- Retry logic with exponential backoff using Polly
- Circuit breaker pattern for fault tolerance
- Integrated Microsoft.Extensions.Logging support
- HttpClient lifecycle management with IHttpClientFactory
- Full nullable reference type support
- Async disposal with IAsyncDisposable
- Configuration validation with data annotations
- Thread-safe operations
- Extensive unit test coverage with xUnit, Moq, and FluentAssertions

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

[1.0.0]: https://github.com/DrHazemAli/AzureSoraSDK/releases/tag/v1.0.0 