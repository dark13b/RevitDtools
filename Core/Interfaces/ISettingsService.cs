using System.Collections.Generic;
using RevitDtools.Core.Models;

namespace RevitDtools.Core.Interfaces
{
    /// <summary>
    /// Interface for managing user settings and persistence
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// Load user settings from persistent storage
        /// </summary>
        /// <returns>User settings object</returns>
        UserSettings LoadSettings();

        /// <summary>
        /// Save user settings to persistent storage
        /// </summary>
        /// <param name="settings">Settings to save</param>
        void SaveSettings(UserSettings settings);

        /// <summary>
        /// Get current settings (loads if not already loaded)
        /// </summary>
        /// <returns>Current user settings</returns>
        UserSettings GetCurrentSettings();

        /// <summary>
        /// Save a layer mapping template for reuse
        /// </summary>
        /// <param name="template">Template to save</param>
        void SaveLayerMappingTemplate(LayerMappingTemplate template);

        /// <summary>
        /// Get all available layer mapping templates
        /// </summary>
        /// <returns>List of layer mapping templates</returns>
        List<LayerMappingTemplate> GetLayerMappingTemplates();

        /// <summary>
        /// Delete a layer mapping template
        /// </summary>
        /// <param name="templateName">Name of template to delete</param>
        /// <returns>True if deleted successfully</returns>
        bool DeleteLayerMappingTemplate(string templateName);

        /// <summary>
        /// Save default column families to settings
        /// </summary>
        /// <param name="familyNames">List of family names</param>
        void SaveDefaultColumnFamilies(List<string> familyNames);

        /// <summary>
        /// Export settings to a file
        /// </summary>
        /// <param name="filePath">Path to export file</param>
        void ExportSettings(string filePath);

        /// <summary>
        /// Import settings from a file
        /// </summary>
        /// <param name="filePath">Path to import file</param>
        /// <returns>True if imported successfully</returns>
        bool ImportSettings(string filePath);

        /// <summary>
        /// Reset settings to defaults
        /// </summary>
        void ResetToDefaults();
    }
}