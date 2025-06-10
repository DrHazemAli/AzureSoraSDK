# Changelog

All notable changes to the AzureSoraSDK project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2025-01-01
### Added
- **BREAKING CHANGE**: Separate configuration for Prompt Enhancer with new `PromptEnhancerOptions` class
  - Independent endpoint and API version configuration for video generation and prompt enhancement
  - New `PromptEnhancerOptions` with dedicated properties for chat completion settings
  - Support for different Azure OpenAI endpoints for each service
  - Configurable temperature, top-p, and max tokens for prompt enhancement
  - Separate HTTP timeout and retry settings optimized for each service
- New dependency injection overload: `AddAzureSoraSDK(configureSoraOptions, configurePromptEnhancerOptions)`
- Separate HTTP client configuration and retry policies for each service
- Enhanced configuration validation for both `SoraClientOptions` and `PromptEnhancerOptions`
- Support for configuration from separate appsettings.json sections
- Backward compatibility with existing single configuration approach

### Changed
- **BREAKING CHANGE**: `PromptEnhancer` constructor now uses `PromptEnhancerOptions` instead of `SoraClientOptions`
- Default API version for prompt enhancement changed to `2024-02-15-preview` (chat completions)
- Default timeout for prompt enhancement reduced to 2 minutes (from 5 minutes)
- Default retry delay for prompt enhancement reduced to 1 second (from 2 seconds)
- Service collection extensions now support separate validation for both configuration types
- Updated retry policies with different strategies for video generation vs. chat completions

### Fixed
- Improved configuration flexibility for different Azure OpenAI service types
- Better resource utilization with service-specific timeouts and retry policies
- Enhanced error handling with service-specific logging

### Documentation
- Updated README.md with separate configuration examples
- Enhanced Configuration.md wiki with comprehensive examples for both configuration approaches
- Updated Examples.md with practical implementations using separate configurations
- Added migration guide for upgrading from single to separate configuration

### Deprecated
- Legacy `PromptEnhancer` constructors marked as `[Obsolete]` but still functional for backward compatibility

### Configuration Options (New)
#### PromptEnhancerOptions
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


## [1.0.2] - 2025-06-10

### Fixed
- Fixed invalid parameters in job creation and status responses to match Azure API:
  - Job creation response now uses `id` field instead of `jobId`
  - Job status response now includes `generations` array with generation IDs
  - Video URL is now properly constructed from generation ID
  - Added support for additional job status values: `queued`, `preprocessing`, `processing`
  - Fixed timestamp fields to use Unix timestamps with underscores (e.g., `created_at`)

### Added
- New aspect ratio calculation utilities:
  - `CalculateDimensionsFromAspectRatio` - Calculates width/height from aspect ratio string
  - `GetCommonDimensions` - Provides common video dimensions for standard aspect ratios
- New `SubmitVideoJobAsync` overload that accepts aspect ratio and quality instead of explicit dimensions
- Support for common aspect ratios: 16:9, 4:3, 1:1, 9:16, 3:4, 21:9
- Quality presets: low, medium, high, ultra

### Changed
- Response DTOs updated to match actual Azure API response format
- Job status tracking now properly handles video URL construction
- Improved error messages for job failures using `failure_reason` field


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