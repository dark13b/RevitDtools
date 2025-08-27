using RevitDtools.Core.Interfaces;
using RevitDtools.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RevitDtools.Utilities
{
    /// <summary>
    /// Comprehensive logging system for RevitDtools
    /// </summary>
    public class Logger : ILogger
    {
        private static readonly Lazy<Logger> _instance = new Lazy<Logger>(() => new Logger());
        public static Logger Instance => _instance.Value;

        private readonly string _logDirectory;
        private readonly string _logFilePath;
        private readonly ConcurrentQueue<LogEntry> _logBuffer;
        private readonly object _fileLock = new object();
        private bool _verboseMode = false;
        private LogLevel _minimumLogLevel = LogLevel.Info;
        private readonly int _maxLogEntries = 10000;

        // Static convenience methods
        public static void LogInfo(string message, string context = null) => ((ILogger)Instance).LogInfo(message, context);
        public static void LogWarning(string message, string context = null) => ((ILogger)Instance).LogWarning(message, context);
        public static void LogError(Exception exception, string context = null) => ((ILogger)Instance).LogError(exception, context);
        public static void LogError(string message, string context = null) => ((ILogger)Instance).LogError(message, context);
        public static void LogUsage(string command, Dictionary<string, object> parameters = null) => ((ILogger)Instance).LogUsage(command, parameters);
        public static ProcessingReport GenerateReport(ProcessingSession session) => ((ILogger)Instance).GenerateReport(session);
        public static void ExportLogs(string filePath, DateTime? fromDate = null) => ((ILogger)Instance).ExportLogs(filePath, fromDate);
        public static List<LogEntry> GetRecentLogs(int count = 100) => ((ILogger)Instance).GetRecentLogs(count);
        public static void ClearLogs() => ((ILogger)Instance).ClearLogs();
        public static void SetVerboseMode(bool enabled) => ((ILogger)Instance).SetVerboseMode(enabled);

        private Logger()
        {
            _logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RevitDtools", "Logs");

            Directory.CreateDirectory(_logDirectory);

            _logFilePath = Path.Combine(_logDirectory, $"RevitDtools_{DateTime.Now:yyyyMMdd}.log");
            _logBuffer = new ConcurrentQueue<LogEntry>();

            // Log initialization
            WriteLog(LogEntry.CreateInfo("Logger initialized", "Logger"));
        }

        void ILogger.LogInfo(string message, string context)
        {
            WriteLog(LogEntry.CreateInfo(message, context));
        }

        void ILogger.LogWarning(string message, string context)
        {
            WriteLog(LogEntry.CreateWarning(message, context));
        }

        void ILogger.LogError(Exception exception, string context)
        {
            var message = exception?.Message ?? "Unknown error";
            WriteLog(LogEntry.CreateError(message, exception, context));
        }

        void ILogger.LogError(string message, string context)
        {
            WriteLog(LogEntry.CreateError(message, null, context));
        }

        void ILogger.LogUsage(string command, Dictionary<string, object> parameters)
        {
            WriteLog(LogEntry.CreateUsage(command, parameters, "Usage"));
        }

        ProcessingReport ILogger.GenerateReport(ProcessingSession session)
        {
            try
            {
                var report = new ProcessingReport
                {
                    Summary = session.GetSummary(),
                    DetailedResults = session.Results
                };

                // Get related log entries from the session timeframe
                var sessionLogs = GetLogEntriesInTimeRange(session.StartTime, session.EndTime ?? DateTime.Now);
                report.RelatedLogEntries = sessionLogs;

                LogInfo($"Generated processing report for session {session.SessionId}", "Logger");
                return report;
            }
            catch (Exception ex)
            {
                LogError(ex, "GenerateReport");
                throw;
            }
        }

        void ILogger.ExportLogs(string filePath, DateTime? fromDate)
        {
            try
            {
                var logs = GetRecentLogs(int.MaxValue);
                
                if (fromDate.HasValue)
                {
                    logs = logs.Where(l => l.Timestamp >= fromDate.Value).ToList();
                }

                var exportData = new
                {
                    ExportedAt = DateTime.Now,
                    ExportedBy = Environment.UserName,
                    MachineName = Environment.MachineName,
                    FromDate = fromDate,
                    LogCount = logs.Count,
                    Logs = logs
                };

                var json = SerializeToJson(exportData);

                File.WriteAllText(filePath, json);
                LogInfo($"Exported {logs.Count} log entries to {filePath}", "Logger");
            }
            catch (Exception ex)
            {
                LogError(ex, "ExportLogs");
                throw;
            }
        }

        List<LogEntry> ILogger.GetRecentLogs(int count)
        {
            try
            {
                var logs = new List<LogEntry>();
                
                // Get from buffer first
                var bufferArray = _logBuffer.ToArray();
                var bufferLogs = bufferArray.Skip(Math.Max(0, bufferArray.Length - count)).ToList();
                logs.AddRange(bufferLogs);

                // If we need more logs, read from file
                if (logs.Count < count && File.Exists(_logFilePath))
                {
                    var fileLogs = ReadLogsFromFile(count - logs.Count);
                    logs.InsertRange(0, fileLogs);
                }

                return logs.OrderByDescending(l => l.Timestamp).Take(count).ToList();
            }
            catch (Exception ex)
            {
                // Can't log this error as it would cause recursion
                System.Diagnostics.Debug.WriteLine($"Error getting recent logs: {ex.Message}");
                return new List<LogEntry>();
            }
        }

        void ILogger.ClearLogs()
        {
            try
            {
                // Clear buffer
                while (_logBuffer.TryDequeue(out _)) { }

                // Clear log files
                var logFiles = Directory.GetFiles(_logDirectory, "RevitDtools_*.log");
                foreach (var file in logFiles)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Could not delete log file {file}: {ex.Message}");
                    }
                }

                LogInfo("Log files cleared", "Logger");
            }
            catch (Exception ex)
            {
                LogError(ex, "ClearLogs");
            }
        }

        void ILogger.SetVerboseMode(bool enabled)
        {
            _verboseMode = enabled;
            _minimumLogLevel = enabled ? LogLevel.Debug : LogLevel.Info;
            LogInfo($"Verbose mode {(enabled ? "enabled" : "disabled")}", "Logger");
        }

        private void WriteLog(LogEntry entry)
        {
            try
            {
                // Check if we should log this entry based on level
                if (entry.Level < _minimumLogLevel)
                {
                    return;
                }

                // Add to buffer
                _logBuffer.Enqueue(entry);

                // Maintain buffer size
                while (_logBuffer.Count > _maxLogEntries)
                {
                    _logBuffer.TryDequeue(out _);
                }

                // Write to file
                WriteToFile(entry);

                // Write to debug output in verbose mode
                if (_verboseMode)
                {
                    System.Diagnostics.Debug.WriteLine(entry.ToString());
                }
            }
            catch (Exception ex)
            {
                // Fallback to debug output if logging fails
                System.Diagnostics.Debug.WriteLine($"Logging failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Original message: {entry}");
            }
        }

        private void WriteToFile(LogEntry entry)
        {
            try
            {
                lock (_fileLock)
                {
                    var logLine = FormatLogEntry(entry);
                    File.AppendAllText(_logFilePath, logLine + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }

        private string FormatLogEntry(LogEntry entry)
        {
            var formatted = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level}] [{entry.UserName}@{entry.MachineName}] {entry.Message}";
            
            if (!string.IsNullOrEmpty(entry.Context))
            {
                formatted += $" (Context: {entry.Context})";
            }

            if (!string.IsNullOrEmpty(entry.StackTrace))
            {
                formatted += $"{Environment.NewLine}Stack Trace: {entry.StackTrace}";
            }

            if (entry.AdditionalData.Count > 0)
            {
                try
                {
                    var additionalJson = SerializeToJson(entry.AdditionalData);
                    formatted += $"{Environment.NewLine}Additional Data: {additionalJson}";
                }
                catch
                {
                    formatted += $"{Environment.NewLine}Additional Data: [Serialization failed]";
                }
            }

            return formatted;
        }

        private List<LogEntry> ReadLogsFromFile(int count)
        {
            var logs = new List<LogEntry>();
            
            try
            {
                if (!File.Exists(_logFilePath))
                {
                    return logs;
                }

                var lines = File.ReadAllLines(_logFilePath);
                var recentLines = lines.Skip(Math.Max(0, lines.Length - (count * 2))).ToArray(); // Take more lines to account for multi-line entries

                LogEntry currentEntry = null;
                
                foreach (var line in recentLines)
                {
                    if (TryParseLogLine(line, out var entry))
                    {
                        if (currentEntry != null)
                        {
                            logs.Add(currentEntry);
                        }
                        currentEntry = entry;
                    }
                    else if (currentEntry != null)
                    {
                        // This is likely a continuation line (stack trace, etc.)
                        currentEntry.Message += Environment.NewLine + line;
                    }
                }

                if (currentEntry != null)
                {
                    logs.Add(currentEntry);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading logs from file: {ex.Message}");
            }

            return logs.Skip(Math.Max(0, logs.Count - count)).ToList();
        }

        private bool TryParseLogLine(string line, out LogEntry entry)
        {
            entry = null;
            
            try
            {
                // Simple parsing - look for timestamp pattern
                if (line.Length < 25 || !line.StartsWith("["))
                {
                    return false;
                }

                var parts = line.Split(new[] { "] [" }, StringSplitOptions.None);
                if (parts.Length < 4)
                {
                    return false;
                }

                var timestampStr = parts[0].Substring(1); // Remove leading [
                var levelStr = parts[1];
                var userMachineStr = parts[2];
                var messageStr = string.Join("] [", parts.Skip(3));
                
                // Remove trailing ] from message
                if (messageStr.EndsWith("]"))
                {
                    messageStr = messageStr.Substring(0, messageStr.Length - 1);
                }

                if (DateTime.TryParse(timestampStr, out var timestamp) &&
                    Enum.TryParse<LogLevel>(levelStr, out var level))
                {
                    entry = new LogEntry
                    {
                        Timestamp = timestamp,
                        Level = level,
                        Message = messageStr
                    };

                    // Parse user@machine
                    var atIndex = userMachineStr.IndexOf('@');
                    if (atIndex > 0)
                    {
                        entry.UserName = userMachineStr.Substring(0, atIndex);
                        entry.MachineName = userMachineStr.Substring(atIndex + 1);
                    }

                    return true;
                }
            }
            catch
            {
                // Parsing failed, return false
            }

            return false;
        }

        private List<LogEntry> GetLogEntriesInTimeRange(DateTime startTime, DateTime endTime)
        {
            var allLogs = GetRecentLogs(int.MaxValue);
            return allLogs.Where(l => l.Timestamp >= startTime && l.Timestamp <= endTime).ToList();
        }

        /// <summary>
        /// Simple JSON serialization for basic objects
        /// </summary>
        private string SerializeToJson(object obj)
        {
            if (obj == null) return "null";
            
            if (obj is string str) return $"\"{str}\"";
            if (obj is bool b) return b.ToString().ToLower();
            if (obj is int || obj is long || obj is double || obj is float) return obj.ToString();
            
            if (obj is Dictionary<string, object> dict)
            {
                var sb = new StringBuilder();
                sb.Append("{");
                var first = true;
                foreach (var kvp in dict)
                {
                    if (!first) sb.Append(",");
                    sb.Append($"\"{kvp.Key}\":{SerializeToJson(kvp.Value)}");
                    first = false;
                }
                sb.Append("}");
                return sb.ToString();
            }
            
            // For complex objects, just return string representation
            return $"\"{obj}\"";
        }

        /// <summary>
        /// Clean up old log files based on retention policy
        /// </summary>
        public void CleanupOldLogs(int retentionDays = 30)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-retentionDays);
                var logFiles = Directory.GetFiles(_logDirectory, "RevitDtools_*.log");

                foreach (var file in logFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        try
                        {
                            File.Delete(file);
                            LogInfo($"Deleted old log file: {Path.GetFileName(file)}", "Logger");
                        }
                        catch (Exception ex)
                        {
                            LogWarning($"Could not delete old log file {Path.GetFileName(file)}: {ex.Message}", "Logger");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "CleanupOldLogs");
            }
        }
    }
}