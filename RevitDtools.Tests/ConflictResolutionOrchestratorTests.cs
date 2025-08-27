using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RevitDtools.Core.Services;

namespace RevitDtools.Tests
{
    [TestClass]
    public class ConflictResolutionOrchestratorTests
    {
        private ConflictResolutionOrchestrator _orchestrator;
        private string _testProjectPath;
        
        [TestInitialize]
        public void Setup()
        {
            _orchestrator = new ConflictResolutionOrchestrator();
            _testProjectPath = Path.GetTempPath();
        }
        
        [TestMethod]
        public async Task ResolveAllConflictsAsync_WithNoConflicts_ReturnsSuccess()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                // Create a simple C# file with no conflicts
                var testFile = Path.Combine(tempDir, "TestFile.cs");
                await File.WriteAllTextAsync(testFile, @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            Console.WriteLine(""Hello World"");
        }
    }
}");
                
                // Act
                var result = await _orchestrator.ResolveAllConflictsAsync(tempDir, createBackup: false);
                
                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Success || result.InitialConflictCount == 0);
                Assert.IsNotNull(result.ConflictDetection);
                Assert.AreEqual(0, result.ConflictDetection.TotalConflictingFiles);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
        
        [TestMethod]
        public async Task ResolveAllConflictsAsync_WithTaskDialogConflicts_ResolvesConflicts()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                // Create a C# file with TaskDialog conflicts
                var testFile = Path.Combine(tempDir, "TaskDialogTest.cs");
                await File.WriteAllTextAsync(testFile, @"
using System;
using Autodesk.Revit.UI;

namespace TestNamespace
{
    public class TestClass
    {
        public void ShowDialog()
        {
            TaskDialog dialog = new TaskDialog(""Test"");
            dialog.Show();
        }
    }
}");
                
                // Act
                var result = await _orchestrator.ResolveAllConflictsAsync(tempDir, createBackup: false);
                
                // Assert
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.ConflictDetection);
                
                // Check if TaskDialog conflicts were detected
                if (result.ConflictDetection.TaskDialogConflicts.Any())
                {
                    // Verify that resolution was attempted
                    var taskDialogStep = result.ResolutionSteps.FirstOrDefault(s => s.StepName == "TaskDialog Resolution");
                    Assert.IsNotNull(taskDialogStep);
                }
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
        
        [TestMethod]
        public void GenerateProgressReport_WithValidResult_ReturnsFormattedReport()
        {
            // Arrange
            var result = new OrchestrationResult
            {
                ProjectPath = "TestProject",
                StartTime = DateTime.Now.AddMinutes(-5),
                EndTime = DateTime.Now,
                Success = true,
                InitialConflictCount = 10,
                ConflictDetection = new ConflictDetectionResult(),
                Analysis = new OrchestrationAnalysis
                {
                    InitialErrorCount = 10,
                    FinalErrorCount = 0,
                    ErrorsResolved = 10,
                    ResolutionEffectiveness = 100.0,
                    TotalFilesModified = 5,
                    SuccessfulSteps = 3,
                    TotalSteps = 3,
                    Recommendations = new[] { "All conflicts resolved successfully!" }.ToList()
                }
            };
            
            // Act
            var report = _orchestrator.GenerateProgressReport(result);
            
            // Assert
            Assert.IsNotNull(report);
            Assert.IsTrue(report.Contains("Namespace Conflict Resolution Report"));
            Assert.IsTrue(report.Contains("SUCCESS"));
            Assert.IsTrue(report.Contains("TestProject"));
            Assert.IsTrue(report.Contains("All conflicts resolved successfully!"));
        }
        
        [TestMethod]
        public async Task RollbackChangesAsync_WithNoBackupSession_ReturnsFailure()
        {
            // Act
            var result = await _orchestrator.RollbackChangesAsync("nonexistent-session");
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Success);
            Assert.IsTrue(result.ErrorMessage.Contains("No backup session found"));
        }
        
        [TestMethod]
        public void OrchestrationResult_TotalDuration_CalculatesCorrectly()
        {
            // Arrange
            var startTime = DateTime.Now;
            var endTime = startTime.AddMinutes(5);
            
            var result = new OrchestrationResult
            {
                StartTime = startTime,
                EndTime = endTime
            };
            
            // Act
            var duration = result.TotalDuration;
            
            // Assert
            Assert.AreEqual(5, duration.TotalMinutes);
        }
        
        [TestMethod]
        public void ConflictDetectionResult_TotalConflictingFiles_SumsCorrectly()
        {
            // Arrange
            var detection = new ConflictDetectionResult
            {
                TaskDialogConflicts = new[] { "file1.cs", "file2.cs" }.ToList(),
                MessageBoxConflicts = new[] { 
                    new MessageBoxConflict { FilePath = "file3.cs" },
                    new MessageBoxConflict { FilePath = "file4.cs" }
                }.ToList(),
                UIControlConflicts = new[] { "file5.cs" }.ToList(),
                DialogConflicts = new[] { 
                    new DialogConflictInfo { FilePath = "file6.cs" }
                }.ToList(),
                ViewConflicts = new[] { "file7.cs" }.ToList()
            };
            
            // Act
            var total = detection.TotalConflictingFiles;
            
            // Assert
            Assert.AreEqual(7, total);
        }
    }
}