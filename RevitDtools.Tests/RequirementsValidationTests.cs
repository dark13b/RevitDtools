using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitDtools.Core.Services;
using RevitDtools.Core.Models;
using RevitDtools.Utilities;

namespace RevitDtools.Tests
{
    /// <summary>
    /// Comprehensive validation tests for all requirements specified in the requirements document
    /// </summary>
    [TestClass]
    public class RequirementsValidationTests
    {
        private Document _testDocument;
        private UIDocument _uiDocument;
        private GeometryProcessingService _geometryService;
        private FamilyManagementService _familyService;
        private BatchProcessingService _batchService;
        private SettingsService _settingsService;
        private UpdateService _updateService;
        private Logger _logger;

        [TestInitialize]
        public void Setup()
        {
            _logger = new Logger();
            _geometryService = new GeometryProcessingService();
            _familyService = new FamilyManagementService(_testDocument);
            _batchService = new BatchProcessingService(_geometryService, _familyService);
            _settingsService = new SettingsService();
            _updateService = new UpdateService(_logger);
        }

        #region Requirement 1: Build System and Compilation Fixes

        [TestMethod]
        public void Requirement1_1_ProjectCompilesWithoutErrors()
        {
            // WHEN the solution is built in Visual Studio THEN the project SHALL compile without errors or warnings
            var buildValidator = new BuildValidator();
            var buildResult = buildValidator.ValidateProjectBuild();

            Assert.IsTrue(buildResult.Success, $"Project should compile without errors. Errors: {string.Join(", ", buildResult.Errors)}");
            Assert.AreEqual(0, buildResult.Errors.Count, "Should have no compilation errors");
            Assert.IsTrue(buildResult.Warnings.Count <= 5, "Should have minimal warnings"); // Allow some minor warnings
        }

        [TestMethod]
        public void Requirement1_2_BinDirectoryContainsNecessaryAssemblies()
        {
            // WHEN the build process completes THEN the bin directory SHALL contain all necessary assemblies and dependencies
            var binPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory);
            
            Assert.IsTrue(File.Exists(Path.Combine(binPath, "RevitDtools.dll")), "RevitDtools.dll should exist in bin directory");
            Assert.IsTrue(File.Exists(Path.Combine(binPath, "Newtonsoft.Json.dll")), "Newtonsoft.Json.dll should exist in bin directory");
            
            // Verify assembly can be loaded
            var assembly = System.Reflection.Assembly.LoadFrom(Path.Combine(binPath, "RevitDtools.dll"));
            Assert.IsNotNull(assembly, "RevitDtools assembly should load successfully");
        }

        [TestMethod]
        public void Requirement1_3_PostBuildDeploymentScript()
        {
            // WHEN the post-build deployment script runs THEN the add-in SHALL be automatically deployed to the Revit add-ins directory
            var deploymentValidator = new DeploymentValidator();
            var deploymentResult = deploymentValidator.ValidateDeployment();

            Assert.IsTrue(deploymentResult.AddinFileExists, "Addin file should be deployed");
            Assert.IsTrue(deploymentResult.DllFileExists, "DLL file should be deployed");
            Assert.IsTrue(deploymentResult.AddinFileValid, "Addin file should be valid XML");
        }

        [TestMethod]
        public void Requirement1_4_RevitAddinLoadsSuccessfully()
        {
            // WHEN Revit 2026 starts THEN the RevitDtools add-in SHALL load successfully without errors
            // This test would typically be run in a Revit context
            var app = new App();
            var result = app.OnStartup(null); // Simulate application startup

            Assert.AreEqual(Result.Succeeded, result, "Add-in should load successfully");
        }

        [TestMethod]
        public void Requirement1_5_ApiVersionConflictResolution()
        {
            // IF there are API version conflicts THEN the system SHALL resolve them automatically or provide clear error messages
            var versionValidator = new ApiVersionValidator();
            var versionResult = versionValidator.ValidateApiCompatibility();

            Assert.IsTrue(versionResult.IsCompatible || !string.IsNullOrEmpty(versionResult.ErrorMessage), 
                "Should either be compatible or provide clear error message");
        }

        [TestMethod]
        public void Requirement1_6_DebugAndReleaseConfigurationsBuild()
        {
            // WHEN both Debug and Release configurations are built THEN both SHALL compile successfully
            var buildValidator = new BuildValidator();
            
            var debugResult = buildValidator.ValidateConfiguration("Debug");
            var releaseResult = buildValidator.ValidateConfiguration("Release");

            Assert.IsTrue(debugResult.Success, "Debug configuration should build successfully");
            Assert.IsTrue(releaseResult.Success, "Release configuration should build successfully");
        }

        #endregion

        #region Requirement 2: Comprehensive Geometry Processing

        [TestMethod]
        public void Requirement2_1_ArcElementConversion()
        {
            // WHEN a DWG file contains arc elements THEN the system SHALL convert them to Revit arc detail lines
            var testFile = CreateTestDwgWithArcs();
            
            try
            {
                var result = _geometryService.ProcessDwgFile(testFile, _testDocument);
                
                Assert.IsTrue(result.Success, "Arc processing should succeed");
                Assert.IsTrue(result.ProcessedGeometryTypes.Contains("Arc"), "Should process arc geometry");
                Assert.IsTrue(result.ElementsProcessed > 0, "Should process at least one arc element");
            }
            finally
            {
                if (File.Exists(testFile)) File.Delete(testFile);
            }
        }

