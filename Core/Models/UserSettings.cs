using System;
using System.Collections.Generic;

namespace RevitDtools.Core.Models
{
    /// <summary>
    /// User settings and preferences for RevitDtools
    /// </summary>
    public class UserSettings
    {
        public LayerMappingSettings LayerMapping { get; set; } = new LayerMappingSettings();
        public ColumnCreationSettings ColumnSettings { get; set; } = new ColumnCreationSettings();
        public BatchProcessingSettings BatchSettings { get; set; } = new BatchProcessingSettings();
        public LoggingSettings LoggingSettings { get; set; } = new LoggingSettings();
        public UISettings UISettings { get; set; } = new UISettings();
        public UpdateSettings UpdateSettings { get; set; } = new UpdateSettings();
        public DateTime LastModified { get; set; } = DateTime.Now;
        public string Version { get; set; } = "1.0";
    }

    /// <summary>
    /// Settings for layer mapping and DWG processing
    /// </summary>
    public class LayerMappingSettings
    {
        public Dictionary<string, string> DefaultLayerMappings { get; set; } = new Dictionary<string, string>();
        public List<string> IgnoredLayers { get; set; } = new List<string>();
        public bool AutoSelectAllLayers { get; set; } = false;
        public bool PreserveDwgLayerNames { get; set; } = true;
        public string DefaultLineStyle { get; set; } = "Thin Lines";
    }

    /// <summary>
    /// Settings for column creation
    /// </summary>
    public class ColumnCreationSettings
    {
        public List<string> DefaultColumnFamilies { get; set; } = new List<string>();
        public string PreferredColumnFamily { get; set; }
        public string DefaultWidthParameter { get; set; } = "b";
        public string DefaultHeightParameter { get; set; } = "h";
        public bool AutoCreateFamilies { get; set; } = true;
        public double MinimumColumnSize { get; set; } = 0.01; // 0.01 feet
        public double MaximumColumnSize { get; set; } = 10.0; // 10 feet
    }

    /// <summary>
    /// Settings for batch processing
    /// </summary>
    public class BatchProcessingSettings
    {
        public int MaxConcurrentFiles { get; set; } = 1;
        public bool IncludeSubfolders { get; set; } = true;
        public bool ContinueOnError { get; set; } = true;
        public bool GenerateDetailedReports { get; set; } = true;
        public string DefaultOutputFolder { get; set; }
        public List<string> SupportedFileExtensions { get; set; } = new List<string> { ".dwg" };
    }

    /// <summary>
    /// Settings for logging and diagnostics
    /// </summary>
    public class LoggingSettings
    {
        public LogLevel MinimumLogLevel { get; set; } = LogLevel.Info;
        public bool EnableFileLogging { get; set; } = true;
        public bool EnableUsageTracking { get; set; } = true;
        public int MaxLogEntries { get; set; } = 10000;
        public int LogRetentionDays { get; set; } = 30;
        public bool VerboseMode { get; set; } = false;
    }

    /// <summary>
    /// Settings for user interface
    /// </summary>
    public class UISettings
    {
        public bool RememberWindowPositions { get; set; } = true;
        public bool ShowDetailedProgress { get; set; } = true;
        public bool ShowTooltips { get; set; } = true;
        public string Theme { get; set; } = "Default";
        public Dictionary<string, object> WindowPositions { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Settings for automatic updates
    /// </summary>
    public class UpdateSettings
    {
        public bool AutoUpdateEnabled { get; set; } = true;
        public DateTime LastUpdateCheck { get; set; } = DateTime.MinValue;
        public int CheckIntervalDays { get; set; } = 7;
        public bool NotifyOnUpdateAvailable { get; set; } = true;
        public bool AutoDownloadUpdates { get; set; } = false;
    }

    /// <summary>
    /// Layer mapping template for reuse
    /// </summary>
    public class LayerMappingTemplate
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<string, string> LayerMappings { get; set; } = new Dictionary<string, string>();
        public DateTime Created { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = Environment.UserName;
    }
}