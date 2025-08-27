using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RevitDtools.Core.Models;

namespace RevitDtools.Utilities
{
    /// <summary>
    /// Performance monitoring and optimization utilities for RevitDtools
    /// </summary>
    public class PerformanceMonitor
    {
        private readonly Dictionary<string, PerformanceMetric> _metrics;
        private readonly Dictionary<string, Stopwatch> _activeTimers;

        public PerformanceMonitor()
        {
            _metrics = new Dictionary<string, PerformanceMetric>();
            _activeTimers = new Dictionary<string, Stopwatch>();
        }

        /// <summary>
        /// Start timing a specific operation
        /// </summary>
        public void StartTimer(string operationName)
        {
            if (_activeTimers.ContainsKey(operationName))
            {
                _activeTimers[operationName].Restart();
            }
            else
            {
                _activeTimers[operationName] = Stopwatch.StartNew();
            }
        }

        /// <summary>
        /// Stop timing an operation and record the metric
        /// </summary>
        public TimeSpan StopTimer(string operationName)
        {
            if (!_activeTimers.ContainsKey(operationName))
            {
                throw new InvalidOperationException($"Timer for operation '{operationName}' was not started");
            }

            var timer = _activeTimers[operationName];
            timer.Stop();
            var elapsed = timer.Elapsed;

            RecordMetric(operationName, elapsed);
            _activeTimers.Remove(operationName);

            return elapsed;
        }

        /// <summary>
        /// Record a performance metric
        /// </summary>
        public void RecordMetric(string operationName, TimeSpan duration, int itemsProcessed = 1)
        {
            if (!_metrics.ContainsKey(operationName))
            {
                _metrics[operationName] = new PerformanceMetric(operationName);
            }

            _metrics[operationName].AddMeasurement(duration, itemsProcessed);
        }

        /// <summary>
        /// Get performance statistics for an operation
        /// </summary>
        public PerformanceMetric GetMetric(string operationName)
        {
            return _metrics.ContainsKey(operationName) ? _metrics[operationName] : null;
        }

        /// <summary>
        /// Get all recorded metrics
        /// </summary>
        public Dictionary<string, PerformanceMetric> GetAllMetrics()
        {
            return new Dictionary<string, PerformanceMetric>(_metrics);
        }

        /// <summary>
        /// Generate a performance report
        /// </summary>
        public PerformanceReport GenerateReport()
        {
            var report = new PerformanceReport
            {
                GeneratedAt = DateTime.Now,
                Metrics = _metrics.Values.ToList()
            };

            // Calculate overall statistics
            report.TotalOperations = _metrics.Values.Sum(m => m.ExecutionCount);
            report.TotalProcessingTime = TimeSpan.FromMilliseconds(_metrics.Values.Sum(m => m.TotalTime.TotalMilliseconds));
            report.AverageOperationTime = report.TotalOperations > 0 
                ? TimeSpan.FromMilliseconds(report.TotalProcessingTime.TotalMilliseconds / report.TotalOperations)
                : TimeSpan.Zero;

            // Identify performance bottlenecks
            report.Bottlenecks = IdentifyBottlenecks();

            return report;
        }

        /// <summary>
        /// Clear all recorded metrics
        /// </summary>
        public void ClearMetrics()
        {
            _metrics.Clear();
            _activeTimers.Clear();
        }

        /// <summary>
        /// Monitor memory usage during an operation
        /// </summary>
        public MemoryUsageResult MonitorMemoryUsage(Action operation, string operationName = null)
        {
            var initialMemory = GC.GetTotalMemory(true);
            var stopwatch = Stopwatch.StartNew();
            MemoryUsageResult result = null;

            try
            {
                operation();
            }
            finally
            {
                stopwatch.Stop();
                var finalMemory = GC.GetTotalMemory(true);

                result = new MemoryUsageResult
                {
                    OperationName = operationName ?? "Unknown Operation",
                    InitialMemory = initialMemory,
                    FinalMemory = finalMemory,
                    MemoryDelta = finalMemory - initialMemory,
                    ExecutionTime = stopwatch.Elapsed
                };
            }

            return result;
        }

        /// <summary>
        /// Optimize memory usage by forcing garbage collection
        /// </summary>
        public void OptimizeMemory()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        /// <summary>
        /// Check if performance meets specified criteria
        /// </summary>
        public bool MeetsPerformanceCriteria(string operationName, TimeSpan maxDuration, double maxAverageTime = 0)
        {
            if (!_metrics.ContainsKey(operationName))
                return false;

            var metric = _metrics[operationName];
            
            // Check maximum duration
            if (metric.MaxTime > maxDuration)
                return false;

            // Check average time if specified
            if (maxAverageTime > 0 && metric.AverageTime.TotalMilliseconds > maxAverageTime)
                return false;

            return true;
        }