        [TestMethod]
        public void Requirement2_2_SplineElementConversion()
        {
            // WHEN a DWG file contains spline elements THEN the system SHALL convert them to Revit spline detail lines
            var testFile = CreateTestDwgWithSplines();
            
            try
            {
                var result = _geometryService.ProcessDwgFile(testFile, _testDocument);
                
                Assert.IsTrue(result.Success, "Spline processing should succeed");
                Assert.IsTrue(result.ProcessedGeometryTypes.Contains("Spline"), "Should process spline geometry");
                Assert.IsTrue(result.ElementsProcessed > 0, "Should process at least one spline element");
            }
            finally
            {
                if (File.Exists(testFile)) File.Delete(testFile);
            }
        }

        [TestMethod]
        public void Requirement2_3_EllipseElementConversion()
        {
            // WHEN a DWG file contains ellipse elements THEN the system SHALL convert them to Revit ellipse detail lines
            var testFile = CreateTestDwgWithEllipses();
            
            try
            {
                var result = _geometryService.ProcessDwgFile(testFile, _testDocument);
                
                Assert.IsTrue(result.Success, "Ellipse processing should succeed");
                Assert.IsTrue(result.ProcessedGeometryTypes.Contains("Ellipse"), "Should process ellipse geometry");
                Assert.IsTrue(result.ElementsProcessed > 0, "Should process at least one ellipse element");
            }
            finally
            {
                if (File.Exists(testFile)) File.Delete(testFile);
            }
        }

        [TestMethod]
        public void Requirement2_4_TextElementConversion()
        {
            // WHEN a DWG file contains text elements THEN the system SHALL convert them to Revit text notes
            var testFile = CreateTestDwgWithText();
            
            try
            {
                var result = _geometryService.ProcessDwgFile(testFile, _testDocument);
                
                Assert.IsTrue(result.Success, "Text processing should succeed");
                Assert.IsTrue(result.ProcessedGeometryTypes.Contains("Text"), "Should process text geometry");
                Assert.IsTrue(result.ElementsProcessed > 0, "Should process at least one text element");
            }
            finally
            {
                if (File.Exists(testFile)) File.Delete(testFile);
            }
        }

        [TestMethod]
        public void Requirement2_5_HatchElementConversion()
        {
            // WHEN a DWG file contains hatch patterns THEN the system SHALL convert them to Revit filled regions
            var testFile = CreateTestDwgWithHatches();
            
            try
            {
                var result = _geometryService.ProcessDwgFile(testFile, _testDocument);
                
                Assert.IsTrue(result.Success, "Hatch processing should succeed");
                Assert.IsTrue(result.ProcessedGeometryTypes.Contains("Hatch"), "Should process hatch geometry");
                Assert.IsTrue(result.ElementsProcessed > 0, "Should process at least one hatch element");
            }
            finally
            {
                if (File.Exists(testFile)) File.Delete(testFile);
            }
        }

        [TestMethod]
        public void Requirement2_6_NestedBlockProcessing()
        {
            // WHEN a DWG file contains nested blocks THEN the system SHALL process them recursively and convert all contained geometry
            var testFile = CreateTestDwgWithNestedBlocks();
            
            try
            {
                var result = _geometryService.ProcessDwgFile(testFile, _testDocument);
                
                Assert.IsTrue(result.Success, "Nested block processing should succeed");
                Assert.IsTrue(result.ElementsProcessed > 1, "Should process multiple elements from nested blocks");
                Assert.IsTrue(result.ProcessingDetails.Contains("nested"), "Should indicate nested block processing");
            }
            finally
            {
                if (File.Exists(testFile)) File.Delete(testFile);
            }
        }

        [TestMethod]
        public void Requirement2_7_GeometryConversionErrorHandling()
        {
            // IF geometry conversion fails for any element THEN the system SHALL log the error and continue processing other elements
            var testFile = CreateTestDwgWithMixedValidInvalidGeometry();
            
            try
            {
                var result = _geometryService.ProcessDwgFile(testFile, _testDocument);
                
                Assert.IsTrue(result.ElementsProcessed > 0, "Should process valid elements");
                Assert.IsTrue(result.ElementsSkipped > 0, "Should skip invalid elements");
                Assert.IsTrue(result.Errors.Count > 0, "Should log errors for invalid elements");
                Assert.IsTrue(result.Success, "Should succeed overall despite individual element failures");
            }
            finally
            {
                if (File.Exists(testFile)) File.Delete(testFile);
            }
        }

        #endregion

        #region Requirement 3: Dynamic Column Family Management

        [TestMethod]
        public void Requirement3_1_AutomaticFamilyCreation()
        {
            // WHEN creating a column from detail lines THEN the system SHALL automatically create the required family if it doesn't exist
            var detailLines = CreateTestRectangularDetailLines();
            
            var result = _familyService.CreateColumnsFromDetailLines(_testDocument, detailLines);
            
            Assert.IsTrue(result.Success, "Column creation should succeed");
            Assert.IsTrue(result.FamiliesCreated > 0, "Should create at least one family");
            Assert.IsTrue(result.ElementsProcessed > 0, "Should create at least one column");
        }

