using System;
using System.Collections.Generic;

namespace RevitDtools.Core.Models
{
    /// <summary>
    /// Log entry for tracking system events and errors
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public LogLevel Level { get; set; }
        public string Message { get; set; }
        public string Context { get; set; }
        public string StackTrace { get; set; }
        public string MachineName { get; set; } = Environment.MachineName;
        public string UserName { get; set; } = Environment.UserName;
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Create an info log entry
        /// </summary>
        public static LogEntry CreateInfo(string message, string context = null)
        {
            return new LogEntry
            {
                Level = LogLevel.Info,
                Message = message,
                Context = context
            };
        }

        /// <summary>
        /// Create a warning log entry
        /// </summary>
        public static LogEntry CreateWarning(string message, string context = null)
        {
            return new LogEntry
            {
                Level = LogLevel.Warning,
                Message = message,
                Context = context
            };
        }

        /// <summary>
        /// Create an error log entry
        /// </summary>
        public static LogEntry CreateError(string message, Exception exception = null, string context = null)
        {
            return new LogEntry
            {
                Level = LogLevel.Error,
                Message = message,
                Context = context,
                StackTrace = exception?.StackTrace
            };
        }

        /// <summary>
        /// Create a usage log entry
        /// </summary>
        public static LogEntry CreateUsage(string command, Dictionary<string, object> parameters = null, string context = null)
        {
            var entry = new LogEntry
            {
                Level = LogLevel.Usage,
                Message = $"Command executed: {command}",
                Context = context
            };

            if (parameters != null)
            {
                entry.AdditionalData = parameters;
            }

            return entry;
        }

        /// <summary>
        /// Format the log entry as a string
        /// </summary>
        public override string ToString()
        {
            var formatted = $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level}] {Message}";
            
            if (!string.IsNullOrEmpty(Context))
            {
                formatted += $" (Context: {Context})";
            }

            return formatted;
        }
    }

    /// <summary>
    /// Log levels for categorizing log entries
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Critical = 4,
        Usage = 5
    }
}