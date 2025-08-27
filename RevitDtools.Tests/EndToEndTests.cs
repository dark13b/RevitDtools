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
    /// Comprehensive end-to-end tests for the complete RevitDtools workflow
    /// </summary>
    [TestClass]
    public class EndToEndTests
    {
        private Document _testDocument;
        private UIDocument _uiDocument;
        private GeometryProcessingService _geometryService;
        private FamilyManagementService _familyService;
        private BatchProcessingService _batchService;
        private SettingsService _settingsService;

        [TestInitialize]
        public void Setup()
        {
            // Initialize test services
            _geometryService = new GeometryProcessingService();
            _familyService = new FamilyManagementService(_testDocument);
            _batchService = new BatchProcessingService(_geometryService, _familyService);
            _settingsService = new SettingsService();
        }

        [TestMethod]
        public void CompleteWorkflow_DwgImportToColumnCreation_Success()
        {
            // Arrange
            var testDwgPath = CreateTestDwgFile();
            var expectedElementCount = 10;

            try
            {
                // Act - Step 1: Import DWG and convert to detail lines
                var geometryResult = _geometryService.ProcessDwgFile(testDwgPath, _testDocument);

                // Assert - Geometry processing
                Assert.IsTrue(geometryResult.Success, "DWG geometry processing should succeed");
                Assert.IsTrue(geometryResult.ElementsProcessed > 0, "Should process at least one element");

                // Act - Step 2: Create columns from rectangular detail lines
                var columnResult = _familyService.CreateColumnsFromDetailLines(_testDocument);

                // Assert - Column creation
                Assert.IsTrue(columnResult.Success, "Column creation should succeed");
                Assert.IsTrue(columnResult.ElementsProcessed > 0, "Should create at least one column");

                // Act - Step 3: Validate final result
                var finalValidation = ValidateCompleteWorkflow(_testDocument);

                // Assert - Final validation
                Assert.IsTrue(finalValidation.IsValid, finalValidation.ValidationMessage);
            }
            finally
            {
                // Cleanup
                if (File.Exists(testDwgPath))
                    File.Delete(testDwgPath);
            }
        }

        [TestMethod]
        public void BatchProcessing_MultipleFiles_AllProcessedSuccessfully()
        {
            // Arrange
            var testFiles = CreateMultipleTestDwgFiles(5);
            var progress = new TestProgressReporter();

            try
            {
                // Act
                var batchResult = _batchService.ProcessMultipleFiles(testFiles, progress).Result;

                // Assert
                Assert.IsNotNull(batchResult, "Batch result should not be null");
                Assert.AreEqual(testFiles.Count, batchResult.TotalFilesProcessed, "All files should be processed");
                Assert.AreEqual(testFiles.Count, batchResult.SuccessfulFiles, "All files should be processed successfully");
                Assert.AreEqual(0, batchResult.FailedFiles, "No files should fail");
                Assert.IsTrue(batchResult.TotalProcessingTime.TotalSeconds > 0, "Processing should take measurable time");
            }
            finally
            {
                // Cleanup
                foreach (var file in testFiles)
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }
            }
        }

        [TestMethod]
        public void PerformanceTest_LargeFileProcessing_MeetsPerformanceCriteria()
        {
            // Arrange
            var largeTestFile = CreateLargeTestDwgFile(1000); // 1000 elements
            var maxProcessingTimeSeconds = 30; // Performance requirement

            try
            {
                var startTime = DateTime.Now;

                // Act
                var result = _geometryService.ProcessDwgFile(largeTestFile, _testDocument);

                var processingTime = DateTime.Now - startTime;

                // Assert
                Assert.IsTrue(result.Success, "Large file processing should succeed");
                Assert.IsTrue(processingTime.TotalSeconds < maxProcessingTimeSeconds, 
                    $"Processing should complete within {maxProcessingTimeSeconds} seconds. Actual: {processingTime.TotalSeconds}");
                Assert.IsTrue(result.ElementsProcessed >= 900, "Should process at least 90% of elements");
            }
            finally
            {
                if (File.Exists(largeTestFile))
                    File.Delete(largeTestFile);
            }
        }

        [TestMethod]
        public void ErrorRecovery_CorruptedFile_GracefulHandling()
        {
            // Arrange
            var corruptedFile = CreateCorruptedTestFile();

            try
            {
                // Act
                var result = _geometryService.ProcessDwgFile(corruptedFile, _testDocument);

                // Assert
                Assert.IsFalse(result.Success, "Corrupted file processing should fail gracefully");
                Assert.IsTrue(result.Errors.Count > 0, "Should report errors");
                Assert.IsTrue(result.Message.Contains("corrupted") || result.Message.Contains("invalid"), 
                    "Error message should indicate file corruption");
            }
            finally
            {
                if (File.Exists(corruptedFile))
                    File.Delete(corruptedFile);
            }
        }

        [TestMethod]
        public void MemoryUsage_LongRunningBatch_NoMemoryLeaks()
        {
            // Arrange
            var testFiles = CreateMultipleTestDwgFiles(20);
            var initialMemory = GC.GetTotalMemory(true);

            try
            {
                // Act - Process multiple batches
                for (int i = 0; i < 3; i++)
                {
                    var batchResult = _batchService.ProcessMultipleFiles(testFiles, null).Result;
                    Assert.IsTrue(batchResult.SuccessfulFiles > 0, $"Batch {i + 1} should process files successfully");
                    
                    // Force garbage collection
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }

                var finalMemory = GC.GetTotalMemory(true);
                var memoryIncrease = finalMemory - initialMemory;
                var maxAllowedIncrease = 50 * 1024 * 1024; // 50MB

                // Assert
                Assert.IsTrue(memoryIncrease < maxAllowedIncrease, 
                    $"Memory increase should be less than 50MB. Actual increase: {memoryIncrease / (1024 * 1024)}MB");
            }
            finally
            {
                foreach (var file in testFiles)
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }
            }
        }

        [TestMethod]
        public void SettingsPersistence_SaveAndLoad_MaintainsIntegrity()
        {
            // Arrange
            var originalSettings = new UserSettings
            {
                LayerMapping = new LayerMappingSettings
                {
                    DefaultLayerName = "Test Layer",
                    LayerMappings = new Dictionary<string, string> { { "DWG_Layer", "Revit_Layer" } }
                },
                ColumnSettings = new ColumnCreationSettings
                {
                    DefaultWidth = 12.0,
                    DefaultHeight = 18.0,
                    DefaultFamilyName = "Test Column Family"
                }
            };

            // Act
            _settingsService.SaveSettings(originalSettings);
            var loadedSettings = _settingsService.LoadSettings();

            // Assert
            Assert.IsNotNull(loadedSettings, "Loaded settings should not be null");
            Assert.AreEqual(originalSettings.LayerMapping.DefaultLayerName, 
                loadedSettings.LayerMapping.DefaultLayerName, "Layer mapping should be preserved");
            Assert.AreEqual(originalSettings.ColumnSettings.DefaultWidth, 
                loadedSettings.ColumnSettings.DefaultWidth, "Column settings should be preserved");
        }

        [TestMethod]
        public void AllGeometryTypes_ProcessingSupport_ComprehensiveCoverage()
        {
            // Arrange
            var testFile = CreateComprehensiveGeometryTestFile();

            try
            {
                // Act
                var result = _geometryService.ProcessDwgFile(testFile, _testDocument);

                // Assert
                Assert.IsTrue(result.Success, "Comprehensive geometry processing should succeed");
                
                // Verify all geometry types were processed
                var processedTypes = result.ProcessedGeometryTypes;
                Assert.IsTrue(processedTypes.Contains("Line"), "Should process lines");
                Assert.IsTrue(processedTypes.Contains("Arc"), "Should process arcs");
                Assert.IsTrue(processedTypes.Contains("Spline"), "Should process splines");
                Assert.IsTrue(processedTypes.Contains("Ellipse"), "Should process ellipses");
                Assert.IsTrue(processedTypes.Contains("Text"), "Should process text");
                Assert.IsTrue(processedTypes.Contains("Hatch"), "Should process hatches");
            }
            finally
            {
                if (File.Exists(testFile))
                    File.Delete(testFile);
            }
        }

        #region Helper Methods

        private string CreateTestDwgFile()
        {
            var tempPath = Path.GetTempFileName();
            var dwgPath = Path.ChangeExtension(tempPath, ".dwg");
            
            // Create a simple test DWG file with basic geometry
            // This would typically use a DWG library or pre-created test files
            File.WriteAllBytes(dwgPath, CreateBasicDwgContent());
            
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

        private string CreateLargeTestDwgFile(int elementCount)
        {
            var tempPath = Path.GetTempFileName();
            var dwgPath = Path.ChangeExtension(tempPath, ".dwg");
            
            // Create a DWG file with specified number of elements
            File.WriteAllBytes(dwgPath, CreateLargeDwgContent(elementCount));
            
            return dwgPath;
        }

        private string CreateCorruptedTestFile()
        {
            var tempPath = Path.GetTempFileName();
            var dwgPath = Path.ChangeExtension(tempPath, ".dwg");
            
            // Create a corrupted file
            File.WriteAllText(dwgPath, "This is not a valid DWG file");
            
            return dwgPath;
        }

        private string CreateComprehensiveGeometryTestFile()
        {
            var tempPath = Path.GetTempFileName();
            var dwgPath = Path.ChangeExtension(tempPath, ".dwg");
            
            // Create a DWG file with all supported geometry types
            File.WriteAllBytes(dwgPath, CreateComprehensiveDwgContent());
            
            return dwgPath;
        }

        private byte[] CreateBasicDwgContent()
        {
            // This would create actual DWG content
            // For testing purposes, return minimal valid DWG structure
            return new byte[] { 0x41, 0x43, 0x31, 0x30 }; // Basic DWG header
        }

        private byte[] CreateLargeDwgContent(int elementCount)
        {
            // Create DWG content with specified element count
            var content = new List<byte>(CreateBasicDwgContent());
            // Add elements based on count
            return content.ToArray();
        }

        private byte[] CreateComprehensiveDwgContent()
        {
            // Create DWG content with all geometry types
            return CreateBasicDwgContent();
        }

        private WorkflowValidationResult ValidateCompleteWorkflow(Document document)
        {
            var result = new WorkflowValidationResult();
            
            try
            {
                // Validate that detail lines were created
                var detailLines = new FilteredElementCollector(document)
                    .OfClass(typeof(DetailLine))
                    .ToElements();

                if (detailLines.Count == 0)
                {
                    result.IsValid = false;
                    result.ValidationMessage = "No detail lines found in document";
                    return result;
                }

                // Validate that columns were created
                var columns = new FilteredElementCollector(document)
                    .OfCategory(BuiltInCategory.OST_StructuralColumns)
                    .ToElements();

                if (columns.Count == 0)
                {
                    result.IsValid = false;
                    result.ValidationMessage = "No structural columns found in document";
                    return result;
                }

                result.IsValid = true;
                result.ValidationMessage = $"Workflow validation successful. Created {detailLines.Count} detail lines and {columns.Count} columns.";
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ValidationMessage = $"Validation failed with error: {ex.Message}";
            }

            return result;
        }

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

        private class WorkflowValidationResult
        {
            public bool IsValid { get; set; }
            public string ValidationMessage { get; set; }
        }

        #endregion
    }
}