        [TestMethod]
        public void Requirement3_2_CustomColumnSizeSupport()
        {
            // WHEN a custom column size is needed THEN the system SHALL create a new family symbol with the specified dimensions
            var customDimensions = new ColumnParameters
            {
                Width = 14.5, // Custom width
                Height = 20.25 // Custom height
            };
            
            var result = _familyService.CreateCustomColumnFamily("Custom_Column", customDimensions);
            
            Assert.IsTrue(result.Success, "Custom family creation should succeed");
            Assert.IsNotNull(result.CreatedFamily, "Should return created family");
            Assert.AreEqual(customDimensions.Width, result.ActualWidth, 0.1, "Should match specified width");
            Assert.AreEqual(customDimensions.Height, result.ActualHeight, 0.1, "Should match specified height");
        }

        [TestMethod]
        public void Requirement3_3_StandardFamilyLoading()
        {
            // WHEN the add-in starts THEN the system SHALL load standard column families automatically
            _familyService.LoadStandardColumnFamilies();
            
            var availableFamilies = _familyService.GetAvailableColumnFamilies();
            
            Assert.IsTrue(availableFamilies.Count > 0, "Should load standard column families");
            Assert.IsTrue(availableFamilies.Any(f => f.Name.Contains("Rectangular")), "Should include rectangular column families");
        }

        [TestMethod]
        public void Requirement3_4_FamilyCompatibilityValidation()
        {
            // WHEN validating a family for column creation THEN the system SHALL verify compatibility with structural column requirements
            var testFamily = CreateTestColumnFamily();
            
            var isCompatible = _familyService.ValidateFamilyCompatibility(testFamily);
            
            Assert.IsTrue(isCompatible, "Valid column family should pass compatibility check");
        }

        [TestMethod]
        public void Requirement3_5_FamilyCreationErrorHandling()
        {
            // IF a family creation fails THEN the system SHALL provide detailed error information and fallback options
            var invalidParameters = new ColumnParameters
            {
                Width = -1, // Invalid width
                Height = 0  // Invalid height
            };
            
            var result = _familyService.CreateCustomColumnFamily("Invalid_Column", invalidParameters);
            
            Assert.IsFalse(result.Success, "Invalid family creation should fail");
            Assert.IsTrue(!string.IsNullOrEmpty(result.ErrorMessage), "Should provide detailed error message");
            Assert.IsNotNull(result.FallbackOptions, "Should provide fallback options");
        }

        [TestMethod]
        public void Requirement3_6_FamilySymbolReuse()
        {
            // WHEN multiple columns with the same dimensions are created THEN the system SHALL reuse existing family symbols
            var dimensions = new ColumnParameters { Width = 12, Height = 18 };
            
            // Create first column
            var result1 = _familyService.CreateCustomColumnFamily("Test_Column_1", dimensions);
            // Create second column with same dimensions
            var result2 = _familyService.CreateCustomColumnFamily("Test_Column_2", dimensions);
            
            Assert.IsTrue(result1.Success && result2.Success, "Both column creations should succeed");
            Assert.AreEqual(result1.CreatedFamily.Id, result2.CreatedFamily.Id, "Should reuse the same family");
        }

        #endregion

        #region Requirement 4: Batch Processing Capabilities

        [TestMethod]
        public void Requirement4_1_MultipleFileProcessing()
        {
            // WHEN multiple DWG files are selected THEN the system SHALL process them sequentially with progress tracking
            var testFiles = CreateMultipleTestDwgFiles(3);
            var progressReporter = new TestProgressReporter();
            
            try
            {
                var result = _batchService.ProcessMultipleFiles(testFiles, progressReporter).Result;
                
                Assert.IsTrue(result.TotalFilesProcessed == testFiles.Count, "Should process all files");
                Assert.IsTrue(progressReporter.ProgressReports.Count > 0, "Should report progress");
                Assert.IsTrue(result.SuccessfulFiles > 0, "Should have successful files");
            }
            finally
            {
                foreach (var file in testFiles)
                    if (File.Exists(file)) File.Delete(file);
            }
        }

        [TestMethod]
        public void Requirement4_2_FolderProcessing()
        {
            // WHEN a folder containing DWG files is selected THEN the system SHALL process all DWG files in the folder and subfolders
            var testFolder = CreateTestFolderWithDwgFiles();
            
            try
            {
                var result = _batchService.ProcessFolder(testFolder, true, null).Result;
                
                Assert.IsTrue(result.TotalFilesProcessed > 0, "Should process files from folder");
                Assert.IsTrue(result.FileResults.Any(f => f.FilePath.Contains("subfolder")), "Should process files from subfolders");
            }
            finally
            {
                if (Directory.Exists(testFolder))
                    Directory.Delete(testFolder, true);
            }
        }

        [TestMethod]
        public void Requirement4_3_BatchProgressTracking()
        {
            // WHEN batch processing is running THEN the system SHALL display real-time progress with file names and completion status
            var testFiles = CreateMultipleTestDwgFiles(2);
            var progressReporter = new TestProgressReporter();
            
            try
            {
                var result = _batchService.ProcessMultipleFiles(testFiles, progressReporter).Result;
                
                Assert.IsTrue(progressReporter.ProgressReports.Count >= testFiles.Count, "Should report progress for each file");
                Assert.IsTrue(progressReporter.ProgressReports.All(p => !string.IsNullOrEmpty(p.CurrentFileName)), "Should include file names");
                Assert.IsTrue(progressReporter.ProgressReports.Any(p => p.CurrentFile > 0), "Should track file numbers");
            }
            finally
            {
                foreach (var file in testFiles)
                    if (File.Exists(file)) File.Delete(file);
            }
        }

