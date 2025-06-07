using System.Threading;
using System.Threading.Tasks;

namespace AzureSoraSDK.Interfaces
{
    /// <summary>
    /// Interface for enhancing and suggesting improved prompts
    /// </summary>
    public interface IPromptEnhancer
    {
        /// <summary>
        /// Suggests improved prompts based on a partial or complete prompt
        /// </summary>
        /// <param name="partialPrompt">The initial prompt to enhance</param>
        /// <param name="maxSuggestions">Maximum number of suggestions to return</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Array of suggested prompt improvements</returns>
        Task<string[]> SuggestPromptsAsync(
            string partialPrompt,
            int maxSuggestions = 3,
            CancellationToken cancellationToken = default);
    }
} 