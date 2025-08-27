using System;
using System.Collections.Generic;

namespace RevitDtools.Core.Models
{
    /// <summary>
    /// Result of a geometry processing operation
    /// </summary>
    public class ProcessingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int ElementsProcessed { get; set; }
        public int ElementsSkipped { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public TimeSpan ProcessingTime { get; set; }
        public string Context { get; set; }

        /// <summary>
        /// Create a successful processing result
        /// </summary>
        public static ProcessingResult CreateSuccess(int processed, string message = null)
        {
            return new ProcessingResult
            {
                Success = true,
                ElementsProcessed = processed,
                Message = message ?? $"Successfully processed {processed} elements"
            };
        }

        /// <summary>
        /// Create a failed processing result
        /// </summary>
        public static ProcessingResult CreateFailure(string message, Exception exception = null)
        {
            var result = new ProcessingResult
            {
                Success = false,
                Message = message
            };

            if (exception != null)
            {
                result.Errors.Add($"{exception.GetType().Name}: {exception.Message}");
            }

            return result;
        }

        /// <summary>
        /// Create a skipped processing result
        /// </summary>
        public static ProcessingResult CreateSkipped(string message)
        {
            return new ProcessingResult
            {
                Success = true,
                ElementsSkipped = 1,
                Message = message
            };
        }

        /// <summary>
        /// Create a warning processing result
        /// </summary>
        public static ProcessingResult CreateWarning(string message)
        {
            var result = new ProcessingResult
            {
                Success = true,
                Message = message
            };
            result.Warnings.Add(message);
            return result;
        }

        /// <summary>
        /// Combine multiple processing results
        /// </summary>
        public static ProcessingResult Combine(List<ProcessingResult> results)
        {
            var combined = new ProcessingResult
            {
                Success = true,
                ElementsProcessed = 0,
                ElementsSkipped = 0,
                Warnings = new List<string>(),
                Errors = new List<string>()
            };

            foreach (var result in results)
            {
                combined.ElementsProcessed += result.ElementsProcessed;
                combined.ElementsSkipped += result.ElementsSkipped;
                combined.Warnings.AddRange(result.Warnings);
                combined.Errors.AddRange(result.Errors);
                combined.ProcessingTime = combined.ProcessingTime.Add(result.ProcessingTime);

                if (!result.Success)
                {
                    combined.Success = false;
                }
            }

            combined.Message = $"Combined result: {combined.ElementsProcessed} processed, {combined.ElementsSkipped} skipped";
            if (combined.Errors.Count > 0)
            {
                combined.Message += $", {combined.Errors.Count} errors";
            }

            return combined;
        }
    }
}