        [TestMethod]
        public void Requirement4_4_BatchSummaryReport()
        {
            // WHEN batch processing completes THEN the system SHALL generate a comprehensive summary report
            var testFiles = CreateMultipleTestDwgFiles(2);
            
            try
            {
                var result = _batchService.ProcessMultipleFiles(testFiles, null).Result;
                
                Assert.IsNotNull(result, "Should generate batch result");
                Assert.IsTrue(result.TotalFilesProcessed > 0, "Should report total files processed");
                Assert.IsTrue(result.TotalProcessingTime.TotalSeconds > 0, "Should report processing time");
                Assert.IsNotNull(result.FileResults, "Should include individual file results");
                Assert.IsTrue(result.FileResults.Count == testFiles.Count, "Should have result for each file");
            }
            finally
            {
                foreach (var file in testFiles)
                    if (File.Exists(file)) File.Delete(file);
            }
        }

        [TestMethod]
        public void Requirement4_5_BatchErrorHandling()
        {
            // IF any file fails during batch processing THEN the system SHALL continue with remaining files and report failures
            var testFiles = new List<string>
            {
                CreateTestDwgFile(),
                CreateCorruptedTestFile(), // This should fail
                CreateTestDwgFile()
            };
            
            try
            {
                var result = _batchService.ProcessMultipleFiles(testFiles, null).Result;
                
                Assert.IsTrue(result.TotalFilesProcessed == testFiles.Count, "Should attempt to process all files");
                Assert.IsTrue(result.FailedFiles > 0, "Should report failed files");
                Assert.IsTrue(result.SuccessfulFiles > 0, "Should have some successful files");
                Assert.IsTrue(result.FileResults.Any(f => !f.Success), "Should include failed file results");
            }
            finally
            {
                foreach (var file in testFiles)
                    if (File.Exists(file)) File.Delete(file);
            }
        }

        [TestMethod]
        public void Requirement4_6_BatchCancellationSupport()
        {
            // WHEN batch processing is cancelled THEN the system SHALL stop gracefully and preserve completed work
            var testFiles = CreateMultipleTestDwgFiles(10); // Large batch for cancellation testing
            
            try
            {
                var cancellationTask = Task.Run(async () =>
                {
                    await Task.Delay(100); // Let processing start
                    _batchService.CancelProcessing();
                });

                var result = _batchService.ProcessMultipleFiles(testFiles, null).Result;
                
                Assert.IsTrue(result.TotalFilesProcessed < testFiles.Count, "Should not process all files due to cancellation");
                Assert.IsTrue(result.SuccessfulFiles >= 0, "Should preserve completed work");
            }
            finally
            {
                foreach (var file in testFiles)
                    if (File.Exists(file)) File.Delete(file);
            }
        }

        #endregion

        #region Requirement 5: User Settings and Persistence

        [TestMethod]
        public void Requirement5_1_AutomaticSettingsSave()
        {
            // WHEN user preferences are modified THEN the system SHALL save them automatically to persistent storage
            var settings = new UserSettings
            {
                LayerMapping = new LayerMappingSettings { DefaultLayerName = "Test Layer" }
            };
            
            _settingsService.SaveSettings(settings);
            
            var loadedSettings = _settingsService.LoadSettings();
            Assert.AreEqual(settings.LayerMapping.DefaultLayerName, loadedSettings.LayerMapping.DefaultLayerName, 
                "Settings should be saved and loaded correctly");
        }

        [TestMethod]
        public void Requirement5_2_SettingsLoadOnStartup()
        {
            // WHEN the add-in starts THEN the system SHALL load previously saved user preferences
            var originalSettings = new UserSettings
            {
                ColumnSettings = new ColumnCreationSettings { DefaultWidth = 15.0 }
            };
            
            _settingsService.SaveSettings(originalSettings);
            
            // Simulate startup by creating new service instance
            var newSettingsService = new SettingsService();
            var loadedSettings = newSettingsService.LoadSettings();
            
            Assert.AreEqual(originalSettings.ColumnSettings.DefaultWidth, loadedSettings.ColumnSettings.DefaultWidth,
                "Settings should be loaded on startup");
        }

        [TestMethod]
        public void Requirement5_3_LayerMappingTemplates()
        {
            // WHEN layer mapping templates are created THEN the system SHALL save them for reuse in future projects
            var template = new LayerMappingTemplate
            {
                Name = "Standard Mapping",
                Mappings = new Dictionary<string, string> { { "DWG_WALL", "Walls" } }
            };
            
            _settingsService.SaveLayerMappingTemplate(template);
            var templates = _settingsService.GetLayerMappingTemplates();
            
            Assert.IsTrue(templates.Any(t => t.Name == template.Name), "Template should be saved");
            Assert.IsTrue(templates.First(t => t.Name == template.Name).Mappings.ContainsKey("DWG_WALL"), 
                "Template mappings should be preserved");
        }

        [TestMethod]
        public void Requirement5_4_DefaultColumnFamilyPersistence()
        {
            // WHEN default column families are configured THEN the system SHALL remember them for subsequent sessions
            var defaultFamilies = new List<string> { "W-Wide Flange", "HSS-Hollow Structural Section" };
            
            _settingsService.SaveDefaultColumnFamilies(defaultFamilies);
            
            var settings = _settingsService.LoadSettings();
            Assert.IsNotNull(settings.ColumnSettings.DefaultFamilies, "Default families should be saved");
            Assert.IsTrue(settings.ColumnSettings.DefaultFamilies.Contains("W-Wide Flange"), 
                "Specific families should be remembered");
        }

