using System;
using System.ComponentModel.DataAnnotations;

namespace AzureSoraSDK.Configuration
{
    /// <summary>
    /// Configuration options for the Prompt Enhancer
    /// </summary>
    public class PromptEnhancerOptions
    {
        /// <summary>
        /// Azure OpenAI endpoint URL for prompt enhancement
        /// </summary>
        [Required]
        [Url]
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Azure OpenAI API key for prompt enhancement
        /// </summary>
        [Required]
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Deployment name for the chat completion model used for prompt enhancement
        /// </summary>
        [Required]
        public string DeploymentName { get; set; } = string.Empty;

        /// <summary>
        /// API version to use for prompt enhancement (default: 2024-02-15-preview)
        /// </summary>
        public string ApiVersion { get; set; } = "2024-02-15-preview";

        /// <summary>
        /// Default timeout for HTTP requests
        /// </summary>
        public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Maximum number of retry attempts for failed requests
        /// </summary>
        [Range(0, 10)]
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Base delay between retry attempts (will be multiplied exponentially)
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Default temperature for prompt enhancement completions
        /// </summary>
        [Range(0.0, 2.0)]
        public double DefaultTemperature { get; set; } = 0.7;

        /// <summary>
        /// Default top-p value for prompt enhancement completions
        /// </summary>
        [Range(0.0, 1.0)]
        public double DefaultTopP { get; set; } = 0.9;

        /// <summary>
        /// Maximum tokens per prompt enhancement request
        /// </summary>
        [Range(1, 4096)]
        public int MaxTokensPerRequest { get; set; } = 1000;

        /// <summary>
        /// Validates the options
        /// </summary>
        public void Validate()
        {
            var validationContext = new ValidationContext(this);
            Validator.ValidateObject(this, validationContext, validateAllProperties: true);

            if (HttpTimeout <= TimeSpan.Zero)
                throw new ArgumentException("HttpTimeout must be positive", nameof(HttpTimeout));

            if (RetryDelay <= TimeSpan.Zero)
                throw new ArgumentException("RetryDelay must be positive", nameof(RetryDelay));

            if (string.IsNullOrWhiteSpace(Endpoint))
                throw new ArgumentException("Endpoint is required", nameof(Endpoint));

            if (string.IsNullOrWhiteSpace(ApiKey))
                throw new ArgumentException("ApiKey is required", nameof(ApiKey));

            if (string.IsNullOrWhiteSpace(DeploymentName))
                throw new ArgumentException("DeploymentName is required", nameof(DeploymentName));

            if (string.IsNullOrWhiteSpace(ApiVersion))
                throw new ArgumentException("ApiVersion is required", nameof(ApiVersion));
        }
    }
} 