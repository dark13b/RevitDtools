using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RevitDtools.Core.Interfaces;
using RevitDtools.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RevitDtools.Tests
{
    [TestClass]
    public class SimpleBatchProcessingTests
    {
        [TestMethod]
        public void BatchProgress_CalculatesPercentCorrectly()
        {
            // Arrange
            var progress = new BatchProgress
            {
                CurrentFile = 3,
                TotalFiles = 10
            };

            // Act
            var percent = progress.PercentComplete;

            // Assert
            Assert.AreEqual(30.0, percent, 0.1);
        }

        [TestMethod]
        public void BatchResult_CalculatesSummaryCorrectly()
        {
            // Arrange
            var result = new BatchResult
            {
                ProcessingStartTime = DateTime.Now.AddMinutes(-5),
                ProcessingEndTime = DateTime.Now
            };

            result.FileResults.Add(new FileProcessingResult 
            { 
                Success = true, 
                ElementsProcessed = 10, 
                ElementsSkipped = 2,
                Errors = new List<string>()
            });

            result.FileResults.Add(new FileProcessingResult 
            { 
                Success = false, 
                ElementsProcessed = 5, 
                ElementsSkipped = 1,
                Errors = new List<string> { "Test error" }
            });

            // Act
            result.CalculateSummary();

            // Assert
            Assert.AreEqual(2, result.TotalFilesProcessed);
            Assert.AreEqual(1, result.SuccessfulFiles);
            Assert.AreEqual(1, result.FailedFiles);
            Assert.IsTrue(result.Summary.Contains("1/2 files successful"));
            Assert.IsTrue(result.Summary.Contains("15 elements processed"));
            Assert.IsTrue(result.Summary.Contains("3 skipped"));
            Assert.IsTrue(result.Summary.Contains("1 errors"));
        }

        [TestMethod]
        public void FileProcessingResult_InitializesCorrectly()
        {
            // Arrange & Act
            var result = new FileProcessingResult
            {
                FilePath = @"C:\test\file.dwg",
                FileName = "file.dwg",
                Success = true,
                ElementsProcessed = 5,
                ElementsSkipped = 2
            };

            // Assert
            Assert.AreEqual(@"C:\test\file.dwg", result.FilePath);
            Assert.AreEqual("file.dwg", result.FileName);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(5, result.ElementsProcessed);
            Assert.AreEqual(2, result.ElementsSkipped);
            Assert.IsNotNull(result.Warnings);
            Assert.IsNotNull(result.Errors);
        }

        [TestMethod]
        public void BatchProcessingStatus_EnumValuesExist()
        {
            // Test that all expected enum values exist
            Assert.IsTrue(Enum.IsDefined(typeof(BatchProcessingStatus), BatchProcessingStatus.NotStarted));
            Assert.IsTrue(Enum.IsDefined(typeof(BatchProcessingStatus), BatchProcessingStatus.Running));
            Assert.IsTrue(Enum.IsDefined(typeof(BatchProcessingStatus), BatchProcessingStatus.Paused));
            Assert.IsTrue(Enum.IsDefined(typeof(BatchProcessingStatus), BatchProcessingStatus.Completed));
            Assert.IsTrue(Enum.IsDefined(typeof(BatchProcessingStatus), BatchProcessingStatus.Cancelled));
            Assert.IsTrue(Enum.IsDefined(typeof(BatchProcessingStatus), BatchProcessingStatus.Failed));
        }

        [TestMethod]
        public void ProcessingResult_CreateSuccessMethod_WorksCorrectly()
        {
            // Act
            var result = ProcessingResult.CreateSuccess(5, "Test message");

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(5, result.ElementsProcessed);
            Assert.AreEqual("Test message", result.Message);
            Assert.AreEqual(0, result.ElementsSkipped);
            Assert.IsNotNull(result.Warnings);
            Assert.IsNotNull(result.Errors);
        }

        [TestMethod]
        public void ProcessingResult_CreateFailureMethod_WorksCorrectly()
        {
            // Act
            var result = ProcessingResult.CreateFailure("Test error");

            // Assert
            Assert.IsFalse(result.Success);
            Assert.AreEqual("Test error", result.Message);
            Assert.AreEqual(0, result.ElementsProcessed);
            Assert.AreEqual(0, result.ElementsSkipped);
            Assert.IsNotNull(result.Warnings);
            Assert.IsNotNull(result.Errors);
        }
    }
}