using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using RevitDtools.Core.Interfaces;
using RevitDtools.Core.Models;
using RevitDtools.Utilities;

namespace RevitDtools.Core.Services
{
    /// <summary>
    /// Service for managing user settings and persistence
    /// </summary>
    public class SettingsService : ISettingsService
    {
        protected readonly string _settingsPath;
        protected readonly string _templatesPath;
        private UserSettings _currentSettings;
        private List<LayerMappingTemplate> _layerMappingTemplates;

        public SettingsService() : this(null)
        {
        }

        protected SettingsService(string customDirectory)
        {
            var appDataPath = customDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RevitDtools");

            // Ensure directory exists
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            _settingsPath = Path.Combine(appDataPath, "settings.json");
            _templatesPath = Path.Combine(appDataPath, "layer_templates.json");
            
            _layerMappingTemplates = new List<LayerMappingTemplate>();
        }

        /// <summary>
        /// Load user settings from persistent storage
        /// </summary>
        /// <returns>User settings object</returns>
        public UserSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    _currentSettings = JsonConvert.DeserializeObject<UserSettings>(json);
                    
                    if (_currentSettings != null)
                    {
                        Logger.LogInfo($"Settings loaded successfully from {_settingsPath}");
                        return _currentSettings;
                    }
                }
                