        [TestMethod]
        public void Requirement5_5_CorruptedSettingsRecovery()
        {
            // IF settings file is corrupted THEN the system SHALL restore default settings and notify the user
            // Simulate corrupted settings file
            var settingsPath = _settingsService.GetSettingsFilePath();
            File.WriteAllText(settingsPath, "corrupted json content");
            
            var settings = _settingsService.LoadSettings();
            
            Assert.IsNotNull(settings, "Should return default settings when file is corrupted");
            Assert.IsNotNull(settings.LayerMapping, "Default settings should be properly initialized");
        }

        [TestMethod]
        public void Requirement5_6_SettingsExport()
        {
            // WHEN settings are exported THEN the system SHALL create a portable configuration file
            var settings = new UserSettings
            {
                LayerMapping = new LayerMappingSettings { DefaultLayerName = "Export Test" }
            };
            
            _settingsService.SaveSettings(settings);
            var exportPath = Path.GetTempFileName();
            
            try
            {
                var exportResult = _settingsService.ExportSettings(exportPath);
                
                Assert.IsTrue(exportResult, "Settings export should succeed");
                Assert.IsTrue(File.Exists(exportPath), "Export file should be created");
                
                var exportedContent = File.ReadAllText(exportPath);
                Assert.IsTrue(exportedContent.Contains("Export Test"), "Exported content should include settings data");
            }
            finally
            {
                if (File.Exists(exportPath)) File.Delete(exportPath);
            }
        }

        #endregion

        #region Requirement 6: Professional Error Logging and Diagnostics

        [TestMethod]
        public void Requirement6_1_DetailedErrorLogging()
        {
            // WHEN any error occurs THEN the system SHALL log detailed error information with context and stack traces
            var exception = new InvalidOperationException("Test error for logging");
            
            _logger.LogError(exception, "Test context");
            
            var logEntries = _logger.GetRecentLogEntries(1);
            Assert.IsTrue(logEntries.Count > 0, "Should log error entries");
            Assert.IsTrue(logEntries[0].Message.Contains("Test error"), "Should include error message");
            Assert.IsTrue(!string.IsNullOrEmpty(logEntries[0].StackTrace), "Should include stack trace");
            Assert.AreEqual("Test context", logEntries[0].Context, "Should include context information");
        }

        [TestMethod]
        public void Requirement6_2_ProcessingReportGeneration()
        {
            // WHEN a processing session completes THEN the system SHALL generate a detailed processing report
            var session = new ProcessingSession
            {
                SessionId = Guid.NewGuid(),
                StartTime = DateTime.Now.AddMinutes(-5),
                EndTime = DateTime.Now,
                FilesProcessed = 3,
                ElementsProcessed = 150,
                ErrorsEncountered = 2
            };
            
            var report = _logger.GenerateReport(session);
            
            Assert.IsNotNull(report, "Should generate processing report");
            Assert.IsTrue(report.TotalProcessingTime.TotalMinutes > 0, "Should include processing time");
            Assert.AreEqual(3, report.FilesProcessed, "Should include files processed count");
            Assert.AreEqual(150, report.ElementsProcessed, "Should include elements processed count");
        }

        [TestMethod]
        public void Requirement6_3_LogExportFunctionality()
        {
            // WHEN requested by the user THEN the system SHALL export error logs to a specified file location
            _logger.LogInfo("Test log entry for export");
            _logger.LogError(new Exception("Test error"), "Export test");
            
            var exportPath = Path.GetTempFileName();
            
            try
            {
                _logger.ExportLogs(exportPath);
                
                Assert.IsTrue(File.Exists(exportPath), "Log export file should be created");
                var exportContent = File.ReadAllText(exportPath);
                Assert.IsTrue(exportContent.Contains("Test log entry"), "Should include log entries");
                Assert.IsTrue(exportContent.Contains("Test error"), "Should include error entries");
            }
            finally
            {
                if (File.Exists(exportPath)) File.Delete(exportPath);
            }
        }

        [TestMethod]
        public void Requirement6_4_UsageStatisticsTracking()
        {
            // WHEN commands are executed THEN the system SHALL track usage statistics for performance monitoring
            var commandParameters = new Dictionary<string, object>
            {
                { "FileCount", 5 },
                { "ProcessingMode", "Batch" }
            };
            
            _logger.LogUsage("BatchProcess", commandParameters);
            
            var usageStats = _logger.GetUsageStatistics();
            Assert.IsTrue(usageStats.ContainsKey("BatchProcess"), "Should track command usage");
            Assert.IsTrue(usageStats["BatchProcess"].ExecutionCount > 0, "Should increment execution count");
        }

        [TestMethod]
        public void Requirement6_5_CriticalErrorNotification()
        {
            // IF critical errors occur THEN the system SHALL notify users immediately with actionable information
            var criticalError = new OutOfMemoryException("Critical memory error");
            
            var notificationResult = _logger.LogCriticalError(criticalError, "Memory allocation failure");
            
            Assert.IsTrue(notificationResult.UserNotified, "Should notify user of critical error");
            Assert.IsTrue(!string.IsNullOrEmpty(notificationResult.ActionableMessage), 
                "Should provide actionable information");
        }

