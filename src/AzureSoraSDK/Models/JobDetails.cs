using System;
using System.Collections.Generic;

namespace AzureSoraSDK.Models
{
    /// <summary>
    /// Contains detailed information about a video generation job
    /// </summary>
    public class JobDetails
    {
        /// <summary>
        /// The unique identifier of the job
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// Current status of the job
        /// </summary>
        public JobStatus Status { get; set; } = JobStatus.Unknown;

        /// <summary>
        /// URL of the generated video (available when status is Succeeded)
        /// </summary>
        public string? VideoUrl { get; set; }

        /// <summary>
        /// Error message if the job failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Error code if the job failed
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// When the job was created
        /// </summary>
        public DateTimeOffset? CreatedAt { get; set; }

        /// <summary>
        /// When the job was last updated
        /// </summary>
        public DateTimeOffset? UpdatedAt { get; set; }

        /// <summary>
        /// When the job completed (either successfully or with failure)
        /// </summary>
        public DateTimeOffset? CompletedAt { get; set; }

        /// <summary>
        /// Progress percentage (0-100) if available
        /// </summary>
        public int? ProgressPercentage { get; set; }

        /// <summary>
        /// Estimated time remaining if available
        /// </summary>
        public TimeSpan? EstimatedTimeRemaining { get; set; }

        /// <summary>
        /// Metadata associated with the job
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }
} 