                Logger.LogInfo("No existing settings found, creating default settings");
                _currentSettings = CreateDefaultSettings();
                SaveSettings(_currentSettings);
                return _currentSettings;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load settings");
                _currentSettings = CreateDefaultSettings();
                return _currentSettings;
            }
        }

        /// <summary>
        /// Save user settings to persistent storage
        /// </summary>
        /// <param name="settings">Settings to save</param>
        public void SaveSettings(UserSettings settings)
        {
            try
            {
                if (settings == null)
                {
                    Logger.LogWarning("Attempted to save null settings");
                    return;
                }

                settings.LastModified = DateTime.Now;
                
                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(_settingsPath, json);
                
                _currentSettings = settings;
                Logger.LogInfo($"Settings saved successfully to {_settingsPath}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to save settings");
                throw;
            }
        }

        /// <summary>
        /// Get current settings (loads if not already loaded)
        /// </summary>
        /// <returns>Current user settings</returns>
        public UserSettings GetCurrentSettings()
        {
            if (_currentSettings == null)
            {
                return LoadSettings();
            }
            return _currentSettings;
        }

        /// <summary>
        /// Save a layer mapping template for reuse
        /// </summary>
        /// <param name="template">Template to save</param>
        public void SaveLayerMappingTemplate(LayerMappingTemplate template)
        {
            try
            {
                if (template == null)
                {
                    Logger.LogWarning("Attempted to save null layer mapping template");
                    return;
                }

                LoadLayerMappingTemplates();
                
                // Remove existing template with same name
                _layerMappingTemplates.RemoveAll(t => t.Name.Equals(template.Name, StringComparison.OrdinalIgnoreCase));
                
                // Add new template
                template.Created = DateTime.Now;
                template.CreatedBy = Environment.UserName;
                _layerMappingTemplates.Add(template);
                
                SaveLayerMappingTemplates();
                Logger.LogInfo($"Layer mapping template '{template.Name}' saved successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Failed to save layer mapping template '{template?.Name}'");
                throw;
            }
        }

        /// <summary>
        /// Get all available layer mapping templates
        /// </summary>
        /// <returns>List of layer mapping templates</returns>
        public List<LayerMappingTemplate> GetLayerMappingTemplates()
        {
            try
            {
                LoadLayerMappingTemplates();
                return _layerMappingTemplates.ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load layer mapping templates");
                return new List<LayerMappingTemplate>();
            }
        }

        /// <summary>
        /// Delete a layer mapping template
        /// </summary>
        /// <param name="templateName">Name of template to delete</param>
        /// <returns>True if deleted successfully</returns>
        public bool DeleteLayerMappingTemplate(string templateName)
        {
            try
            {
                LoadLayerMappingTemplates();
                
                var removed = _layerMappingTemplates.RemoveAll(t => 
                    t.Name.Equals(templateName, StringComparison.OrdinalIgnoreCase));
                
                if (removed > 0)
                {
                    SaveLayerMappingTemplates();
                    Logger.LogInfo($"Layer mapping template '{templateName}' deleted successfully");
                    return true;
                }
                
                Logger.LogWarning($"Layer mapping template '{templateName}' not found");
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Failed to delete layer mapping template '{templateName}'");
                return false;
            }
        }

        /// <summary>
        /// Save default column families to settings
        /// </summary>
        /// <param name="familyNames">List of family names</param>
        public void SaveDefaultColumnFamilies(List<string> familyNames)
        {
            try
            {
                var settings = GetCurrentSettings();
                settings.ColumnSettings.DefaultColumnFamilies = familyNames ?? new List<string>();
                SaveSettings(settings);
                Logger.LogInfo($"Default column families updated: {string.Join(", ", familyNames ?? new List<string>())}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to save default column families");
                throw;
            }
        }

        /// <summary>
        /// Export settings to a file
        /// </summary>
        /// <param name="filePath">Path to export file</param>
        public void ExportSettings(string filePath)
        {
            try
            {
                var settings = GetCurrentSettings();
                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(filePath, json);
                Logger.LogInfo($"Settings exported to {filePath}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Failed to export settings to {filePath}");
                throw;
            }
        }

        /// <summary>
        /// Import settings from a file
        /// </summary>
        /// <param name="filePath">Path to import file</param>
        /// <returns>True if imported successfully</returns>
        public bool ImportSettings(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Logger.LogWarning($"Settings file not found: {filePath}");
                    return false;
                }

                var json = File.ReadAllText(filePath);
                var importedSettings = JsonConvert.DeserializeObject<UserSettings>(json);
                
                if (importedSettings != null)
                {
                    SaveSettings(importedSettings);
                    Logger.LogInfo($"Settings imported from {filePath}");
                    return true;
                }
                
                Logger.LogWarning($"Invalid settings file format: {filePath}");
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Failed to import settings from {filePath}");
                return false;
            }
        }

        /// <summary>
        /// Reset settings to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            try
            {
                _currentSettings = CreateDefaultSettings();
                SaveSettings(_currentSettings);
                Logger.LogInfo("Settings reset to defaults");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to reset settings to defaults");
                throw;
            }
        }

        /// <summary>
        /// Create default settings
        /// </summary>
        /// <returns>Default user settings</returns>
        private UserSettings CreateDefaultSettings()
        {
            return new UserSettings
            {
                LayerMapping = new LayerMappingSettings
                {
                    DefaultLayerMappings = new Dictionary<string, string>
                    {
                        { "0", "Thin Lines" },
                        { "DEFPOINTS", "Hidden Lines" },
                        { "TEXT", "Text" },
                        { "DIMENSIONS", "Dimension Lines" }
                    },
                    IgnoredLayers = new List<string> { "XREF", "VIEWPORT" },
                    AutoSelectAllLayers = false,
                    PreserveDwgLayerNames = true,
                    DefaultLineStyle = "Thin Lines"
                },
                ColumnSettings = new ColumnCreationSettings
                {
                    DefaultColumnFamilies = new List<string> 
                    { 
                        "Structural Column", 
                        "M_Concrete-Rectangular-Column",
                        "M_Steel-Column-W Shape"
                    },
                    PreferredColumnFamily = "Structural Column",
                    DefaultWidthParameter = "b",
                    DefaultHeightParameter = "h",
                    AutoCreateFamilies = true,
                    MinimumColumnSize = 0.01,
                    MaximumColumnSize = 10.0
                },
                BatchSettings = new BatchProcessingSettings
                {
                    MaxConcurrentFiles = 1,
                    IncludeSubfolders = true,
                    ContinueOnError = true,
                    GenerateDetailedReports = true,
                    SupportedFileExtensions = new List<string> { ".dwg" }
                },
                LoggingSettings = new LoggingSettings
                {
                    MinimumLogLevel = LogLevel.Info,
                    EnableFileLogging = true,
                    EnableUsageTracking = true,
                    MaxLogEntries = 10000,
                    LogRetentionDays = 30,
                    VerboseMode = false
                },
                UISettings = new UISettings
                {
                    RememberWindowPositions = true,
                    ShowDetailedProgress = true,
                    ShowTooltips = true,
                    Theme = "Default",
                    WindowPositions = new Dictionary<string, object>()
                }
            };
        }

        /// <summary>
        /// Load layer mapping templates from file
        /// </summary>
        private void LoadLayerMappingTemplates()
        {
            try
            {
                if (File.Exists(_templatesPath))
                {
                    var json = File.ReadAllText(_templatesPath);
                    var templates = JsonConvert.DeserializeObject<List<LayerMappingTemplate>>(json);
                    _layerMappingTemplates = templates ?? new List<LayerMappingTemplate>();
                }
                else
                {
                    _layerMappingTemplates = new List<LayerMappingTemplate>();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load layer mapping templates");
                _layerMappingTemplates = new List<LayerMappingTemplate>();
            }
        }

        /// <summary>
        /// Save layer mapping templates to file
        /// </summary>
        private void SaveLayerMappingTemplates()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_layerMappingTemplates, Formatting.Indented);
                File.WriteAllText(_templatesPath, json);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to save layer mapping templates");
                throw;
            }
        }
    }
}