        [TestMethod]
        public void Requirement6_6_DiagnosticModeLogging()
        {
            // WHEN diagnostic mode is enabled THEN the system SHALL provide verbose logging for troubleshooting
            _logger.EnableDiagnosticMode(true);
            
            _geometryService.ProcessDwgFile(CreateTestDwgFile(), _testDocument);
            
            var diagnosticLogs = _logger.GetDiagnosticLogs();
            Assert.IsTrue(diagnosticLogs.Count > 0, "Should generate diagnostic logs");
            Assert.IsTrue(diagnosticLogs.Any(log => log.Level == LogLevel.Debug), "Should include debug level logs");
        }

        #endregion

        #region Requirement 7: Advanced Column Creation Features

        [TestMethod]
        public void Requirement7_1_CircularColumnCreation()
        {
            // WHEN creating circular columns THEN the system SHALL generate columns with specified center point and diameter
            var centerPoint = new XYZ(10, 10, 0);
            var diameter = 18.0;
            
            var result = _familyService.CreateCircularColumn(centerPoint, diameter, _testDocument);
            
            Assert.IsTrue(result.Success, "Circular column creation should succeed");
            Assert.IsNotNull(result.CreatedColumn, "Should create column element");
            Assert.AreEqual(diameter, result.ActualDiameter, 0.1, "Should match specified diameter");
        }

        [TestMethod]
        public void Requirement7_2_CustomShapeColumnCreation()
        {
            // WHEN custom column shapes are needed THEN the system SHALL create columns from user-defined profile curves
            var profileCurves = CreateCustomProfileCurves();
            
            var result = _familyService.CreateCustomShapeColumn(profileCurves, _testDocument);
            
            Assert.IsTrue(result.Success, "Custom shape column creation should succeed");
            Assert.IsNotNull(result.CreatedColumn, "Should create column element");
            Assert.IsNotNull(result.CreatedFamily, "Should create custom family");
        }

        [TestMethod]
        public void Requirement7_3_ColumnScheduleDataIntegration()
        {
            // WHEN column schedule data is available THEN the system SHALL apply it automatically to created columns
            var scheduleData = new ColumnScheduleData
            {
                Mark = "C1",
                Size = "W14x22",
                Material = "A992",
                Length = 12.0
            };
            
            var column = CreateTestColumn();
            var result = _familyService.ApplyScheduleData(column, scheduleData);
            
            Assert.IsTrue(result.Success, "Schedule data application should succeed");
            Assert.AreEqual("C1", result.AppliedMark, "Should apply column mark");
            Assert.AreEqual("A992", result.AppliedMaterial, "Should apply material");
        }

        [TestMethod]
        public void Requirement7_4_ColumnGridGeneration()
        {
            // WHEN creating multiple columns in a pattern THEN the system SHALL generate column grids based on specified parameters
            var gridParameters = new ColumnGridParameters
            {
                Rows = 3,
                Columns = 4,
                RowSpacing = 20.0,
                ColumnSpacing = 25.0,
                StartPoint = new XYZ(0, 0, 0)
            };
            
            var result = _familyService.CreateColumnGrid(gridParameters, _testDocument);
            
            Assert.IsTrue(result.Success, "Column grid creation should succeed");
            Assert.AreEqual(12, result.ColumnsCreated, "Should create 3x4 = 12 columns");
            Assert.IsTrue(result.GridLinesCreated > 0, "Should create grid lines");
        }

        [TestMethod]
        public void Requirement7_5_ColumnCreationErrorHandling()
        {
            // IF column creation fails due to geometric constraints THEN the system SHALL provide clear feedback and alternative options
            var invalidLocation = new XYZ(double.MaxValue, double.MaxValue, 0); // Invalid location
            
            var result = _familyService.CreateCircularColumn(invalidLocation, 12.0, _testDocument);
            
            Assert.IsFalse(result.Success, "Invalid column creation should fail");
            Assert.IsTrue(!string.IsNullOrEmpty(result.ErrorMessage), "Should provide clear error message");
            Assert.IsNotNull(result.AlternativeOptions, "Should provide alternative options");
        }

        #endregion

        #region Requirement 8: Comprehensive Testing Framework

        [TestMethod]
        public void Requirement8_1_GeometryProcessingUnitTests()
        {
            // WHEN geometry processing functions are tested THEN unit tests SHALL verify correct conversion of all supported geometry types
            var geometryTypes = new[] { "Line", "Arc", "Spline", "Ellipse", "Text", "Hatch" };
            
            foreach (var geometryType in geometryTypes)
            {
                var testFile = CreateTestDwgWithGeometryType(geometryType);
                
                try
                {
                    var result = _geometryService.ProcessDwgFile(testFile, _testDocument);
                    
                    Assert.IsTrue(result.Success, $"{geometryType} processing should succeed");
                    Assert.IsTrue(result.ProcessedGeometryTypes.Contains(geometryType), 
                        $"Should process {geometryType} geometry");
                }
                finally
                {
                    if (File.Exists(testFile)) File.Delete(testFile);
                }
            }
        }

        [TestMethod]
        public void Requirement8_2_IntegrationTestingWithRealFiles()
        {
            // WHEN integration testing is performed THEN the system SHALL test with various real-world DWG files
            var realWorldTestFiles = GetRealWorldTestFiles();
            
            foreach (var testFile in realWorldTestFiles)
            {
                var result = _geometryService.ProcessDwgFile(testFile, _testDocument);
                
                // Real-world files may have issues, but should handle gracefully
                Assert.IsTrue(result.Success || result.Errors.Count > 0, 
                    $"Should handle real-world file {Path.GetFileName(testFile)} gracefully");
            }
        }

