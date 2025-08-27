using Autodesk.Revit.DB;
using RevitDtools.Core.Interfaces;
using RevitDtools.Core.Models;
using System;

namespace RevitDtools.Utilities
{
    /// <summary>
    /// Centralized error handling and recovery mechanisms
    /// </summary>
    public static class ErrorHandler
    {
        /// <summary>
        /// Handle geometry processing errors with recovery attempts
        /// </summary>
        public static ProcessingResult HandleGeometryError(Exception ex, string context, ILogger logger = null)
        {
            logger?.LogError(ex, context);

            if (ex is Autodesk.Revit.Exceptions.InvalidOperationException)
            {
                // Attempt recovery with simplified geometry
                return ProcessingResult.CreateFailure(
                    $"Revit operation failed: {ex.Message}. Try simplifying the geometry or checking element validity.",
                    ex);
            }

            if (ex is ArgumentException)
            {
                // Skip invalid element and continue
                return ProcessingResult.CreateFailure(
                    $"Invalid argument: {ex.Message}. Element will be skipped.",
                    ex);
            }

            if (ex is Autodesk.Revit.Exceptions.ArgumentException)
            {
                return ProcessingResult.CreateFailure(
                    $"Revit argument error: {ex.Message}. Check parameter values and element state.",
                    ex);
            }

            if (ex is Autodesk.Revit.Exceptions.InvalidObjectException)
            {
                return ProcessingResult.CreateFailure(
                    $"Invalid Revit object: {ex.Message}. Object may have been deleted or is corrupted.",
                    ex);
            }

            // For unknown errors, fail gracefully
            return ProcessingResult.CreateFailure(
                $"Unexpected error during geometry processing: {ex.Message}",
                ex);
        }

        /// <summary>
        /// Handle family management errors
        /// </summary>
        public static ProcessingResult HandleFamilyError(Exception ex, string context, ILogger logger = null)
        {
            logger?.LogError(ex, context);

            if (ex is Autodesk.Revit.Exceptions.InvalidOperationException)
            {
                return ProcessingResult.CreateFailure(
                    $"Family operation failed: {ex.Message}. Check if family is loaded and editable.",
                    ex);
            }

            if (ex is System.IO.FileNotFoundException)
            {
                return ProcessingResult.CreateFailure(
                    $"Family file not found: {ex.Message}. Verify family file path and accessibility.",
                    ex);
            }

            if (ex is UnauthorizedAccessException)
            {
                return ProcessingResult.CreateFailure(
                    $"Access denied: {ex.Message}. Check file permissions and Revit licensing.",
                    ex);
            }

            return ProcessingResult.CreateFailure(
                $"Family management error: {ex.Message}",
                ex);
        }

        /// <summary>
        /// Handle batch processing errors
        /// </summary>
        public static FileProcessingResult HandleBatchFileError(Exception ex, string filePath, ILogger logger = null)
        {
            logger?.LogError(ex, $"BatchProcessing - {filePath}");

            var result = new FileProcessingResult
            {
                FilePath = filePath,
                FileName = System.IO.Path.GetFileName(filePath),
                Success = false,
                ProcessedAt = DateTime.Now
            };

            if (ex is System.IO.FileNotFoundException)
            {
                result.Message = "File not found";
                result.Errors.Add($"File not found: {filePath}");
            }
            else if (ex is UnauthorizedAccessException)
            {
                result.Message = "Access denied";
                result.Errors.Add($"Access denied to file: {filePath}");
            }
            else if (ex is System.IO.IOException)
            {
                result.Message = "File I/O error";
                result.Errors.Add($"I/O error accessing file: {ex.Message}");
            }
            else if (ex is Autodesk.Revit.Exceptions.InvalidOperationException)
            {
                result.Message = "Revit operation failed";
                result.Errors.Add($"Revit operation failed: {ex.Message}");
            }
            else
            {
                result.Message = "Unexpected error";
                result.Errors.Add($"Unexpected error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Handle settings-related errors
        /// </summary>
        public static void HandleSettingsError(Exception ex, string operation, ILogger logger = null)
        {
            logger?.LogError(ex, $"Settings - {operation}");

            if (ex is System.IO.DirectoryNotFoundException)
            {
                logger?.LogWarning($"Settings directory not found during {operation}. Will create default settings.", "Settings");
            }
            else if (ex is UnauthorizedAccessException)
            {
                logger?.LogError($"Access denied during {operation}. Check permissions for settings directory.", "Settings");
            }
            else if (ex.GetType().Name.Contains("Json"))
            {
                logger?.LogError($"Settings file corrupted during {operation}. Will restore defaults.", "Settings");
            }
            else
            {
                logger?.LogError($"Unexpected error during {operation}: {ex.Message}", "Settings");
            }
        }

        /// <summary>
        /// Determine if an error is recoverable
        /// </summary>
        public static bool IsRecoverableError(Exception ex)
        {
            return ex is ArgumentException ||
                   ex is System.IO.FileNotFoundException ||
                   ex.GetType().Name.Contains("Json") ||
                   ex is System.IO.DirectoryNotFoundException;
        }

        /// <summary>
        /// Get user-friendly error message
        /// </summary>
        public static string GetUserFriendlyMessage(Exception ex)
        {
            switch (ex)
            {
                case Autodesk.Revit.Exceptions.InvalidOperationException _:
                    return "The operation could not be completed. Please check that all elements are valid and the document is not read-only.";
                
                case ArgumentException _:
                    return "Invalid input provided. Please check your selections and try again.";
                
                case System.IO.FileNotFoundException _:
                    return "A required file could not be found. Please verify file paths and accessibility.";
                
                case UnauthorizedAccessException _:
                    return "Access denied. Please check file permissions and ensure Revit has proper licensing.";
                
                case System.IO.IOException _:
                    return "A file system error occurred. Please check disk space and file accessibility.";
                
                case Exception _ when ex.GetType().Name.Contains("Json"):
                    return "Configuration file is corrupted. Settings will be reset to defaults.";
                
                default:
                    return $"An unexpected error occurred: {ex.Message}";
            }
        }

        /// <summary>
        /// Log critical system errors that require immediate attention
        /// </summary>
        public static void LogCriticalError(Exception ex, string context, ILogger logger)
        {
            var criticalEntry = new LogEntry
            {
                Level = LogLevel.Critical,
                Message = $"CRITICAL ERROR: {ex.Message}",
                Context = context,
                StackTrace = ex.StackTrace
            };

            // Add system information for critical errors
            criticalEntry.AdditionalData["ExceptionType"] = ex.GetType().FullName;
            try
            {
                criticalEntry.AdditionalData["RevitVersion"] = "2026"; // Static for now
            }
            catch { }
            criticalEntry.AdditionalData["OSVersion"] = Environment.OSVersion.ToString();
            criticalEntry.AdditionalData["CLRVersion"] = Environment.Version.ToString();

            logger?.LogError(ex, context);

            // Also write to Windows Event Log for critical errors
            try
            {
                System.Diagnostics.EventLog.WriteEntry("RevitDtools", 
                    $"Critical error in {context}: {ex.Message}", 
                    System.Diagnostics.EventLogEntryType.Error);
            }
            catch
            {
                // Ignore if we can't write to event log
            }
        }
    }
}