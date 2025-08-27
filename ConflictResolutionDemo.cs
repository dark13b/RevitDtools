using System;
using System.Threading.Tasks;
using RevitDtools.Core.Services;
using RevitDtools.Utilities;

namespace RevitDtools
{
    /// <summary>
    /// Demonstration of the ConflictResolutionOrchestrator
    /// This class shows how to use the orchestrator to resolve namespace conflicts
    /// </summary>
    public class ConflictResolutionDemo
    {
        /// <summary>
        /// Demonstrates the complete conflict resolution process
        /// </summary>
        public static async Task<int> Main(string[] args)
        {
            try
            {
                Console.WriteLine("RevitDtools Namespace Conflict Resolution");
                Console.WriteLine("========================================");
                Console.WriteLine();
                
                // Initialize the orchestrator
                var logger = Logger.Instance;
                var orchestrator = new ConflictResolutionOrchestrator(logger);
                
                // Get project path from args or use current directory
                var projectPath = args.Length > 0 ? args[0] : Environment.CurrentDirectory;
                
                Console.WriteLine($"Project Path: {projectPath}");
                Console.WriteLine("Starting conflict resolution process...");
                Console.WriteLine();
                
                // Execute the complete resolution process
                var result = await orchestrator.ResolveAllConflictsAsync(projectPath, createBackup: true);
                
                // Display results
                DisplayResults(result);
                
                // Generate and save detailed report
                var report = orchestrator.GenerateProgressReport(result);
                var reportPath = "conflict_resolution_report.txt";
                await System.IO.File.WriteAllTextAsync(reportPath, report);
                
                Console.WriteLine($"Detailed report saved to: {reportPath}");
                Console.WriteLine();
                
                // Offer rollback option if there were issues
                if (!result.Success && result.BackupSession != null)
                {
                    Console.WriteLine("Resolution was not fully successful.");
                    Console.Write("Would you like to rollback changes? (y/N): ");
                    var response = Console.ReadLine();
                    
                    if (response?.ToLower().StartsWith("y") == true)
                    {
                        Console.WriteLine("Rolling back changes...");
                        var rollbackResult = await orchestrator.RollbackChangesAsync(result.BackupSession.Id);
                        
                        if (rollbackResult.Success)
                        {
                            Console.WriteLine("✅ Rollback completed successfully");
                        }
                        else
                        {
                            Console.WriteLine($"❌ Rollback failed: {rollbackResult.ErrorMessage}");
                        }
                    }
                }
                
                return result.Success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fatal error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return 1;
            }
        }
        
        /// <summary>
        /// Displays the orchestration results in a user-friendly format
        /// </summary>
        private static void DisplayResults(OrchestrationResult result)
        {
            Console.WriteLine("Resolution Results:");
            Console.WriteLine("==================");
            
            // Overall status
            var statusIcon = result.Success ? "✅" : "❌";
            Console.WriteLine($"{statusIcon} Overall Status: {(result.Success ? "SUCCESS" : "FAILED")}");
            Console.WriteLine($"⏱️  Total Duration: {result.TotalDuration.TotalMinutes:F2} minutes");
            Console.WriteLine();
            
            // Initial vs Final state
            if (result.InitialBuildResult != null && result.FinalBuildResult != null)
            {
                Console.WriteLine("Build Status Comparison:");
                Console.WriteLine($"  Initial Errors: {result.InitialBuildResult.ErrorCount}");
                Console.WriteLine($"  Final Errors: {result.FinalBuildResult.ErrorCount}");
                
                var errorsResolved = Math.Max(0, result.InitialBuildResult.ErrorCount - result.FinalBuildResult.ErrorCount);
                Console.WriteLine($"  Errors Resolved: {errorsResolved}");
                Console.WriteLine();
            }
            
            // Conflict detection summary
            Console.WriteLine("Conflicts Detected:");
            Console.WriteLine($"  📋 TaskDialog: {result.ConflictDetection.TaskDialogConflicts.Count} files");
            Console.WriteLine($"  💬 MessageBox: {result.ConflictDetection.MessageBoxConflicts.Count} files");
            Console.WriteLine($"  🎛️  UI Controls: {result.ConflictDetection.UIControlConflicts.Count} files");
            Console.WriteLine($"  📁 Dialogs: {result.ConflictDetection.DialogConflicts.Count} files");
            Console.WriteLine($"  👁️  Views: {result.ConflictDetection.ViewConflicts.Count} files");
            Console.WriteLine();
            
            // Resolution steps
            Console.WriteLine("Resolution Steps:");
            foreach (var step in result.ResolutionSteps)
            {
                var stepIcon = step.Success ? "✅" : "❌";
                Console.WriteLine($"  {stepIcon} {step.StepName}: {step.FilesModified.Count} files modified ({step.Duration.TotalSeconds:F1}s)");
                
                if (!step.Success)
                {
                    Console.WriteLine($"      ⚠️  Error: {step.ErrorMessage}");
                }
            }
            Console.WriteLine();
            
            // Analysis and recommendations
            if (result.Analysis?.Recommendations.Any() == true)
            {
                Console.WriteLine("Recommendations:");
                foreach (var recommendation in result.Analysis.Recommendations)
                {
                    Console.WriteLine($"  {recommendation}");
                }
                Console.WriteLine();
            }
            
            // Backup information
            if (result.BackupSession != null)
            {
                Console.WriteLine("Backup Information:");
                Console.WriteLine($"  📦 Session: {result.BackupSession.Name}");
                Console.WriteLine($"  🆔 ID: {result.BackupSession.Id}");
                Console.WriteLine($"  📁 Files: {result.BackupSession.BackedUpFiles.Count} backed up");
                Console.WriteLine();
            }
            
            // Error details if build failed
            if (result.FinalBuildResult?.BuildSuccessful == false && result.FinalBuildResult.BuildErrors.Any())
            {
                Console.WriteLine("Remaining Build Errors (first 5):");
                foreach (var error in result.FinalBuildResult.BuildErrors.Take(5))
                {
                    Console.WriteLine($"  ❌ {System.IO.Path.GetFileName(error.FilePath)}({error.LineNumber}): {error.Message}");
                }
                
                if (result.FinalBuildResult.BuildErrors.Count > 5)
                {
                    Console.WriteLine($"  ... and {result.FinalBuildResult.BuildErrors.Count - 5} more errors");
                }
                Console.WriteLine();
            }
        }
    }
}