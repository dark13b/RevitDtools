using RevitDtools.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RevitDtools.Core.Interfaces
{
    /// <summary>
    /// Interface for batch processing multiple DWG files
    /// </summary>
    public interface IBatchProcessor
    {
        /// <summary>
        /// Process multiple DWG files with progress reporting
        /// </summary>
        Task<BatchResult> ProcessMultipleFiles(List<string> filePaths, IProgress<BatchProgress> progress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Process all DWG files in a folder with optional subfolder inclusion
        /// </summary>
        Task<BatchResult> ProcessFolder(string folderPath, bool includeSubfolders, IProgress<BatchProgress> progress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancel the current batch processing operation
        /// </summary>
        void CancelProcessing();

        /// <summary>
        /// Get the current processing status
        /// </summary>
        BatchProcessingStatus GetProcessingStatus();
    }

    /// <summary>
    /// Enumeration for batch processing status
    /// </summary>
    public enum BatchProcessingStatus
    {
        NotStarted,
        Running,
        Paused,
        Completed,
        Cancelled,
        Failed
    }
}