using RevitDtools.Core.Models;
using System;
using System.Collections.Generic;

namespace RevitDtools.Core.Interfaces
{
    /// <summary>
    /// Interface for comprehensive logging and diagnostics
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Log an informational message
        /// </summary>
        void LogInfo(string message, string context = null);

        /// <summary>
        /// Log a warning message
        /// </summary>
        void LogWarning(string message, string context = null);

        /// <summary>
        /// Log an error with exception details
        /// </summary>
        void LogError(Exception exception, string context = null);

        /// <summary>
        /// Log an error message without exception
        /// </summary>
        void LogError(string message, string context = null);

        /// <summary>
        /// Log command usage for performance monitoring
        /// </summary>
        void LogUsage(string command, Dictionary<string, object> parameters = null);

        /// <summary>
        /// Generate a comprehensive processing report
        /// </summary>
        ProcessingReport GenerateReport(ProcessingSession session);

        /// <summary>
        /// Export logs to a specified file
        /// </summary>
        void ExportLogs(string filePath, DateTime? fromDate = null);

        /// <summary>
        /// Get recent log entries
        /// </summary>
        List<LogEntry> GetRecentLogs(int count = 100);

        /// <summary>
        /// Clear all log entries
        /// </summary>
        void ClearLogs();

        /// <summary>
        /// Enable or disable verbose logging for troubleshooting
        /// </summary>
        void SetVerboseMode(bool enabled);
    }
}