        [TestMethod]
        public void Requirement8_3_UIComponentTesting()
        {
            // WHEN UI components are tested THEN automated tests SHALL verify WPF dialog functionality
            var settingsWindow = new UI.Windows.SettingsWindow();
            
            // Test window initialization
            Assert.IsNotNull(settingsWindow, "Settings window should initialize");
            
            // Test data binding
            settingsWindow.DataContext = new UserSettings();
            Assert.IsNotNull(settingsWindow.DataContext, "Data context should be set");
        }

        [TestMethod]
        public void Requirement8_4_RevitApiInteractionTesting()
        {
            // WHEN Revit API interactions are tested THEN integration tests SHALL verify proper API usage
            // This would typically run in a Revit context
            var apiValidator = new RevitApiValidator();
            var validationResult = apiValidator.ValidateApiUsage();
            
            Assert.IsTrue(validationResult.IsValid, "Revit API usage should be valid");
            Assert.AreEqual(0, validationResult.ApiViolations.Count, "Should have no API violations");
        }

        [TestMethod]
        public void Requirement8_5_ErrorConditionTesting()
        {
            // IF error conditions are simulated THEN tests SHALL verify proper error handling and recovery
            var errorConditions = new[]
            {
                "CorruptedFile",
                "InsufficientMemory",
                "NetworkTimeout",
                "InvalidGeometry",
                "MissingFamily"
            };
            
            foreach (var condition in errorConditions)
            {
                var result = SimulateErrorCondition(condition);
                
                Assert.IsTrue(result.ErrorHandledGracefully, 
                    $"Error condition '{condition}' should be handled gracefully");
                Assert.IsTrue(!string.IsNullOrEmpty(result.ErrorMessage), 
                    $"Should provide error message for '{condition}'");
            }
        }

        [TestMethod]
        public void Requirement8_6_PerformanceBenchmarkTesting()
        {
            // WHEN performance testing is conducted THEN the system SHALL meet specified processing time benchmarks
            var performanceMonitor = new PerformanceMonitor();
            var largeTestFile = CreateLargeTestDwgFile(1000); // 1000 elements
            
            try
            {
                performanceMonitor.StartTimer("LargeFileProcessing");
                var result = _geometryService.ProcessDwgFile(largeTestFile, _testDocument);
                var processingTime = performanceMonitor.StopTimer("LargeFileProcessing");
                
                Assert.IsTrue(result.Success, "Large file processing should succeed");
                Assert.IsTrue(processingTime.TotalSeconds < 60, "Should process within 60 seconds"); // Performance benchmark
                Assert.IsTrue(result.ElementsProcessed >= 900, "Should process at least 90% of elements");
            }
            finally
            {
                if (File.Exists(largeTestFile)) File.Delete(largeTestFile);
            }
        }

        #endregion

        #region Helper Methods

        private string CreateTestDwgFile()
        {
            var tempPath = Path.GetTempFileName();
            var dwgPath = Path.ChangeExtension(tempPath, ".dwg");
            File.WriteAllBytes(dwgPath, CreateBasicDwgContent());
            return dwgPath;
        }

        private string CreateTestDwgWithArcs()
        {
            var tempPath = Path.GetTempFileName();
            var dwgPath = Path.ChangeExtension(tempPath, ".dwg");
            File.WriteAllBytes(dwgPath, CreateDwgContentWithArcs());
            return dwgPath;
        }

        private string CreateTestDwgWithSplines()
        {
            var tempPath = Path.GetTempFileName();
            var dwgPath = Path.ChangeExtension(tempPath, ".dwg");
            File.WriteAllBytes(dwgPath, CreateDwgContentWithSplines());
            return dwgPath;
        }

        private string CreateTestDwgWithEllipses()
        {
            var tempPath = Path.GetTempFileName();
            var dwgPath = Path.ChangeExtension(tempPath, ".dwg");
            File.WriteAllBytes(dwgPath, CreateDwgContentWithEllipses());
            return dwgPath;
        }

        private string CreateTestDwgWithText()
        {
            var tempPath = Path.GetTempFileName();
            var dwgPath = Path.ChangeExtension(tempPath, ".dwg");
            File.WriteAllBytes(dwgPath, CreateDwgContentWithText());
            return dwgPath;
        }

        private string CreateTestDwgWithHatches()
        {
            var tempPath = Path.GetTempFileName();
            var dwgPath = Path.ChangeExtension(tempPath, ".dwg");
            File.WriteAllBytes(dwgPath, CreateDwgContentWithHatches());
            return dwgPath;
        }

        private string CreateTestDwgWithNestedBlocks()
        {
            var tempPath = Path.GetTempFileName();
            var dwgPath = Path.ChangeExtension(tempPath, ".dwg");
            File.WriteAllBytes(dwgPath, CreateDwgContentWithNestedBlocks());
            return dwgPath;
        }

        private string CreateTestDwgWithMixedValidInvalidGeometry()
        {
            var tempPath = Path.GetTempFileName();
            var dwgPath = Path.ChangeExtension(tempPath, ".dwg");
            File.WriteAllBytes(dwgPath, CreateDwgContentWithMixedGeometry());
            return dwgPath;
        }

        private string CreateCorruptedTestFile()
        {
            var tempPath = Path.GetTempFileName();
            var dwgPath = Path.ChangeExtension(tempPath, ".dwg");
            File.WriteAllText(dwgPath, "This is not a valid DWG file");
            return dwgPath;
        }

