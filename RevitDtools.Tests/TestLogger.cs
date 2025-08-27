using RevitDtools.Core.Interfaces;
using RevitDtools.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitDtools.Tests
{
    /// <summary>
    /// Test implementation of ILogger for unit testing
    /// </summary>
    public class TestLogger : ILogger
    {
        private readonly List<LogEntry> _logEntries = new List<LogEntry>();
        private bool _verboseMode = false;

        public List<LogEntry> LogEntries => _logEntries.ToList();
        public int InfoCount => _logEntries.Count(e => e.Level == LogLevel.Info);
        public int WarningCount => _logEntries.Count(e => e.Level == LogLevel.Warning);
        public int ErrorCount => _logEntries.Count(e => e.Level == LogLevel.Error);

        public void LogInfo(string message, string context = null)
        {
            _logEntries.Add(LogEntry.CreateInfo(message, context));
        }

        public void LogWarning(string message, string context = null)
        {
            _logEntries.Add(LogEntry.CreateWarning(message, context));
        }

        public void LogError(Exception exception, string context = null)
        {
            var message = exception?.Message ?? "Unknown error";
            _logEntries.Add(LogEntry.CreateError(message, exception, context));
        }

        public void LogError(string message, string context = null)
        {
            _logEntries.Add(LogEntry.CreateError(message, null, context));
        }

        public void LogUsage(string command, Dictionary<string, object> parameters = null)
        {
            _logEntries.Add(LogEntry.CreateUsage(command, parameters, "Usage"));
        }

        public ProcessingReport GenerateReport(ProcessingSession session)
        {
            return new ProcessingReport
            {
                Summary = session.GetSummary(),
                DetailedResults = session.Results,
                RelatedLogEntries = _logEntries.ToList()
            };
        }

        public void ExportLogs(string filePath, DateTime? fromDate = null)
        {
            // Test implementation - no actual file export
        }

        public List<LogEntry> GetRecentLogs(int count = 100)
        {
            return _logEntries.Skip(Math.Max(0, _logEntries.Count - count)).ToList();
        }

        public void ClearLogs()
        {
            _logEntries.Clear();
        }

        public void SetVerboseMode(bool enabled)
        {
            _verboseMode = enabled;
        }

        /// <summary>
        /// Test helper method to check if a specific message was logged
        /// </summary>
        public bool HasLoggedMessage(string message, LogLevel? level = null)
        {
            return _logEntries.Any(e => e.Message.Contains(message) && 
                                       (level == null || e.Level == level));
        }

        /// <summary>
        /// Test helper method to get the last logged message
        /// </summary>
        public string GetLastMessage()
        {
            return _logEntries.LastOrDefault()?.Message;
        }

        /// <summary>
        /// Test helper method to get all messages of a specific level
        /// </summary>
        public List<string> GetMessages(LogLevel level)
        {
            return _logEntries.Where(e => e.Level == level).Select(e => e.Message).ToList();
        }
    }
}