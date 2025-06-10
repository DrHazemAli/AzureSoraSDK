using System;
using System.Threading;
using System.Threading.Tasks;
using AzureSoraSDK.Models;

namespace AzureSoraSDK.Interfaces
{
    /// <summary>
    /// Interface for Azure OpenAI Sora video generation client
    /// </summary>
    public interface ISoraClient : IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// Submits a video generation job to Azure OpenAI Sora
        /// </summary>
        /// <param name="prompt">The text prompt describing the video to generate</param>
        /// <param name="width">Video width in pixels</param>
        /// <param name="height">Video height in pixels</param>
        /// <param name="nSeconds">Video duration in seconds</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The job ID of the submitted video generation job</returns>
        Task<string> SubmitVideoJobAsync(
            string prompt,
            int width,
            int height,
            int nSeconds,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Submits a video generation job using aspect ratio and quality settings
        /// </summary>
        /// <param name="prompt">The text prompt describing the video to generate</param>
        /// <param name="aspectRatio">Aspect ratio (e.g., "16:9", "4:3", "1:1")</param>
        /// <param name="quality">Quality level: "low", "medium", "high", "ultra"</param>
        /// <param name="nSeconds">Video duration in seconds</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The job ID of the submitted video generation job</returns>
        Task<string> SubmitVideoJobAsync(
            string prompt,
            string aspectRatio,
            string quality,
            int nSeconds,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current status of a video generation job
        /// </summary>
        /// <param name="jobId">The job ID to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Job details including status and video URL if completed</returns>
        Task<JobDetails> GetJobStatusAsync(string jobId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Waits for a video generation job to complete
        /// </summary>
        /// <param name="jobId">The job ID to wait for</param>
        /// <param name="pollIntervalSeconds">Polling interval in seconds</param>
        /// <param name="maxWaitTime">Maximum time to wait before timing out</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The URI of the completed video</returns>
        Task<Uri> WaitForCompletionAsync(
            string jobId, 
            int pollIntervalSeconds = 5,
            TimeSpan? maxWaitTime = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads a generated video to a local file
        /// </summary>
        /// <param name="videoUri">The URI of the video to download</param>
        /// <param name="filePath">The local file path to save the video</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task DownloadVideoAsync(Uri videoUri, string filePath, CancellationToken cancellationToken = default);
    }
} 