        private List<PerformanceBottleneck> IdentifyBottlenecks()
        {
            var bottlenecks = new List<PerformanceBottleneck>();

            foreach (var metric in _metrics.Values)
            {
                // Identify operations that take longer than 5 seconds on average
                if (metric.AverageTime.TotalSeconds > 5)
                {
                    bottlenecks.Add(new PerformanceBottleneck
                    {
                        OperationName = metric.OperationName,
                        Issue = "High average execution time",
                        AverageTime = metric.AverageTime,
                        Recommendation = "Consider optimizing this operation or breaking it into smaller chunks"
                    });
                }

                // Identify operations with high variance
                if (metric.MaxTime.TotalMilliseconds > metric.AverageTime.TotalMilliseconds * 3)
                {
                    bottlenecks.Add(new PerformanceBottleneck
                    {
                        OperationName = metric.OperationName,
                        Issue = "High execution time variance",
                        AverageTime = metric.AverageTime,
                        MaxTime = metric.MaxTime,
                        Recommendation = "Investigate why some executions take significantly longer"
                    });
                }
            }

            return bottlenecks;
        }
    }

    /// <summary>
    /// Performance metric for a specific operation
    /// </summary>
    public class PerformanceMetric
    {
        public string OperationName { get; }
        public int ExecutionCount { get; private set; }
        public TimeSpan TotalTime { get; private set; }
        public TimeSpan MinTime { get; private set; } = TimeSpan.MaxValue;
        public TimeSpan MaxTime { get; private set; } = TimeSpan.MinValue;
        public TimeSpan AverageTime => ExecutionCount > 0 ? TimeSpan.FromTicks(TotalTime.Ticks / ExecutionCount) : TimeSpan.Zero;
        public int TotalItemsProcessed { get; private set; }
        public double ItemsPerSecond => TotalTime.TotalSeconds > 0 ? TotalItemsProcessed / TotalTime.TotalSeconds : 0;

        public PerformanceMetric(string operationName)
        {
            OperationName = operationName;
        }

        public void AddMeasurement(TimeSpan duration, int itemsProcessed = 1)
        {
            ExecutionCount++;
            TotalTime = TotalTime.Add(duration);
            TotalItemsProcessed += itemsProcessed;

            if (duration < MinTime)
                MinTime = duration;

            if (duration > MaxTime)
                MaxTime = duration;
        }
    }

    /// <summary>
    /// Performance report containing all metrics and analysis
    /// </summary>
    public class PerformanceReport
    {
        public DateTime GeneratedAt { get; set; }
        public List<PerformanceMetric> Metrics { get; set; } = new List<PerformanceMetric>();
        public int TotalOperations { get; set; }
        public TimeSpan TotalProcessingTime { get; set; }
        public TimeSpan AverageOperationTime { get; set; }
        public List<PerformanceBottleneck> Bottlenecks { get; set; } = new List<PerformanceBottleneck>();

        public string GenerateTextReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== RevitDtools Performance Report ===");
            report.AppendLine($"Generated: {GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Total Operations: {TotalOperations}");
            report.AppendLine($"Total Processing Time: {TotalProcessingTime:hh\\:mm\\:ss\\.fff}");
            report.AppendLine($"Average Operation Time: {AverageOperationTime:hh\\:mm\\:ss\\.fff}");
            report.AppendLine();

            report.AppendLine("=== Operation Metrics ===");
            foreach (var metric in Metrics.OrderByDescending(m => m.TotalTime))
            {
                report.AppendLine($"Operation: {metric.OperationName}");
                report.AppendLine($"  Executions: {metric.ExecutionCount}");
                report.AppendLine($"  Total Time: {metric.TotalTime:hh\\:mm\\:ss\\.fff}");
                report.AppendLine($"  Average Time: {metric.AverageTime:hh\\:mm\\:ss\\.fff}");
                report.AppendLine($"  Min Time: {metric.MinTime:hh\\:mm\\:ss\\.fff}");
                report.AppendLine($"  Max Time: {metric.MaxTime:hh\\:mm\\:ss\\.fff}");
                report.AppendLine($"  Items Processed: {metric.TotalItemsProcessed}");
                report.AppendLine($"  Items/Second: {metric.ItemsPerSecond:F2}");
                report.AppendLine();
            }

            if (Bottlenecks.Any())
            {
                report.AppendLine("=== Performance Bottlenecks ===");
                foreach (var bottleneck in Bottlenecks)
                {
                    report.AppendLine($"Operation: {bottleneck.OperationName}");
                    report.AppendLine($"  Issue: {bottleneck.Issue}");
                    report.AppendLine($"  Recommendation: {bottleneck.Recommendation}");
                    report.AppendLine();
                }
            }

            return report.ToString();
        }
    }

    /// <summary>
    /// Performance bottleneck identification
    /// </summary>
    public class PerformanceBottleneck
    {
        public string OperationName { get; set; }
        public string Issue { get; set; }
        public string Recommendation { get; set; }
        public TimeSpan AverageTime { get; set; }
        public TimeSpan MaxTime { get; set; }
    }

    /// <summary>
    /// Memory usage monitoring result
    /// </summary>
    public class MemoryUsageResult
    {
        public string OperationName { get; set; }
        public long InitialMemory { get; set; }
        public long FinalMemory { get; set; }
        public long MemoryDelta { get; set; }
        public TimeSpan ExecutionTime { get; set; }

        public string GetMemoryDeltaString()
        {
            var deltaMB = MemoryDelta / (1024.0 * 1024.0);
            return $"{deltaMB:F2} MB";
        }
    }
}