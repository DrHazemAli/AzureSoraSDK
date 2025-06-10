using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace AzureSoraSDK.Models
{
    /// <summary>
    /// Request parameters for video generation
    /// </summary>
    public class VideoGenerationRequest
    {
        /// <summary>
        /// The text prompt describing the video to generate
        /// </summary>
        [Required]
        [MinLength(1)]
        [MaxLength(4000)]
        public string Prompt { get; set; } = string.Empty;

        /// <summary>
        /// Video width in pixels (must be between 128 and 2048)
        /// </summary>
        [Required]
        [Range(128, 2048)]
        public int Width { get; set; }

        /// <summary>
        /// Video height in pixels (must be between 128 and 2048)
        /// </summary>
        [Required]
        [Range(128, 2048)]
        public int Height { get; set; }

        /// <summary>
        /// Duration of the video in seconds (must be between 1 and 60)
        /// </summary>
        [Required]
        [Range(1, 60)]
        public int NSeconds { get; set; }

        /// <summary>
        /// Additional metadata to associate with the job
        /// </summary>
        public Dictionary<string, string>? Metadata { get; set; }

        /// <summary>
        /// Validates the request parameters
        /// </summary>
        public void Validate()
        {
            var validationContext = new ValidationContext(this);
            Validator.ValidateObject(this, validationContext, validateAllProperties: true);

            // Additional custom validation
            if (Width % 8 != 0 || Height % 8 != 0)
            {
                throw new ValidationException("Width and height must be divisible by 8");
            }
        }
    }
} 