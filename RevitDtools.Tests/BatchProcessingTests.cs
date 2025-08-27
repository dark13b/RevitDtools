using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RevitDtools.Core.Interfaces;
using RevitDtools.Core.Models;
using RevitDtools.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RevitDtools.Tests
{
    [TestClass]
    public class BatchProcessingTests
    {
        private Mock<ILogger> _mockLogger;
        private Mock<IGeometryProcessor> _mockGeometryProcessor;
        private BatchProcessingService _batchProcessor;
        private string _testDirectory;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger>();
            _mockGeometryProcessor = new Mock<IGeometryProcessor>();
            
            // Create a mock document (in real implementation, this would be a Revit Document)
            var mockDocument = new Mock<Autodesk.Revit.DB.Document>();
            
            _batchProcessor = new BatchProcessingService(mockDocument.Object, _mockLogger.Object, _mockGeometryProcessor.Object);
            
            // Create test directory with sample files
            _testDirectory = Path.Combine(Path.GetTempPath(), "RevitDtoolsTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _batchProcessor?.Dispose();
            
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [TestMethod]
        public async Task ProcessMultipleFiles_WithValidFiles_ReturnsSuccessfulResult()
        {
            // Arrange
            var testFiles = CreateTestFiles(3);
            var progress = new Mock<IProgress<BatchProgress>>();
            
            _mockGeometryProcessor
                .Setup(x => x.ProcessElement(It.IsAny<Autodesk.Revit.DB.Element>()))
                .Returns(new ProcessingResult 
                { 
                    Success = true, 
                    ElementsProcessed = 5,
                    ElementsSkipped = 1,
                    Message = "Success"
                });

            // Act
            var result = await _batchProcessor.ProcessMultipleFiles(testFiles, progress.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(testFiles.Count, result.TotalFilesProcessed);
            Assert.IsFalse(result.WasCancelled);
            Assert.IsTrue(result.ProcessingEndTime > result.ProcessingStartTime);
            
            // Verify progress was reported
            progress.Verify(x => x.Report(It.IsAny<BatchProgress>()), Times.AtLeast(testFiles.Count));
            
            // Verify logging
            _mockLogger.Verify(x => x.LogInfo(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task ProcessMultipleFiles_WithEmptyList_ThrowsArgumentException()
        {
            // Arrange
            var emptyList = new List<string>();
            var progress = new Mock<IProgress<BatchProgress>>();

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _batchProcessor.ProcessMultipleFiles(emptyList, progress.Object));
        }

        [TestMethod]
        public async Task ProcessMultipleFiles_WithNullList_ThrowsArgumentException()
        {
            // Arrange
            List<string> nullList = null;
            var progress = new Mock<IProgress<BatchProgress>>();

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _batchProcessor.ProcessMultipleFiles(nullList, progress.Object));
        }

        [TestMethod]
        public async Task ProcessMultipleFiles_WithCancellation_ReturnsCancelledResult()
        {
            // Arrange
            var testFiles = CreateTestFiles(5);
            var progress = new Mock<IProgress<BatchProgress>>();
            var cancellationTokenSource = new CancellationTokenSource();
            
            // Cancel after a short delay
            cancellationTokenSource.CancelAfter(100);

            _mockGeometryProcessor
                .Setup(x => x.ProcessElement(It.IsAny<Autodesk.Revit.DB.Element>()))
                .Returns(() =>
                {
                    Thread.Sleep(200); // Simulate processing time
                    return new ProcessingResult { Success = true, ElementsProcessed = 1 };
                });

            // Act
            var result = await _batchProcessor.ProcessMultipleFiles(testFiles, progress.Object, cancellationTokenSource.Token);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.WasCancelled);
            Assert.IsTrue(result.TotalFilesProcessed < testFiles.Count);
        }

        [TestMethod]
        public async Task ProcessFolder_WithValidFolder_ReturnsSuccessfulResult()
        {
            // Arrange
            CreateTestFiles(3); // Files are created in _testDirectory
            var progress = new Mock<IProgress<BatchProgress>>();
            
            _mockGeometryProcessor
                .Setup(x => x.ProcessElement(It.IsAny<Autodesk.Revit.DB.Element>()))
                .Returns(new ProcessingResult { Success = true, ElementsProcessed = 2 });

            // Act
            var result = await _batchProcessor.ProcessFolder(_testDirectory, false, progress.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.TotalFilesProcessed);
            Assert.IsFalse(result.WasCancelled);
        }

        [TestMethod]
        public async Task ProcessFolder_WithSubfolders_ProcessesAllFiles()
        {
            // Arrange
            CreateTestFiles(2); // 2 files in root
            
            var subDirectory = Path.Combine(_testDirectory, "subfolder");
            Directory.CreateDirectory(subDirectory);
            CreateTestFiles(3, subDirectory); // 3 files in subfolder
            
            var progress = new Mock<IProgress<BatchProgress>>();
            
            _mockGeometryProcessor
                .Setup(x => x.ProcessElement(It.IsAny<Autodesk.Revit.DB.Element>()))
                .Returns(new ProcessingResult { Success = true, ElementsProcessed = 1 });

            // Act
            var result = await _batchProcessor.ProcessFolder(_testDirectory, true, progress.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(5, result.TotalFilesProcessed); // 2 + 3 files
        }

        [TestMethod]
        public async Task ProcessFolder_WithoutSubfolders_ProcessesOnlyRootFiles()
        {
            // Arrange
            CreateTestFiles(2); // 2 files in root
            
            var subDirectory = Path.Combine(_testDirectory, "subfolder");
            Directory.CreateDirectory(subDirectory);
            CreateTestFiles(3, subDirectory); // 3 files in subfolder (should be ignored)
            
            var progress = new Mock<IProgress<BatchProgress>>();
            
            _mockGeometryProcessor
                .Setup(x => x.ProcessElement(It.IsAny<Autodesk.Revit.DB.Element>()))
                .Returns(new ProcessingResult { Success = true, ElementsProcessed = 1 });

            // Act
            var result = await _batchProcessor.ProcessFolder(_testDirectory, false, progress.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.TotalFilesProcessed); // Only root files
        }

        [TestMethod]
        public async Task ProcessFolder_WithNonExistentFolder_ThrowsDirectoryNotFoundException()
        {
            // Arrange
            var nonExistentFolder = Path.Combine(_testDirectory, "nonexistent");
            var progress = new Mock<IProgress<BatchProgress>>();

            // Act & Assert
            await Assert.ThrowsExceptionAsync<DirectoryNotFoundException>(
                () => _batchProcessor.ProcessFolder(nonExistentFolder, false, progress.Object));
        }

        [TestMethod]
        public async Task ProcessFolder_WithEmptyFolder_ReturnsEmptyResult()
        {
            // Arrange
            var emptyFolder = Path.Combine(_testDirectory, "empty");
            Directory.CreateDirectory(emptyFolder);
            var progress = new Mock<IProgress<BatchProgress>>();

            // Act
            var result = await _batchProcessor.ProcessFolder(emptyFolder, false, progress.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.TotalFilesProcessed);
            Assert.AreEqual(0, result.SuccessfulFiles);
            Assert.AreEqual(0, result.FailedFiles);
            Assert.IsTrue(result.Summary.Contains("No DWG files found"));
        }

        [TestMethod]
        public void GetProcessingStatus_InitialState_ReturnsNotStarted()
        {
            // Act
            var status = _batchProcessor.GetProcessingStatus();

            // Assert
            Assert.AreEqual(BatchProcessingStatus.NotStarted, status);
        }

        [TestMethod]
        public void CancelProcessing_WhenNotProcessing_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            _batchProcessor.CancelProcessing();
            
            // Verify logging
            _mockLogger.Verify(x => x.LogInfo(It.Is<string>(s => s.Contains("cancellation"))), Times.Once);
        }

        [TestMethod]
        public async Task ProcessMultipleFiles_WithMixedResults_ReturnsCorrectSummary()
        {
            // Arrange
            var testFiles = CreateTestFiles(4);
            var progress = new Mock<IProgress<BatchProgress>>();
            var callCount = 0;
            
            _mockGeometryProcessor
                .Setup(x => x.ProcessElement(It.IsAny<Autodesk.Revit.DB.Element>()))
                .Returns(() =>
                {
                    callCount++;
                    // First two succeed, last two fail
                    return new ProcessingResult 
                    { 
                        Success = callCount <= 2,
                        ElementsProcessed = callCount <= 2 ? 3 : 0,
                        ElementsSkipped = 1,
                        Message = callCount <= 2 ? "Success" : "Failed",
                        Errors = callCount <= 2 ? new List<string>() : new List<string> { "Test error" }
                    };
                });

            // Act
            var result = await _batchProcessor.ProcessMultipleFiles(testFiles, progress.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.TotalFilesProcessed);
            Assert.AreEqual(2, result.SuccessfulFiles);
            Assert.AreEqual(2, result.FailedFiles);
            Assert.IsTrue(result.Summary.Contains("2/4 files successful"));
        }

        [TestMethod]
        public async Task ProcessMultipleFiles_WithProgressReporting_ReportsCorrectProgress()
        {
            // Arrange
            var testFiles = CreateTestFiles(3);
            var progressReports = new List<BatchProgress>();
            var progress = new Progress<BatchProgress>(p => progressReports.Add(p));
            
            _mockGeometryProcessor
                .Setup(x => x.ProcessElement(It.IsAny<Autodesk.Revit.DB.Element>()))
                .Returns(new ProcessingResult { Success = true, ElementsProcessed = 1 });

            // Act
            await _batchProcessor.ProcessMultipleFiles(testFiles, progress);

            // Assert
            Assert.IsTrue(progressReports.Count >= testFiles.Count);
            
            var lastProgress = progressReports.Last();
            Assert.AreEqual(testFiles.Count, lastProgress.TotalFiles);
            Assert.AreEqual(testFiles.Count, lastProgress.CurrentFile);
            Assert.AreEqual(100.0, lastProgress.PercentComplete, 0.1);
        }

        /// <summary>
        /// Create test DWG files for testing
        /// </summary>
        private List<string> CreateTestFiles(int count, string directory = null)
        {
            var targetDirectory = directory ?? _testDirectory;
            var files = new List<string>();

            for (int i = 1; i <= count; i++)
            {
                var fileName = Path.Combine(targetDirectory, $"test{i}.dwg");
                
                // Create a dummy file (not a real DWG, but sufficient for testing)
                File.WriteAllText(fileName, $"Test DWG file {i}");
                files.Add(fileName);
            }

            return files;
        }
    }
}