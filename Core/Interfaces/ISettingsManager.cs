using RevitDtools.Core.Models;
using System.Collections.Generic;

namespace RevitDtools.Core.Interfaces
{
    /// <summary>
    /// Interface for managing user settings and preferences
    /// </summary>
    public interface ISettingsManager
    {
        /// <summary>
        /// Load user settings from persistent storage
        /// </summary>
        UserSettings LoadSettings();

        /// <summary>
        /// Save user settings to persistent storage
        /// </summary>
        void SaveSettings(UserSettings settings);

        /// <summary>
        /// Save a layer mapping template for reuse
        /// </summary>
        void SaveLayerMappingTemplate(LayerMappingTemplate template);

        /// <summary>
        /// Get all saved layer mapping templates
        /// </summary>
        List<LayerMappingTemplate> GetLayerMappingTemplates();

        /// <summary>
        /// Save default column families preferences
        /// </summary>
        void SaveDefaultColumnFamilies(List<string> familyNames);

        /// <summary>
        /// Get default column families preferences
        /// </summary>
        List<string> GetDefaultColumnFamilies();

        /// <summary>
        /// Export settings to a portable configuration file
        /// </summary>
        void ExportSettings(string filePath);

        /// <summary>
        /// Import settings from a configuration file
        /// </summary>
        UserSettings ImportSettings(string filePath);

        /// <summary>
        /// Reset settings to default values
        /// </summary>
        void ResetToDefaults();
    }
}