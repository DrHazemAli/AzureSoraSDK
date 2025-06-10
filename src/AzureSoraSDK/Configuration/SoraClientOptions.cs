using System;
using System.ComponentModel.DataAnnotations;

namespace AzureSoraSDK.Configuration
{
    /// <summary>
    /// Configuration options for the Sora client
    /// </summary>
    public class SoraClientOptions
    {
        /// <summary>
        /// Azure OpenAI endpoint URL
        /// </summary>
        [Required]
        [Url]
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Azure OpenAI API key
        /// </summary>
        [Required]
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Deployment name for the Sora model
        /// </summary>
        [Required]
        public string DeploymentName { get; set; } = string.Empty;

        /// <summary>
        /// API version to use (default: preview)
        /// </summary>
        public string ApiVersion { get; set; } = "preview";

        /// <summary>
        /// Default timeout for HTTP requests
        /// </summary>
        public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Maximum number of retry attempts for failed requests
        /// </summary>
        [Range(0, 10)]
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Base delay between retry attempts (will be multiplied exponentially)
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Default polling interval for job status checks
        /// </summary>
        public TimeSpan DefaultPollingInterval { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Maximum wait time for job completion
        /// </summary>
        public TimeSpan MaxWaitTime { get; set; } = TimeSpan.FromHours(1);

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

            if (DefaultPollingInterval <= TimeSpan.Zero)
                throw new ArgumentException("DefaultPollingInterval must be positive", nameof(DefaultPollingInterval));

            if (MaxWaitTime <= TimeSpan.Zero)
                throw new ArgumentException("MaxWaitTime must be positive", nameof(MaxWaitTime));
        }
    }
} 