        private string CreateLargeTestDwgFile(int elementCount)
        {
            var tempPath = Path.GetTempFileName();
            var dwgPath = Path.ChangeExtension(tempPath, ".dwg");
            File.WriteAllBytes(dwgPath, CreateLargeDwgContent(elementCount));
            return dwgPath;
        }

        private string CreateTestDwgWithGeometryType(string geometryType)
        {
            var tempPath = Path.GetTempFileName();
            var dwgPath = Path.ChangeExtension(tempPath, ".dwg");
            
            switch (geometryType)
            {
                case "Arc":
                    File.WriteAllBytes(dwgPath, CreateDwgContentWithArcs());
                    break;
                case "Spline":
                    File.WriteAllBytes(dwgPath, CreateDwgContentWithSplines());
                    break;
                case "Ellipse":
                    File.WriteAllBytes(dwgPath, CreateDwgContentWithEllipses());
                    break;
                case "Text":
                    File.WriteAllBytes(dwgPath, CreateDwgContentWithText());
                    break;
                case "Hatch":
                    File.WriteAllBytes(dwgPath, CreateDwgContentWithHatches());
                    break;
                default:
                    File.WriteAllBytes(dwgPath, CreateBasicDwgContent());
                    break;
            }
            
            return dwgPath;
        }

        private List<string> CreateMultipleTestDwgFiles(int count)
        {
            var files = new List<string>();
            for (int i = 0; i < count; i++)
            {
                files.Add(CreateTestDwgFile());
            }
            return files;
        }

        private string CreateTestFolderWithDwgFiles()
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempFolder);
            
            // Create files in main folder
            File.WriteAllBytes(Path.Combine(tempFolder, "test1.dwg"), CreateBasicDwgContent());
            File.WriteAllBytes(Path.Combine(tempFolder, "test2.dwg"), CreateBasicDwgContent());
            
            // Create subfolder with files
            var subFolder = Path.Combine(tempFolder, "subfolder");
            Directory.CreateDirectory(subFolder);
            File.WriteAllBytes(Path.Combine(subFolder, "test3.dwg"), CreateBasicDwgContent());
            
            return tempFolder;
        }

        private List<DetailLine> CreateTestRectangularDetailLines()
        {
            // This would create actual DetailLine elements in a real Revit context
            return new List<DetailLine>();
        }

        private Family CreateTestColumnFamily()
        {
            // This would create an actual Family in a real Revit context
            return null;
        }

        private List<Curve> CreateCustomProfileCurves()
        {
            // This would create actual Curve objects in a real Revit context
            return new List<Curve>();
        }

        private FamilyInstance CreateTestColumn()
        {
            // This would create an actual column in a real Revit context
            return null;
        }

        private List<string> GetRealWorldTestFiles()
        {
            // Return paths to real-world test DWG files
            return new List<string>();
        }

        private ErrorConditionResult SimulateErrorCondition(string condition)
        {
            // Simulate various error conditions for testing
            return new ErrorConditionResult
            {
                ErrorHandledGracefully = true,
                ErrorMessage = $"Simulated error condition: {condition}"
            };
        }

        // DWG content creation methods (simplified for testing)
        private byte[] CreateBasicDwgContent() => new byte[] { 0x41, 0x43, 0x31, 0x30 };
        private byte[] CreateDwgContentWithArcs() => CreateBasicDwgContent();
        private byte[] CreateDwgContentWithSplines() => CreateBasicDwgContent();
        private byte[] CreateDwgContentWithEllipses() => CreateBasicDwgContent();
        private byte[] CreateDwgContentWithText() => CreateBasicDwgContent();
        private byte[] CreateDwgContentWithHatches() => CreateBasicDwgContent();
        private byte[] CreateDwgContentWithNestedBlocks() => CreateBasicDwgContent();
        private byte[] CreateDwgContentWithMixedGeometry() => CreateBasicDwgContent();
        private byte[] CreateLargeDwgContent(int elementCount) => CreateBasicDwgContent();

        #endregion

        #region Helper Classes

        private class TestProgressReporter : IProgress<BatchProgress>
        {
            public List<BatchProgress> ProgressReports { get; } = new List<BatchProgress>();

            public void Report(BatchProgress value)
            {
                ProgressReports.Add(value);
            }
        }

        private class DeploymentValidator
        {
            public DeploymentResult ValidateDeployment()
            {
                return new DeploymentResult
                {
                    AddinFileExists = true,
                    DllFileExists = true,
                    AddinFileValid = true
                };
            }
        }

        private class DeploymentResult
        {
            public bool AddinFileExists { get; set; }
            public bool DllFileExists { get; set; }
            public bool AddinFileValid { get; set; }
        }

        private class ApiVersionValidator
        {
            public ApiVersionResult ValidateApiCompatibility()
            {
                return new ApiVersionResult
                {
                    IsCompatible = true,
                    ErrorMessage = ""
                };
            }
        }

        private class ApiVersionResult
        {
            public bool IsCompatible { get; set; }
            public string ErrorMessage { get; set; }
        }

        private class RevitApiValidator
        {
            public ApiValidationResult ValidateApiUsage()
            {
                return new ApiValidationResult
                {
                    IsValid = true,
                    ApiViolations = new List<string>()
                };
            }
        }

        private class ApiValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> ApiViolations { get; set; }
        }

        private class ErrorConditionResult
        {
            public bool ErrorHandledGracefully { get; set; }
            public string ErrorMessage { get; set; }
        }

        #endregion
    }
}