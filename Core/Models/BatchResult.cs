using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitDtools.Core.Models
{
    /// <summary>
    /// Result of a batch processing operation
    /// </summary>
    public class BatchResult
    {
        public List<FileProcessingResult> FileResults { get; set; } = new List<FileProcessingResult>();
        public int TotalFilesProcessed { get; set; }
        public int SuccessfulFiles { get; set; }
        public int FailedFiles { get; set; }
        public TimeSpan TotalProcessingTime { get; set; }
        public DateTime ProcessingStartTime { get; set; }
        public DateTime ProcessingEndTime { get; set; }
        public bool WasCancelled { get; set; }
        public string Summary { get; set; }
        public string ProcessingReport { get; set; }

        /// <summary>
        /// Calculate summary statistics from file results
        /// </summary>
        public void CalculateSummary()
        {
            TotalFilesProcessed = FileResults.Count;
            SuccessfulFiles = FileResults.Count(r => r.Success);
            FailedFiles = FileResults.Count(r => !r.Success);
            TotalProcessingTime = ProcessingEndTime - ProcessingStartTime;

            var totalElements = FileResults.Sum(r => r.ElementsProcessed);
            var totalSkipped = FileResults.Sum(r => r.ElementsSkipped);
            var totalErrors = FileResults.Sum(r => r.Errors.Count);

            Summary = $"Batch processing completed: {SuccessfulFiles}/{TotalFilesProcessed} files successful, " +
                     $"{totalElements} elements processed, {totalSkipped} skipped, {totalErrors} errors";

            if (WasCancelled)
            {
                Summary += " (Operation was cancelled)";
            }
        }
    }

    /// <summary>
    /// Result of processing a single file in a batch operation
    /// </summary>
    public class FileProcessingResult
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public int ElementsProcessed { get; set; }
        public int ElementsSkipped { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public TimeSpan ProcessingTime { get; set; }
        public DateTime ProcessedAt { get; set; }
    }

    /// <summary>
    /// Progress information for batch processing
    /// </summary>
    public class BatchProgress
    {
        public int CurrentFile { get; set; }
        public int TotalFiles { get; set; }
        public string CurrentFileName { get; set; }
        public string CurrentOperation { get; set; }
        public double PercentComplete => TotalFiles > 0 ? (double)CurrentFile / TotalFiles * 100 : 0;
        public TimeSpan ElapsedTime { get; set; }
        public TimeSpan EstimatedTimeRemaining { get; set; }
    }
}