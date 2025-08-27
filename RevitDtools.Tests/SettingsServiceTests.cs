using Microsoft.VisualStudio.TestTools.UnitTesting;
using RevitDtools.Core.Models;
using RevitDtools.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RevitDtools.Tests
{
    [TestClass]
    public class SettingsServiceTests
    {
        private SettingsService _settingsService;
        private string _testDirectory;

        [TestInitialize]
        public void Setup()
        {
            // Create a temporary directory for test settings
            _testDirectory = Path.Combine(Path.GetTempPath(), "RevitDtoolsTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);

            // Override the settings path for testing
            _settingsService = new TestSettingsService(_testDirectory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean up test directory
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [TestMethod]
        public void LoadSettings_WhenNoSettingsExist_ReturnsDefaultSettings()
        {
            // Act
            var settings = _settingsService.LoadSettings();

            // Assert
            Assert.IsNotNull(settings);
            Assert.IsNotNull(settings.LayerMapping);
            Assert.IsNotNull(settings.ColumnSettings);
            Assert.IsNotNull(settings.BatchSettings);
            Assert.IsNotNull(settings.LoggingSettings);
            Assert.IsNotNull(settings.UISettings);
            Assert.AreEqual("1.0", settings.Version);
        }

        [TestMethod]
        public void SaveSettings_ValidSettings_SavesSuccessfully()
        {
            // Arrange
            var settings = new UserSettings
            {
                Version = "1.0",
                LayerMapping = new LayerMappingSettings
                {
                    DefaultLineStyle = "Test Line Style",
                    AutoSelectAllLayers = true
                }
            };

            // Act
            _settingsService.SaveSettings(settings);

            // Assert
            var loadedSettings = _settingsService.LoadSettings();
            Assert.AreEqual("Test Line Style", loadedSettings.LayerMapping.DefaultLineStyle);
            Assert.IsTrue(loadedSettings.LayerMapping.AutoSelectAllLayers);
        }

        [TestMethod]
        public void SaveLayerMappingTemplate_ValidTemplate_SavesSuccessfully()
        {
            // Arrange
            var template = new LayerMappingTemplate
            {
                Name = "Test Template",
                Description = "Test Description",
                LayerMappings = new Dictionary<string, string>
                {
                    { "Layer1", "Style1" },
                    { "Layer2", "Style2" }
                }
            };

            // Act
            _settingsService.SaveLayerMappingTemplate(template);

            // Assert
            var templates = _settingsService.GetLayerMappingTemplates();
            Assert.AreEqual(1, templates.Count);
            Assert.AreEqual("Test Template", templates[0].Name);
            Assert.AreEqual("Test Description", templates[0].Description);
            Assert.AreEqual(2, templates[0].LayerMappings.Count);
        }

        [TestMethod]
        public void GetLayerMappingTemplates_WhenNoTemplatesExist_ReturnsEmptyList()
        {
            // Act
            var templates = _settingsService.GetLayerMappingTemplates();

            // Assert
            Assert.IsNotNull(templates);
            Assert.AreEqual(0, templates.Count);
        }

        [TestMethod]
        public void DeleteLayerMappingTemplate_ExistingTemplate_DeletesSuccessfully()
        {
            // Arrange
            var template = new LayerMappingTemplate
            {
                Name = "Test Template",
                Description = "Test Description"
            };
            _settingsService.SaveLayerMappingTemplate(template);

            // Act
            var result = _settingsService.DeleteLayerMappingTemplate("Test Template");

            // Assert
            Assert.IsTrue(result);
            var templates = _settingsService.GetLayerMappingTemplates();
            Assert.AreEqual(0, templates.Count);
        }

        [TestMethod]
        public void DeleteLayerMappingTemplate_NonExistentTemplate_ReturnsFalse()
        {
            // Act
            var result = _settingsService.DeleteLayerMappingTemplate("Non-existent Template");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SaveDefaultColumnFamilies_ValidFamilies_SavesSuccessfully()
        {
            // Arrange
            var families = new List<string> { "Family1", "Family2", "Family3" };

            // Act
            _settingsService.SaveDefaultColumnFamilies(families);

            // Assert
            var settings = _settingsService.GetCurrentSettings();
            Assert.AreEqual(3, settings.ColumnSettings.DefaultColumnFamilies.Count);
            Assert.IsTrue(settings.ColumnSettings.DefaultColumnFamilies.Contains("Family1"));
            Assert.IsTrue(settings.ColumnSettings.DefaultColumnFamilies.Contains("Family2"));
            Assert.IsTrue(settings.ColumnSettings.DefaultColumnFamilies.Contains("Family3"));
        }

        [TestMethod]
        public void ExportSettings_ValidPath_ExportsSuccessfully()
        {
            // Arrange
            var settings = _settingsService.LoadSettings();
            var exportPath = Path.Combine(_testDirectory, "exported_settings.json");

            // Act
            _settingsService.ExportSettings(exportPath);

            // Assert
            Assert.IsTrue(File.Exists(exportPath));
            var content = File.ReadAllText(exportPath);
            Assert.IsTrue(content.Contains("LayerMapping"));
            Assert.IsTrue(content.Contains("ColumnSettings"));
        }

        [TestMethod]
        public void ImportSettings_ValidFile_ImportsSuccessfully()
        {
            // Arrange
            var originalSettings = _settingsService.LoadSettings();
            originalSettings.LayerMapping.DefaultLineStyle = "Modified Style";
            _settingsService.SaveSettings(originalSettings);

            var exportPath = Path.Combine(_testDirectory, "test_export.json");
            _settingsService.ExportSettings(exportPath);

            // Modify current settings
            var modifiedSettings = _settingsService.GetCurrentSettings();
            modifiedSettings.LayerMapping.DefaultLineStyle = "Different Style";
            _settingsService.SaveSettings(modifiedSettings);

            // Act
            var result = _settingsService.ImportSettings(exportPath);

            // Assert
            Assert.IsTrue(result);
            var importedSettings = _settingsService.GetCurrentSettings();
            Assert.AreEqual("Modified Style", importedSettings.LayerMapping.DefaultLineStyle);
        }

        [TestMethod]
        public void ImportSettings_NonExistentFile_ReturnsFalse()
        {
            // Act
            var result = _settingsService.ImportSettings("non_existent_file.json");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ResetToDefaults_ResetsAllSettings()
        {
            // Arrange
            var settings = _settingsService.LoadSettings();
            settings.LayerMapping.DefaultLineStyle = "Modified Style";
            settings.ColumnSettings.AutoCreateFamilies = false;
            _settingsService.SaveSettings(settings);

            // Act
            _settingsService.ResetToDefaults();

            // Assert
            var resetSettings = _settingsService.GetCurrentSettings();
            Assert.AreEqual("Thin Lines", resetSettings.LayerMapping.DefaultLineStyle);
            Assert.IsTrue(resetSettings.ColumnSettings.AutoCreateFamilies);
        }

        [TestMethod]
        public void GetCurrentSettings_WhenNotLoaded_LoadsAndReturnsSettings()
        {
            // Act
            var settings = _settingsService.GetCurrentSettings();

            // Assert
            Assert.IsNotNull(settings);
            Assert.IsNotNull(settings.LayerMapping);
        }

        [TestMethod]
        public void SaveSettings_NullSettings_DoesNotThrow()
        {
            // Act & Assert - Should not throw exception
            _settingsService.SaveSettings(null);
        }

        [TestMethod]
        public void SaveLayerMappingTemplate_NullTemplate_DoesNotThrow()
        {
            // Act & Assert - Should not throw exception
            _settingsService.SaveLayerMappingTemplate(null);
        }

        [TestMethod]
        public void SaveLayerMappingTemplate_DuplicateName_ReplacesExisting()
        {
            // Arrange
            var template1 = new LayerMappingTemplate
            {
                Name = "Test Template",
                Description = "First Description"
            };
            var template2 = new LayerMappingTemplate
            {
                Name = "Test Template",
                Description = "Second Description"
            };

            // Act
            _settingsService.SaveLayerMappingTemplate(template1);
            _settingsService.SaveLayerMappingTemplate(template2);

            // Assert
            var templates = _settingsService.GetLayerMappingTemplates();
            Assert.AreEqual(1, templates.Count);
            Assert.AreEqual("Second Description", templates[0].Description);
        }
    }

    /// <summary>
    /// Test implementation of SettingsService that uses a custom directory
    /// </summary>
    public class TestSettingsService : SettingsService
    {
        public TestSettingsService(string testDirectory) : base(testDirectory)
        {
        }
    }
}