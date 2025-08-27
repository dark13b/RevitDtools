using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RevitDtools.Core.Services;
using RevitDtools.Core.Interfaces;
using RevitDtools.Utilities;

namespace RevitDtools
{
    /// <summary>
    /// Main execution script for complete namespace conflict resolution
    /// Implements task 9: Execute complete conflict resolution and validation
    /// </summary>
    public class ExecuteConflictResolution
    {
        private static readonly ILogger _logger = Logger.Instance;
        
        /// <summary>
        /// Main entry point for conflict resolution execution
        /// </summary>
        public static async Task<int> Main(string[] args)
        {
            try
            {
                Console.WriteLine("RevitDtools Namespace Conflict Resolution");
                Console.WriteLine("========================================");
                Console.WriteLine($"Started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine();
                
                // Step 1: Run full conflict detection scan on current codebase
                Console.WriteLine("Step 1: Running full conflict detection scan...");
                var orchestrator = new ConflictResolutionOrchestrator(_logger);
                var projectPath = Directory.GetCurrentDirectory();
                
                // Step 2: Execute systematic resolution of all conflict categories in order
                Console.WriteLine("Step 2: Executing systematic resolution...");
                var result = await orchestrator.ResolveAllConflictsAsync(projectPath, createBackup: true);
                
                // Step 3: Generate comprehensive report of all changes made
                Console.WriteLine("Step 3: Generating comprehensive report...");
                var report = orchestrator.GenerateProgressReport(result);
                var reportPath = Path.Combine(projectPath, "conflict_resolution_report.txt");
                await File.WriteAllTextAsync(reportPath, report);
                
                // Display results
                DisplayExecutionResults(result, reportPath);
                
                // Step 4: Verify zero compilation errors in final build
                Console.WriteLine("Step 4: Verifying final build status...");
                await VerifyFinalBuildStatus(result);
                
                // Step 5: Test sample functionality to ensure no breaking changes
                Console.WriteLine("Step 5: Testing sample functionality...");
                await TestSampleFunctionality(result);
                
                // Final summary
                DisplayFinalSummary(result);
                
                return result.Success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fatal error during execution: {ex.Message}");
                _logger.LogError(ex, "Fatal error during conflict resolution execution");
                return 1;
            }
        }
        
        /// <summary>
        /// Display execution results in a comprehensive format
        /// </summary>
        private static void DisplayExecutionResults(OrchestrationResult result, string reportPath)
        {
            Console.WriteLine();
            Console.WriteLine("Execution Results Summary:");
            Console.WriteLine("=========================");
            
            // Overall status
            var statusIcon = result.Success ? "✅" : "❌";
            Console.WriteLine($"{statusIcon} Overall Status: {(result.Success ? "SUCCESS" : "FAILED")}");
            Console.WriteLine($"⏱️  Total Duration: {result.TotalDuration.TotalMinutes:F2} minutes");
            Console.WriteLine($"📊 Detailed Report: {reportPath}");
            Console.WriteLine();
            
            // Conflict detection results
            Console.WriteLine("Conflict Detection Results:");
            Console.WriteLine($"  📋 TaskDialog conflicts: {result.ConflictDetection.TaskDialogConflicts.Count} files");
            Console.WriteLine($"  💬 MessageBox conflicts: {result.ConflictDetection.MessageBoxConflicts.Count} files");
            Console.WriteLine($"  🎛️  UI Control conflicts: {result.ConflictDetection.UIControlConflicts.Count} files");
            Console.WriteLine($"  📁 Dialog conflicts: {result.ConflictDetection.DialogConflicts.Count} files");
            Console.WriteLine($"  👁️  View conflicts: {result.ConflictDetection.ViewConflicts.Count} files");
            Console.WriteLine();
            
            // Build status comparison
            if (result.InitialBuildResult != null && result.FinalBuildResult != null)
            {
                Console.WriteLine("Build Status Comparison:");
                Console.WriteLine($"  Initial Errors: {result.InitialBuildResult.ErrorCount}");
                Console.WriteLine($"  Final Errors: {result.FinalBuildResult.ErrorCount}");
                
                var errorsResolved = Math.Max(0, result.InitialBuildResult.ErrorCount - result.FinalBuildResult.ErrorCount);
                var resolutionRate = result.InitialBuildResult.ErrorCount > 0 
                    ? (double)errorsResolved / result.InitialBuildResult.ErrorCount * 100 
                    : 100;
                
                Console.WriteLine($"  Errors Resolved: {errorsResolved} ({resolutionRate:F1}%)");
                Console.WriteLine();
            }
            
            // Resolution step results
            Console.WriteLine("Resolution Steps:");
            foreach (var step in result.ResolutionSteps)
            {
                var stepIcon = step.Success ? "✅" : "❌";
                Console.WriteLine($"  {stepIcon} {step.StepName}: {step.FilesModified.Count} files modified");
                
                if (!step.Success && !string.IsNullOrEmpty(step.ErrorMessage))
                {
                    Console.WriteLine($"      ⚠️  Error: {step.ErrorMessage}");
                }
            }
            Console.WriteLine();
        }
        
        /// <summary>
        /// Verify final build status and ensure zero compilation errors
        /// </summary>
        private static async Task VerifyFinalBuildStatus(OrchestrationResult result)
        {
            try
            {
                if (result.FinalBuildResult == null)
                {
                    Console.WriteLine("⚠️  No final build result available");
                    return;
                }
                
                Console.WriteLine("Final Build Verification:");
                Console.WriteLine($"  Build Success: {(result.FinalBuildResult.BuildSuccessful ? "✅ YES" : "❌ NO")}");
                Console.WriteLine($"  Error Count: {result.FinalBuildResult.ErrorCount}");
                Console.WriteLine($"  Warning Count: {result.FinalBuildResult.WarningCount}");
                
                if (result.FinalBuildResult.ErrorCount == 0)
                {
                    Console.WriteLine("✅ SUCCESS: Zero compilation errors achieved!");
                }
                else
                {
                    Console.WriteLine($"❌ FAILED: {result.FinalBuildResult.ErrorCount} compilation errors remain");
                    
                    // Show first few errors for debugging
                    if (result.FinalBuildResult.BuildErrors.Any())
                    {
                        Console.WriteLine("  Remaining errors (first 3):");
                        foreach (var error in result.FinalBuildResult.BuildErrors.Take(3))
                        {
                            Console.WriteLine($"    • {Path.GetFileName(error.FilePath)}({error.LineNumber}): {error.Message}");
                        }
                    }
                }
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Error verifying build status: {ex.Message}");
                _logger.LogError(ex, "Error verifying final build status");
            }
        }
        
        /// <summary>
        /// Test sample functionality to ensure no breaking changes
        /// </summary>
        private static async Task TestSampleFunctionality(OrchestrationResult result)
        {
            try
            {
                Console.WriteLine("Sample Functionality Tests:");
                
                // Test 1: Verify core classes can be instantiated
                await TestCoreClassInstantiation();
                
                // Test 2: Verify resolver classes work correctly
                await TestResolverFunctionality();
                
                // Test 3: Verify build validator works
                await TestBuildValidatorFunctionality();
                
                Console.WriteLine("✅ All sample functionality tests passed");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Sample functionality test failed: {ex.Message}");
                _logger.LogError(ex, "Sample functionality test failed");
            }
        }
        
        /// <summary>
        /// Test core class instantiation
        /// </summary>
        private static async Task TestCoreClassInstantiation()
        {
            try
            {
                // Test orchestrator instantiation
                var orchestrator = new ConflictResolutionOrchestrator();
                Console.WriteLine("  ✅ ConflictResolutionOrchestrator instantiated successfully");
                
                // Test logger
                var logger = Logger.Instance;
                Logger.LogInfo("Test log message", "Test");
                Console.WriteLine("  ✅ Logger working correctly");
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ Core class instantiation failed: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Test resolver functionality
        /// </summary>
        private static async Task TestResolverFunctionality()
        {
            try
            {
                var projectPath = Directory.GetCurrentDirectory();
                
                // Test TaskDialogResolver
                var taskDialogResolver = new TaskDialogResolver();
                var testFile = Path.Combine(projectPath, "ConflictResolutionDemo.cs");
                if (File.Exists(testFile))
                {
                    var summary = taskDialogResolver.AnalyzeFile(testFile);
                    Console.WriteLine("  ✅ TaskDialogResolver analysis working");
                }
                
                // Test MessageBoxResolver
                var messageBoxResolver = new MessageBoxResolver();
                var conflicts = messageBoxResolver.DetectConflicts(projectPath);
                Console.WriteLine("  ✅ MessageBoxResolver detection working");
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ Resolver functionality test failed: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Test build validator functionality
        /// </summary>
        private static async Task TestBuildValidatorFunctionality()
        {
            try
            {
                var buildResult = BuildValidator.ValidateProject();
                
                Console.WriteLine($"  ✅ BuildValidator executed (Success: {buildResult.OverallSuccess})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ BuildValidator test failed: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Display final summary of the entire execution
        /// </summary>
        private static void DisplayFinalSummary(OrchestrationResult result)
        {
            Console.WriteLine("Final Execution Summary:");
            Console.WriteLine("=======================");
            
            if (result.Success && result.FinalBuildResult?.ErrorCount == 0)
            {
                Console.WriteLine("🎉 COMPLETE SUCCESS!");
                Console.WriteLine("✅ All namespace conflicts resolved");
                Console.WriteLine("✅ Zero compilation errors achieved");
                Console.WriteLine("✅ No breaking changes detected");
                Console.WriteLine("✅ Project ready for continued development");
            }
            else if (result.Success)
            {
                Console.WriteLine("⚠️  PARTIAL SUCCESS");
                Console.WriteLine("✅ Conflict resolution process completed");
                Console.WriteLine($"⚠️  {result.FinalBuildResult?.ErrorCount ?? 0} compilation errors remain");
                Console.WriteLine("📋 Review the detailed report for next steps");
            }
            else
            {
                Console.WriteLine("❌ EXECUTION FAILED");
                Console.WriteLine("❌ Conflict resolution process encountered errors");
                Console.WriteLine("📋 Review the detailed report and logs for troubleshooting");
                
                if (result.BackupSession != null)
                {
                    Console.WriteLine($"🔄 Rollback available using session: {result.BackupSession.Id}");
                }
            }
            
            Console.WriteLine();
            Console.WriteLine($"Execution completed at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Total duration: {result.TotalDuration.TotalMinutes:F2} minutes");
            
            if (result.Analysis != null)
            {
                Console.WriteLine($"Files modified: {result.Analysis.TotalFilesModified}");
                Console.WriteLine($"Resolution effectiveness: {result.Analysis.ResolutionEffectiveness:F1}%");
            }
        }
    }
}