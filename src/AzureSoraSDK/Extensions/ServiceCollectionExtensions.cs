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

            // Add HttpClient for PromptEnhancer (can share the same HttpClient configuration)
            services.AddHttpClient<IPromptEnhancer, PromptEnhancer>((serviceProvider, httpClient) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<SoraClientOptions>>().Value;
                httpClient.BaseAddress = new Uri(options.Endpoint);
                httpClient.Timeout = options.HttpTimeout;
                httpClient.DefaultRequestHeaders.Add("api-key", options.ApiKey);
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddPolicyHandler(GetRetryPolicy())
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
                        if (context.Values.TryGetValue("logger", out var loggerValue) && 
                            loggerValue is Microsoft.Extensions.Logging.ILogger logger)
                        {
                            logger.LogWarning(
                                "Retry {RetryCount} after {Delay}ms", 
                                retryCount, 
                                timespan.TotalMilliseconds);
                        }
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
    }
} 