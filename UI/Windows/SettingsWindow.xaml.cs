using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using RevitDtools.Core.Models;
using RevitDtools.Core.Services;
using RevitDtools.Utilities;
using WinFormsFolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using WinFormsDialogResult = System.Windows.Forms.DialogResult;

namespace RevitDtools.UI.Windows
{
    /// <summary>
    /// Settings window for RevitDtools configuration
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly SettingsService _settingsService;
        private UserSettings _currentSettings;
        private bool _hasChanges = false;

        // Observable collections for data binding
        private ObservableCollection<LayerMapping> _layerMappings;
        private ObservableCollection<string> _ignoredLayers;
        private ObservableCollection<string> _columnFamilies;
        private ObservableCollection<string> _fileExtensions;
        private ObservableCollection<LayerMappingTemplate> _layerTemplates;

        public SettingsWindow()
        {
            InitializeComponent();
            _settingsService = new SettingsService();
            InitializeWindow();
        }

        /// <summary>
        /// Initialize the window with current settings
        /// </summary>
        private void InitializeWindow()
        {
            try
            {
                _currentSettings = _settingsService.LoadSettings();
                InitializeCollections();
                PopulateControls();
                SetupEventHandlers();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to initialize settings window");
                System.Windows.MessageBox.Show($"Error loading settings: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Initialize observable collections
        /// </summary>
        private void InitializeCollections()
        {
            _layerMappings = new ObservableCollection<LayerMapping>(
                _currentSettings.LayerMapping.DefaultLayerMappings.Select(kvp => 
                    new LayerMapping { Key = kvp.Key, Value = kvp.Value }));

            _ignoredLayers = new ObservableCollection<string>(_currentSettings.LayerMapping.IgnoredLayers);
            _columnFamilies = new ObservableCollection<string>(_currentSettings.ColumnSettings.DefaultColumnFamilies);
            _fileExtensions = new ObservableCollection<string>(_currentSettings.BatchSettings.SupportedFileExtensions);
            _layerTemplates = new ObservableCollection<LayerMappingTemplate>(_settingsService.GetLayerMappingTemplates());
        }

        /// <summary>
        /// Populate controls with current settings
        /// </summary>
        private void PopulateControls()
        {
            // Layer Mapping Tab
            LayerMappingGrid.ItemsSource = _layerMappings;
            AutoSelectAllLayersCheckBox.IsChecked = _currentSettings.LayerMapping.AutoSelectAllLayers;
            PreserveDwgLayerNamesCheckBox.IsChecked = _currentSettings.LayerMapping.PreserveDwgLayerNames;
            DefaultLineStyleComboBox.Text = _currentSettings.LayerMapping.DefaultLineStyle;
            IgnoredLayersListBox.ItemsSource = _ignoredLayers;
            LayerTemplatesListBox.ItemsSource = _layerTemplates;

            // Column Creation Tab
            ColumnFamiliesListBox.ItemsSource = _columnFamilies;
            PreferredColumnFamilyComboBox.ItemsSource = _columnFamilies;
            PreferredColumnFamilyComboBox.Text = _currentSettings.ColumnSettings.PreferredColumnFamily;
            WidthParameterTextBox.Text = _currentSettings.ColumnSettings.DefaultWidthParameter;
            HeightParameterTextBox.Text = _currentSettings.ColumnSettings.DefaultHeightParameter;
            MinimumSizeTextBox.Text = _currentSettings.ColumnSettings.MinimumColumnSize.ToString();
            MaximumSizeTextBox.Text = _currentSettings.ColumnSettings.MaximumColumnSize.ToString();
            AutoCreateFamiliesCheckBox.IsChecked = _currentSettings.ColumnSettings.AutoCreateFamilies;

            // Batch Processing Tab
            MaxConcurrentFilesTextBox.Text = _currentSettings.BatchSettings.MaxConcurrentFiles.ToString();
            IncludeSubfoldersCheckBox.IsChecked = _currentSettings.BatchSettings.IncludeSubfolders;
            ContinueOnErrorCheckBox.IsChecked = _currentSettings.BatchSettings.ContinueOnError;
            GenerateDetailedReportsCheckBox.IsChecked = _currentSettings.BatchSettings.GenerateDetailedReports;
            DefaultOutputFolderTextBox.Text = _currentSettings.BatchSettings.DefaultOutputFolder;
            FileExtensionsListBox.ItemsSource = _fileExtensions;

            // Logging Tab
            MinimumLogLevelComboBox.ItemsSource = Enum.GetValues(typeof(LogLevel));
            MinimumLogLevelComboBox.SelectedItem = _currentSettings.LoggingSettings.MinimumLogLevel;
            EnableFileLoggingCheckBox.IsChecked = _currentSettings.LoggingSettings.EnableFileLogging;
            EnableUsageTrackingCheckBox.IsChecked = _currentSettings.LoggingSettings.EnableUsageTracking;
            VerboseModeCheckBox.IsChecked = _currentSettings.LoggingSettings.VerboseMode;
            MaxLogEntriesTextBox.Text = _currentSettings.LoggingSettings.MaxLogEntries.ToString();
            LogRetentionDaysTextBox.Text = _currentSettings.LoggingSettings.LogRetentionDays.ToString();

            // UI Settings Tab
            RememberWindowPositionsCheckBox.IsChecked = _currentSettings.UISettings.RememberWindowPositions;
            ShowDetailedProgressCheckBox.IsChecked = _currentSettings.UISettings.ShowDetailedProgress;
            ShowTooltipsCheckBox.IsChecked = _currentSettings.UISettings.ShowTooltips;
            ThemeComboBox.ItemsSource = new[] { "Default", "Light", "Dark" };
            ThemeComboBox.Text = _currentSettings.UISettings.Theme;

            // Populate combo boxes with common values
            PopulateLineStyleComboBox();
        }

        /// <summary>
        /// Populate line style combo box with common Revit line styles
        /// </summary>
        private void PopulateLineStyleComboBox()
        {
            var commonLineStyles = new[]
            {
                "Thin Lines", "Medium Lines", "Thick Lines", "Wide Lines",
                "Hidden Lines", "Centerlines", "Dimension Lines", "Text"
            };
            DefaultLineStyleComboBox.ItemsSource = commonLineStyles;
        }

        /// <summary>
        /// Setup event handlers for change tracking
        /// </summary>
        private void SetupEventHandlers()
        {
            // Track changes for all controls
            foreach (var control in GetAllControls(this))
            {
                if (control is System.Windows.Controls.TextBox textBox)
                    textBox.TextChanged += Control_Changed;
                else if (control is System.Windows.Controls.CheckBox checkBox)
                    checkBox.Checked += Control_Changed;
                else if (control is System.Windows.Controls.ComboBox comboBox)
                    comboBox.SelectionChanged += Control_Changed;
            }

            // Collection change handlers
            _layerMappings.CollectionChanged += (s, e) => _hasChanges = true;
            _ignoredLayers.CollectionChanged += (s, e) => _hasChanges = true;
            _columnFamilies.CollectionChanged += (s, e) => _hasChanges = true;
            _fileExtensions.CollectionChanged += (s, e) => _hasChanges = true;
        }

        /// <summary>
        /// Get all controls recursively
        /// </summary>
        private IEnumerable<System.Windows.Controls.Control> GetAllControls(DependencyObject parent)
        {
            var controls = new List<System.Windows.Controls.Control>();
            var childrenCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            
            for (int i = 0; i < childrenCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is System.Windows.Controls.Control control)
                    controls.Add(control);
                controls.AddRange(GetAllControls(child));
            }
            
            return controls;
        }

        /// <summary>
        /// Handle control changes
        /// </summary>
        private void Control_Changed(object sender, EventArgs e)
        {
            _hasChanges = true;
        }

        /// <summary>
        /// Apply current settings
        /// </summary>
        private void ApplySettings()
        {
            try
            {
                // Update settings from controls
                UpdateSettingsFromControls();
                
                // Save settings
                _settingsService.SaveSettings(_currentSettings);
                _hasChanges = false;
                
                Logger.LogInfo("Settings applied successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to apply settings");
                System.Windows.MessageBox.Show($"Error saving settings: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Update settings object from control values
        /// </summary>
        private void UpdateSettingsFromControls()
        {
            // Layer Mapping Settings
            _currentSettings.LayerMapping.DefaultLayerMappings = _layerMappings.ToDictionary(lm => lm.Key, lm => lm.Value);
            _currentSettings.LayerMapping.AutoSelectAllLayers = AutoSelectAllLayersCheckBox.IsChecked ?? false;
            _currentSettings.LayerMapping.PreserveDwgLayerNames = PreserveDwgLayerNamesCheckBox.IsChecked ?? true;
            _currentSettings.LayerMapping.DefaultLineStyle = DefaultLineStyleComboBox.Text;
            _currentSettings.LayerMapping.IgnoredLayers = _ignoredLayers.ToList();

            // Column Creation Settings
            _currentSettings.ColumnSettings.DefaultColumnFamilies = _columnFamilies.ToList();
            _currentSettings.ColumnSettings.PreferredColumnFamily = PreferredColumnFamilyComboBox.Text;
            _currentSettings.ColumnSettings.DefaultWidthParameter = WidthParameterTextBox.Text;
            _currentSettings.ColumnSettings.DefaultHeightParameter = HeightParameterTextBox.Text;
            _currentSettings.ColumnSettings.AutoCreateFamilies = AutoCreateFamiliesCheckBox.IsChecked ?? true;
            
            if (double.TryParse(MinimumSizeTextBox.Text, out double minSize))
                _currentSettings.ColumnSettings.MinimumColumnSize = minSize;
            if (double.TryParse(MaximumSizeTextBox.Text, out double maxSize))
                _currentSettings.ColumnSettings.MaximumColumnSize = maxSize;

            // Batch Processing Settings
            if (int.TryParse(MaxConcurrentFilesTextBox.Text, out int maxFiles))
                _currentSettings.BatchSettings.MaxConcurrentFiles = maxFiles;
            _currentSettings.BatchSettings.IncludeSubfolders = IncludeSubfoldersCheckBox.IsChecked ?? true;
            _currentSettings.BatchSettings.ContinueOnError = ContinueOnErrorCheckBox.IsChecked ?? true;
            _currentSettings.BatchSettings.GenerateDetailedReports = GenerateDetailedReportsCheckBox.IsChecked ?? true;
            _currentSettings.BatchSettings.DefaultOutputFolder = DefaultOutputFolderTextBox.Text;
            _currentSettings.BatchSettings.SupportedFileExtensions = _fileExtensions.ToList();

            // Logging Settings
            if (MinimumLogLevelComboBox.SelectedItem is LogLevel logLevel)
                _currentSettings.LoggingSettings.MinimumLogLevel = logLevel;
            _currentSettings.LoggingSettings.EnableFileLogging = EnableFileLoggingCheckBox.IsChecked ?? true;
            _currentSettings.LoggingSettings.EnableUsageTracking = EnableUsageTrackingCheckBox.IsChecked ?? true;
            _currentSettings.LoggingSettings.VerboseMode = VerboseModeCheckBox.IsChecked ?? false;
            
            if (int.TryParse(MaxLogEntriesTextBox.Text, out int maxEntries))
                _currentSettings.LoggingSettings.MaxLogEntries = maxEntries;
            if (int.TryParse(LogRetentionDaysTextBox.Text, out int retentionDays))
                _currentSettings.LoggingSettings.LogRetentionDays = retentionDays;

            // UI Settings
            _currentSettings.UISettings.RememberWindowPositions = RememberWindowPositionsCheckBox.IsChecked ?? true;
            _currentSettings.UISettings.ShowDetailedProgress = ShowDetailedProgressCheckBox.IsChecked ?? true;
            _currentSettings.UISettings.ShowTooltips = ShowTooltipsCheckBox.IsChecked ?? true;
            _currentSettings.UISettings.Theme = ThemeComboBox.Text;
        }

        #region Event Handlers

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            ApplySettings();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (_hasChanges)
            {
                var result = System.Windows.MessageBox.Show("You have unsaved changes. Are you sure you want to cancel?", 
                    "Unsaved Changes", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
                if (result == System.Windows.MessageBoxResult.No)
                    return;
            }
            
            DialogResult = false;
            Close();
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            ApplySettings();
        }

        private void AddLayerMapping_Click(object sender, RoutedEventArgs e)
        {
            _layerMappings.Add(new LayerMapping { Key = "New Layer", Value = "Thin Lines" });
        }

        private void RemoveLayerMapping_Click(object sender, RoutedEventArgs e)
        {
            if (LayerMappingGrid.SelectedItem is LayerMapping selected)
            {
                _layerMappings.Remove(selected);
            }
        }

        private void AddIgnoredLayer_Click(object sender, RoutedEventArgs e)
        {
            var layerName = NewIgnoredLayerTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(layerName) && !_ignoredLayers.Contains(layerName))
            {
                _ignoredLayers.Add(layerName);
                NewIgnoredLayerTextBox.Clear();
            }
        }

        private void RemoveIgnoredLayer_Click(object sender, RoutedEventArgs e)
        {
            if (IgnoredLayersListBox.SelectedItem is string selected)
            {
                _ignoredLayers.Remove(selected);
            }
        }

        private void SaveTemplate_Click(object sender, RoutedEventArgs e)
        {
            var templateName = NewTemplateNameTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(templateName))
            {
                System.Windows.MessageBox.Show("Please enter a template name.", "Template Name Required", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                var template = new LayerMappingTemplate
                {
                    Name = templateName,
                    Description = $"Template created on {DateTime.Now:yyyy-MM-dd HH:mm}",
                    LayerMappings = _layerMappings.ToDictionary(lm => lm.Key, lm => lm.Value)
                };

                _settingsService.SaveLayerMappingTemplate(template);
                _layerTemplates.Add(template);
                NewTemplateNameTextBox.Clear();
                
                System.Windows.MessageBox.Show($"Template '{templateName}' saved successfully.", "Template Saved", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Failed to save template '{templateName}'");
                System.Windows.MessageBox.Show($"Error saving template: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void LoadTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (LayerTemplatesListBox.SelectedItem is LayerMappingTemplate template)
            {
                _layerMappings.Clear();
                foreach (var mapping in template.LayerMappings)
                {
                    _layerMappings.Add(new LayerMapping { Key = mapping.Key, Value = mapping.Value });
                }
                _hasChanges = true;
            }
        }

        private void DeleteTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (LayerTemplatesListBox.SelectedItem is LayerMappingTemplate template)
            {
                var result = System.Windows.MessageBox.Show($"Are you sure you want to delete template '{template.Name}'?", 
                    "Delete Template", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _settingsService.DeleteLayerMappingTemplate(template.Name);
                        _layerTemplates.Remove(template);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, $"Failed to delete template '{template.Name}'");
                        System.Windows.MessageBox.Show($"Error deleting template: {ex.Message}", "Error", 
                            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
            }
        }

        private void AddColumnFamily_Click(object sender, RoutedEventArgs e)
        {
            var familyName = NewColumnFamilyTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(familyName) && !_columnFamilies.Contains(familyName))
            {
                _columnFamilies.Add(familyName);
                NewColumnFamilyTextBox.Clear();
            }
        }

        private void RemoveColumnFamily_Click(object sender, RoutedEventArgs e)
        {
            if (ColumnFamiliesListBox.SelectedItem is string selected)
            {
                _columnFamilies.Remove(selected);
            }
        }

        private void BrowseOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WinFormsFolderBrowserDialog();
            if (dialog.ShowDialog() == WinFormsDialogResult.OK)
            {
                DefaultOutputFolderTextBox.Text = dialog.SelectedPath;
            }
        }

        private void AddFileExtension_Click(object sender, RoutedEventArgs e)
        {
            var extension = NewFileExtensionTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(extension))
            {
                if (!extension.StartsWith("."))
                    extension = "." + extension;
                
                if (!_fileExtensions.Contains(extension))
                {
                    _fileExtensions.Add(extension);
                    NewFileExtensionTextBox.Clear();
                }
            }
        }

        private void RemoveFileExtension_Click(object sender, RoutedEventArgs e)
        {
            if (FileExtensionsListBox.SelectedItem is string selected)
            {
                _fileExtensions.Remove(selected);
            }
        }

        private void ViewLogs_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement log viewer window
            System.Windows.MessageBox.Show("Log viewer not yet implemented.", "Feature Not Available", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void ExportLogs_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = "txt",
                FileName = $"RevitDtools_Logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    Logger.ExportLogs(dialog.FileName);
                    System.Windows.MessageBox.Show($"Logs exported to {dialog.FileName}", "Export Complete", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to export logs");
                    System.Windows.MessageBox.Show($"Error exporting logs: {ex.Message}", "Error", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private void ClearLogs_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show("Are you sure you want to clear all logs?", 
                "Clear Logs", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    Logger.ClearLogs();
                    System.Windows.MessageBox.Show("Logs cleared successfully.", "Logs Cleared", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to clear logs");
                    System.Windows.MessageBox.Show($"Error clearing logs: {ex.Message}", "Error", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
            }
        }

        private void ExportSettings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json",
                FileName = $"RevitDtools_Settings_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    UpdateSettingsFromControls();
                    _settingsService.ExportSettings(dialog.FileName);
                    System.Windows.MessageBox.Show($"Settings exported to {dialog.FileName}", "Export Complete", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to export settings");
                    System.Windows.MessageBox.Show($"Error exporting settings: {ex.Message}", "Error", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private void ImportSettings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    if (_settingsService.ImportSettings(dialog.FileName))
                    {
                        _currentSettings = _settingsService.GetCurrentSettings();
                        InitializeCollections();
                        PopulateControls();
                        _hasChanges = false;
                        
                        System.Windows.MessageBox.Show($"Settings imported from {dialog.FileName}", "Import Complete", 
                            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Failed to import settings. Please check the file format.", "Import Failed", 
                            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to import settings");
                    System.Windows.MessageBox.Show($"Error importing settings: {ex.Message}", "Error", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private void ResetToDefaults_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show("Are you sure you want to reset all settings to defaults? This cannot be undone.", 
                "Reset to Defaults", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _settingsService.ResetToDefaults();
                    _currentSettings = _settingsService.GetCurrentSettings();
                    InitializeCollections();
                    PopulateControls();
                    _hasChanges = false;
                    
                    System.Windows.MessageBox.Show("Settings reset to defaults successfully.", "Reset Complete", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to reset settings to defaults");
                    System.Windows.MessageBox.Show($"Error resetting settings: {ex.Message}", "Error", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        #endregion

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_hasChanges)
            {
                var result = System.Windows.MessageBox.Show("You have unsaved changes. Do you want to save them before closing?", 
                    "Unsaved Changes", System.Windows.MessageBoxButton.YesNoCancel, System.Windows.MessageBoxImage.Question);
                
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        ApplySettings();
                        break;
                    case MessageBoxResult.Cancel:
                        e.Cancel = true;
                        return;
                }
            }
            
            base.OnClosing(e);
        }
    }

    /// <summary>
    /// Helper class for layer mapping data binding
    /// </summary>
    public class LayerMapping
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}