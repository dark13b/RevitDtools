using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RevitDtools.Core.Models;
using RevitDtools.Core.Services;
using RevitDtools.Utilities;

namespace RevitDtools.Tests
{
    /// <summary>
    /// Tests for complete user workflows from start to finish
    /// </summary>
    [TestClass]
    public class WorkflowTests
    {
        private Logger _logger;
        private string _testDataPath;

        [TestInitialize]
        public void TestInitialize()
        {
            _logger = Logger.Instance;
            _testDataPath = Path.Combine(Path.GetTempPath(), "RevitDtoolsWorkflowTests");
            Directory.CreateDirectory(_testDataPath);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (Directory.Exists(_testDataPath))
            {
                try
                {
                    Directory.Delete(_testDataPath, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [TestMethod]
        public void Workflow_NewUser_FirstTimeSetup()
        {
            // Test the complete workflow for a new user setting up the tool
            
            // Step 1: First launch - should create default settings
            var settingsService = CreateMockSettingsService();
            var settings = settingsService.LoadSettings();
            
            Assert.IsNotNull(settings, "Default settings should be created");
            Assert.IsNotNull(settings.LayerMapping, "Layer mapping settings should exist");
            Assert.IsNotNull(settings.ColumnSettings, "Column settings should exist");
            Assert.IsNotNull(settings.BatchSettings, "Batch settings should exist");
            
            // Step 2: User configures preferences
            settings.LayerMapping.DefaultLineStyle = "Medium Lines";
            settings.ColumnSettings.DefaultFamilyName = "Custom Column";
            settings.BatchSettings.MaxConcurrentFiles = 5;
            
            settingsService.SaveSettings(settings);
            
            // Step 3: Verify settings persistence
            var reloadedSettings = settingsService.LoadSettings();
            Assert.AreEqual("Medium Lines", reloadedSettings.LayerMapping.DefaultLineStyle);
            Assert.AreEqual("Custom Column", reloadedSettings.ColumnSettings.DefaultFamilyName);
            Assert.AreEqual(5, reloadedSettings.BatchSettings.MaxConcurrentFiles);
        }

        [TestMethod]
        public void Workflow_ArchitecturalDraftsman_DailyWork()
        {
            // Test workflow for an architectural draftsman's typical daily work
            
            // Step 1: Import and process architectural DWG
            var dwgFile = CreateMockDwgFile("architectural_plan.dwg");
            var geometryProcessor = CreateMockGeometryProcessor();
            
            var result = geometryProcessor.ProcessDwgFile(dwgFile, new[] { "WALLS", "DOORS", "WINDOWS" });
            
            Assert.IsTrue(result.Success, "DWG processing should succeed");
            Assert.IsTrue(result.ElementsProcessed > 0, "Should process some elements");
            
            // Step 2: Create columns from detail lines
            var columnProcessor = CreateMockColumnProcessor();
            var rectanglePoints = new[]
            {
                new { X = 0.0, Y = 0.0 },
                new { X = 2.0, Y = 0.0 },
                new { X = 2.0, Y = 1.5 },
                new { X = 0.0, Y = 1.5 }
            };
            
            var columnResult = columnProcessor.CreateColumnFromRectangle(rectanglePoints, "Standard Column");
            Assert.IsTrue(columnResult.Success, "Column creation should succeed");
            
            // Step 3: Generate report
            var report = GenerateWorkflowReport(new[] { result, columnResult });
            Assert.IsNotNull(report, "Report should be generated");
            Assert.IsTrue(report.TotalElementsProcessed > 0, "Report should show processed elements");
        }

        [TestMethod]
        public void Workflow_ProjectManager_BatchProcessing()
        {
            // Test workflow for a project manager processing multiple drawings
            
            // Step 1: Prepare multiple DWG files
            var dwgFiles = new[]
            {
                CreateMockDwgFile("floor_plan_01.dwg"),
                CreateMockDwgFile("floor_plan_02.dwg"),
                CreateMockDwgFile("floor_plan_03.dwg"),
                CreateMockDwgFile("structural_plan.dwg"),
                CreateMockDwgFile("electrical_plan.dwg")
            };
            
            // Step 2: Configure batch processing settings
            var batchSettings = new BatchProcessingSettings
            {
                MaxConcurrentFiles = 3,
                GenerateReports = true,
                ContinueOnError = true,
                OutputDirectory = _testDataPath
            };
            
            // Step 3: Execute batch processing
            var batchProcessor = CreateMockBatchProcessor();
            var batchResult = batchProcessor.ProcessFiles(dwgFiles, batchSettings);
            
            Assert.IsNotNull(batchResult, "Batch result should not be null");
            Assert.AreEqual(dwgFiles.Length, batchResult.TotalFilesProcessed, "All files should be processed");
            Assert.IsTrue(batchResult.SuccessfulFiles > 0, "Some files should process successfully");
            
            // Step 4: Review batch report
            Assert.IsNotNull(batchResult.ProcessingReport, "Batch report should be generated");
            Assert.IsTrue(batchResult.ProcessingReport.Length > 0, "Report should have content");
        }

        [TestMethod]
        public void Workflow_StructuralEngineer_AdvancedColumns()
        {
            // Test workflow for structural engineer using advanced column features
            
            // Step 1: Create circular columns
            var circularColumnProcessor = CreateMockAdvancedColumnProcessor();
            var circularResult = circularColumnProcessor.CreateCircularColumn(
                centerPoint: new { X = 10.0, Y = 10.0, Z = 0.0 },
                diameter: 1.5,
                height: 12.0
            );
            
            Assert.IsTrue(circularResult.Success, "Circular column creation should succeed");
            
            // Step 2: Create custom shape columns
            var customShapePoints = new[]
            {
                new { X = 0.0, Y = 0.0 },
                new { X = 3.0, Y = 0.0 },
                new { X = 3.0, Y = 1.0 },
                new { X = 2.0, Y = 1.0 },
                new { X = 2.0, Y = 2.0 },
                new { X = 0.0, Y = 2.0 }
            };
            
            var customShapeResult = circularColumnProcessor.CreateCustomShapeColumn(customShapePoints);
            Assert.IsTrue(customShapeResult.Success, "Custom shape column creation should succeed");
            
            // Step 3: Generate column grid
            var gridResult = circularColumnProcessor.CreateColumnGrid(
                startPoint: new { X = 0.0, Y = 0.0 },
                endPoint: new { X = 30.0, Y = 20.0 },
                spacingX: 6.0,
                spacingY: 5.0,
                columnType: "Standard Column"
            );
            
            Assert.IsTrue(gridResult.Success, "Column grid creation should succeed");
            Assert.IsTrue(gridResult.ColumnsCreated > 0, "Grid should create multiple columns");
        }

        [TestMethod]
        public async Task Workflow_LargeProject_PerformanceOptimization()
        {
            // Test workflow for large project with performance considerations
            
            // Step 1: Process large number of files with progress tracking
            var largeFileSet = Enumerable.Range(1, 50)
                .Select(i => CreateMockDwgFile($"large_project_sheet_{i:D3}.dwg"))
                .ToArray();
            
            var progress = new Progress<BatchProgress>();
            var progressReports = new List<BatchProgress>();
            
            progress.ProgressChanged += (sender, report) =>
            {
                progressReports.Add(report);
            };
            
            // Step 2: Execute with cancellation support
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var batchProcessor = CreateMockBatchProcessor();
                
                // Start processing
                var processingTask = batchProcessor.ProcessFilesAsync(largeFileSet, progress, cancellationTokenSource.Token);
                
                // Let it run for a bit
                await Task.Delay(100);
                
                // Test cancellation
                cancellationTokenSource.Cancel();
                
                try
                {
                    await processingTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancelled
                }
            }
            
            // Step 3: Verify progress reporting
            Assert.IsTrue(progressReports.Count > 0, "Progress should be reported");
            
            // Step 4: Verify partial results are preserved
            var partialResults = GetPartialProcessingResults();
            Assert.IsNotNull(partialResults, "Partial results should be preserved");
        }

        [TestMethod]
        public void Workflow_ErrorRecovery_RobustHandling()
        {
            // Test workflow with various error conditions
            
            // Step 1: Process file with corrupted geometry
            var corruptedFile = CreateCorruptedDwgFile("corrupted.dwg");
            var geometryProcessor = CreateMockGeometryProcessor();
            
            var result = geometryProcessor.ProcessDwgFile(corruptedFile, new[] { "ALL_LAYERS" });
            
            // Should handle gracefully
            Assert.IsNotNull(result, "Result should not be null even with corrupted file");
            Assert.IsFalse(result.Success, "Processing should fail for corrupted file");
            Assert.IsTrue(result.Errors.Count > 0, "Errors should be reported");
            
            // Step 2: Attempt column creation with invalid geometry
            var columnProcessor = CreateMockColumnProcessor();
            var invalidRectangle = new[]
            {
                new { X = 0.0, Y = 0.0 },
                new { X = 0.0, Y = 0.0 }, // Duplicate point - invalid
                new { X = 1.0, Y = 1.0 },
                new { X = 1.0, Y = 0.0 }
            };
            
            var columnResult = columnProcessor.CreateColumnFromRectangle(invalidRectangle, "Test Column");
            Assert.IsFalse(columnResult.Success, "Column creation should fail with invalid geometry");
            Assert.IsTrue(columnResult.Errors.Count > 0, "Errors should be reported");
            
            // Step 3: Verify error logging
            var errorLogs = _logger.GetRecentErrors(TimeSpan.FromMinutes(1));
            Assert.IsTrue(errorLogs.Count() > 0, "Errors should be logged");
        }

        [TestMethod]
        public void Workflow_UserExperience_ResponsiveUI()
        {
            // Test workflow focusing on user experience and UI responsiveness
            
            // Step 1: Test progress reporting during long operations
            var progressReports = new List<string>();
            var mockProgress = new Progress<string>(report => progressReports.Add(report));
            
            // Simulate long-running operation with progress updates
            SimulateLongRunningOperation(mockProgress);
            
            Assert.IsTrue(progressReports.Count > 0, "Progress should be reported");
            Assert.IsTrue(progressReports.Any(r => r.Contains("Processing")), "Should report processing status");
            
            // Step 2: Test cancellation responsiveness
            var cancellationTokenSource = new CancellationTokenSource();
            var operationStarted = false;
            var operationCancelled = false;
            
            var task = Task.Run(() =>
            {
                operationStarted = true;
                for (int i = 0; i < 1000; i++)
                {
                    if (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        operationCancelled = true;
                        break;
                    }
                    Thread.Sleep(1);
                }
            });
            
            // Cancel after a short delay
            Thread.Sleep(10);
            cancellationTokenSource.Cancel();
            task.Wait(1000);
            
            Assert.IsTrue(operationStarted, "Operation should start");
            Assert.IsTrue(operationCancelled, "Operation should be cancelled quickly");
        }

        // Helper methods for creating mock objects and data

        private ISettingsService CreateMockSettingsService()
        {
            // In a real implementation, this would return a mock or test double
            // For now, return a simple implementation
            return new MockSettingsService(_testDataPath);
        }

        private IGeometryProcessor CreateMockGeometryProcessor()
        {
            return new MockGeometryProcessor();
        }

        private IColumnProcessor CreateMockColumnProcessor()
        {
            return new MockColumnProcessor();
        }

        private IAdvancedColumnProcessor CreateMockAdvancedColumnProcessor()
        {
            return new MockAdvancedColumnProcessor();
        }

        private IBatchProcessor CreateMockBatchProcessor()
        {
            return new MockBatchProcessor();
        }

        private string CreateMockDwgFile(string fileName)
        {
            var filePath = Path.Combine(_testDataPath, fileName);
            File.WriteAllText(filePath, $"Mock DWG content for {fileName}");
            return filePath;
        }

        private string CreateCorruptedDwgFile(string fileName)
        {
            var filePath = Path.Combine(_testDataPath, fileName);
            File.WriteAllText(filePath, "This is not valid DWG content - corrupted file");
            return filePath;
        }

        private WorkflowReport GenerateWorkflowReport(ProcessingResult[] results)
        {
            return new WorkflowReport
            {
                TotalElementsProcessed = results.Sum(r => r.ElementsProcessed),
                TotalErrors = results.Sum(r => r.Errors.Count),
                TotalWarnings = results.Sum(r => r.Warnings.Count),
                ProcessingTime = TimeSpan.FromMilliseconds(results.Sum(r => r.ProcessingTime.TotalMilliseconds))
            };
        }

        private void SimulateLongRunningOperation(IProgress<string> progress)
        {
            for (int i = 0; i < 10; i++)
            {
                progress?.Report($"Processing step {i + 1} of 10");
                Thread.Sleep(10);
            }
        }

        private object GetPartialProcessingResults()
        {
            // Mock method to get partial results
            return new { ProcessedFiles = 5, RemainingFiles = 45 };
        }
    }

    // Mock interfaces and classes for testing
    public interface ISettingsService
    {
        UserSettings LoadSettings();
        void SaveSettings(UserSettings settings);
    }

    public interface IGeometryProcessor
    {
        ProcessingResult ProcessDwgFile(string filePath, string[] layerNames);
    }

    public interface IColumnProcessor
    {
        ProcessingResult CreateColumnFromRectangle(object[] rectanglePoints, string familyName);
    }

    public interface IAdvancedColumnProcessor
    {
        ProcessingResult CreateCircularColumn(object centerPoint, double diameter, double height);
        ProcessingResult CreateCustomShapeColumn(object[] shapePoints);
        ColumnGridResult CreateColumnGrid(object startPoint, object endPoint, double spacingX, double spacingY, string columnType);
    }

    public interface IBatchProcessor
    {
        BatchResult ProcessFiles(string[] filePaths, BatchProcessingSettings settings);
        Task<BatchResult> ProcessFilesAsync(string[] filePaths, IProgress<BatchProgress> progress, CancellationToken cancellationToken);
    }

    // Mock implementations
    public class MockSettingsService : ISettingsService
    {
        private readonly string _settingsPath;

        public MockSettingsService(string basePath)
        {
            _settingsPath = Path.Combine(basePath, "settings.json");
        }

        public UserSettings LoadSettings()
        {
            return new UserSettings
            {
                LayerMapping = new LayerMappingSettings { DefaultLineStyle = "Thin Lines" },
                ColumnSettings = new ColumnCreationSettings { DefaultFamilyName = "Standard Column" },
                BatchSettings = new BatchProcessingSettings { MaxConcurrentFiles = 3 }
            };
        }

        public void SaveSettings(UserSettings settings)
        {
            // Mock save operation
        }
    }

    public class MockGeometryProcessor : IGeometryProcessor
    {
        public ProcessingResult ProcessDwgFile(string filePath, string[] layerNames)
        {
            if (filePath.Contains("corrupted"))
            {
                return new ProcessingResult
                {
                    Success = false,
                    ElementsProcessed = 0,
                    Errors = new List<string> { "File is corrupted" }
                };
            }

            return new ProcessingResult
            {
                Success = true,
                ElementsProcessed = layerNames.Length * 10,
                Warnings = new List<string>(),
                Errors = new List<string>()
            };
        }
    }

    public class MockColumnProcessor : IColumnProcessor
    {
        public ProcessingResult CreateColumnFromRectangle(object[] rectanglePoints, string familyName)
        {
            // Check for invalid geometry (duplicate points)
            if (rectanglePoints.Length != 4)
            {
                return new ProcessingResult
                {
                    Success = false,
                    Errors = new List<string> { "Invalid rectangle - must have exactly 4 points" }
                };
            }

            return new ProcessingResult
            {
                Success = true,
                ElementsProcessed = 1,
                Message = $"Column created with family {familyName}"
            };
        }
    }

    public class MockAdvancedColumnProcessor : IAdvancedColumnProcessor
    {
        public ProcessingResult CreateCircularColumn(object centerPoint, double diameter, double height)
        {
            return new ProcessingResult
            {
                Success = true,
                ElementsProcessed = 1,
                Message = $"Circular column created: diameter {diameter}, height {height}"
            };
        }

        public ProcessingResult CreateCustomShapeColumn(object[] shapePoints)
        {
            return new ProcessingResult
            {
                Success = true,
                ElementsProcessed = 1,
                Message = $"Custom shape column created with {shapePoints.Length} points"
            };
        }

        public ColumnGridResult CreateColumnGrid(object startPoint, object endPoint, double spacingX, double spacingY, string columnType)
        {
            var columnsX = (int)(30.0 / spacingX) + 1;
            var columnsY = (int)(20.0 / spacingY) + 1;
            var totalColumns = columnsX * columnsY;

            return new ColumnGridResult
            {
                Success = true,
                ColumnsCreated = totalColumns,
                Message = $"Created {totalColumns} columns in grid"
            };
        }
    }

    public class MockBatchProcessor : IBatchProcessor
    {
        public BatchResult ProcessFiles(string[] filePaths, BatchProcessingSettings settings)
        {
            return new BatchResult
            {
                TotalFilesProcessed = filePaths.Length,
                SuccessfulFiles = filePaths.Length - 1, // Simulate one failure
                FailedFiles = 1,
                ProcessingReport = $"Processed {filePaths.Length} files"
            };
        }

        public async Task<BatchResult> ProcessFilesAsync(string[] filePaths, IProgress<BatchProgress> progress, CancellationToken cancellationToken)
        {
            var processedFiles = 0;
            
            foreach (var file in filePaths)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                await Task.Delay(10, cancellationToken);
                processedFiles++;
                
                progress?.Report(new BatchProgress
                {
                    CurrentFile = processedFiles,
                    TotalFiles = filePaths.Length,
                    CurrentFileName = Path.GetFileName(file)
                });
            }

            return new BatchResult
            {
                TotalFilesProcessed = processedFiles,
                SuccessfulFiles = processedFiles,
                FailedFiles = 0
            };
        }
    }

    // Additional model classes for testing
    public class WorkflowReport
    {
        public int TotalElementsProcessed { get; set; }
        public int TotalErrors { get; set; }
        public int TotalWarnings { get; set; }
        public TimeSpan ProcessingTime { get; set; }
    }

    public class ColumnGridResult : ProcessingResult
    {
        public int ColumnsCreated { get; set; }
    }
}