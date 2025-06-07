namespace AzureSoraSDK.Models
{
    /// <summary>
    /// Represents the status of a video generation job
    /// </summary>
    public enum JobStatus
    {
        /// <summary>
        /// Status is unknown or unrecognized
        /// </summary>
        Unknown = 0,
        
        /// <summary>
        /// Job is currently running
        /// </summary>
        Running = 1,
        
        /// <summary>
        /// Job completed successfully
        /// </summary>
        Succeeded = 2,
        
        /// <summary>
        /// Job failed with an error
        /// </summary>
        Failed = 3,
        
        /// <summary>
        /// Job was cancelled
        /// </summary>
        Cancelled = 4,
        
        /// <summary>
        /// Job is pending and hasn't started yet
        /// </summary>
        Pending = 5
    }
} 