using System;
using System.Net.Http;
using AzureSoraSDK.Configuration;
using AzureSoraSDK.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace AzureSoraSDK.Extensions
{
    /// <summary>
    /// Extension methods for registering AzureSoraSDK services
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds AzureSoraSDK services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">Configuration section containing SoraClientOptions</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAzureSoraSDK(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            services.Configure<SoraClientOptions>(configuration);
            
            // Configure PromptEnhancer with separate options if available, otherwise use SoraClient options
            var promptEnhancerSection = configuration.GetSection("PromptEnhancer");
            if (promptEnhancerSection.Exists())
            {
                services.Configure<PromptEnhancerOptions>(promptEnhancerSection);
            }
            else
            {
                // Fallback to using SoraClient options for backward compatibility
                services.Configure<PromptEnhancerOptions>(options =>
                {
                    var soraOptions = new SoraClientOptions();
                    configuration.Bind(soraOptions);
                    options.Endpoint = soraOptions.Endpoint;
                    options.ApiKey = soraOptions.ApiKey;
                    options.DeploymentName = soraOptions.DeploymentName;
                    options.HttpTimeout = soraOptions.HttpTimeout;
                    options.MaxRetryAttempts = soraOptions.MaxRetryAttempts;
                    options.RetryDelay = soraOptions.RetryDelay;
                    // Use prompt enhancement specific API version
                    options.ApiVersion = "2024-02-15-preview";
                });
            }
            
            return services.AddAzureSoraSDK();
        }

        /// <summary>
        /// Adds AzureSoraSDK services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configureOptions">Action to configure options</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAzureSoraSDK(
            this IServiceCollection services,
            Action<SoraClientOptions> configureOptions)
        {
            services.Configure(configureOptions);
            
            // Configure PromptEnhancer with default options based on SoraClient for backward compatibility
            services.Configure<PromptEnhancerOptions>(options =>
            {
                var soraOptions = new SoraClientOptions();
                configureOptions(soraOptions);
                options.Endpoint = soraOptions.Endpoint;
                options.ApiKey = soraOptions.ApiKey;
                options.DeploymentName = soraOptions.DeploymentName;
                options.HttpTimeout = soraOptions.HttpTimeout;
                options.MaxRetryAttempts = soraOptions.MaxRetryAttempts;
                options.RetryDelay = soraOptions.RetryDelay;
                options.ApiVersion = "2024-02-15-preview";
            });
            
            return services.AddAzureSoraSDK();
        }

        /// <summary>
        /// Adds AzureSoraSDK services to the service collection with separate configuration for PromptEnhancer
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configureSoraOptions">Action to configure SoraClient options</param>
        /// <param name="configurePromptEnhancerOptions">Action to configure PromptEnhancer options</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAzureSoraSDK(
            this IServiceCollection services,
            Action<SoraClientOptions> configureSoraOptions,
            Action<PromptEnhancerOptions> configurePromptEnhancerOptions)
        {
            services.Configure(configureSoraOptions);
            services.Configure(configurePromptEnhancerOptions);
            return services.AddAzureSoraSDK();
        }

        /// <summary>
        /// Adds AzureSoraSDK services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAzureSoraSDK(this IServiceCollection services)
        {
            // Add options validation
            services.AddSingleton<IValidateOptions<SoraClientOptions>, SoraClientOptionsValidator>();
            services.AddSingleton<IValidateOptions<PromptEnhancerOptions>, PromptEnhancerOptionsValidator>();

            // Add HttpClient for SoraClient
            services.AddHttpClient<ISoraClient, SoraClient>((serviceProvider, httpClient) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<SoraClientOptions>>().Value;
                httpClient.BaseAddress = new Uri(options.Endpoint);
                httpClient.Timeout = options.HttpTimeout;
                httpClient.DefaultRequestHeaders.Add("api-key", options.ApiKey);
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

            // Add HttpClient for PromptEnhancer with separate configuration
            services.AddHttpClient<IPromptEnhancer, PromptEnhancer>((serviceProvider, httpClient) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<PromptEnhancerOptions>>().Value;
                httpClient.BaseAddress = new Uri(options.Endpoint);
                httpClient.Timeout = options.HttpTimeout;
                httpClient.DefaultRequestHeaders.Add("api-key", options.ApiKey);
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddPolicyHandler(GetPromptEnhancerRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

            return services;
        }

        /// <summary>
        /// Creates the retry policy for HTTP requests
        /// </summary>
        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        // Logging is handled by the individual client classes
                    });
        }

        /// <summary>
        /// Creates the retry policy for PromptEnhancer HTTP requests
        /// </summary>
        private static IAsyncPolicy<HttpResponseMessage> GetPromptEnhancerRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(1.5, retryAttempt)), // Slightly different retry pattern
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        // Logging is handled by the PromptEnhancer class
                    });
        }

        /// <summary>
        /// Creates the circuit breaker policy for HTTP requests
        /// </summary>
        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    5,
                    TimeSpan.FromSeconds(30),
                    onBreak: (result, timespan) =>
                    {
                        // Circuit breaker opened
                    },
                    onReset: () =>
                    {
                        // Circuit breaker closed
                    });
        }

        /// <summary>
        /// Options validator for SoraClientOptions
        /// </summary>
        private class SoraClientOptionsValidator : IValidateOptions<SoraClientOptions>
        {
            public ValidateOptionsResult Validate(string? name, SoraClientOptions options)
            {
                try
                {
                    options.Validate();
                    return ValidateOptionsResult.Success;
                }
                catch (Exception ex)
                {
                    return ValidateOptionsResult.Fail(ex.Message);
                }
            }
        }

        /// <summary>
        /// Options validator for PromptEnhancerOptions
        /// </summary>
        private class PromptEnhancerOptionsValidator : IValidateOptions<PromptEnhancerOptions>
        {
            public ValidateOptionsResult Validate(string? name, PromptEnhancerOptions options)
            {
                try
                {
                    options.Validate();
                    return ValidateOptionsResult.Success;
                }
                catch (Exception ex)
                {
                    return ValidateOptionsResult.Fail(ex.Message);
                }
            }
        }
    }
} 