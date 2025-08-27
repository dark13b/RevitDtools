using System;
using System.IO;
using System.Threading.Tasks;
using RevitDtools.Core.Services;
using RevitDtools.Utilities;

namespace RevitDtools
{
    /// <summary>
    /// Simple execution script for namespace conflict resolution
    /// </summary>
    public class RunConflictResolution
    {
        public static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("RevitDtools Namespace Conflict Resolution");
                Console.WriteLine("========================================");
                Console.WriteLine($"Started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine();
                
                // Initialize orchestrator
                var orchestrator = new ConflictResolutionOrchestrator();
                var projectPath = Directory.GetCurrentDirectory();
                
                Console.WriteLine($"Project Path: {projectPath}");
                Console.WriteLine("Starting conflict resolution process...");
                Console.WriteLine();
                
                // Execute the complete resolution process
                var result = await orchestrator.ResolveAllConflictsAsync(projectPath, createBackup: true);
                
                // Display results
                Console.WriteLine("Resolution Results:");
                Console.WriteLine("==================");
                
                var statusIcon = result.Success ? "✅" : "❌";
                Console.WriteLine($"{statusIcon} Overall Status: {(result.Success ? "SUCCESS" : "FAILED")}");
                Console.WriteLine($"⏱️  Total Duration: {result.TotalDuration.TotalMinutes:F2} minutes");
                Console.WriteLine();
                
                // Conflict detection results
                Console.WriteLine("Conflicts Detected:");
                Console.WriteLine($"  📋 TaskDialog: {result.ConflictDetection.TaskDialogConflicts.Count} files");
                Console.WriteLine($"  💬 MessageBox: {result.ConflictDetection.MessageBoxConflicts.Count} files");
                Console.WriteLine($"  🎛️  UI Controls: {result.ConflictDetection.UIControlConflicts.Count} files");
                Console.WriteLine($"  📁 Dialogs: {result.ConflictDetection.DialogConflicts.Count} files");
                Console.WriteLine($"  👁️  Views: {result.ConflictDetection.ViewConflicts.Count} files");
                Console.WriteLine();
                
                // Build status
                if (result.InitialBuildResult != null && result.FinalBuildResult != null)
                {
                    Console.WriteLine("Build Status:");
                    Console.WriteLine($"  Initial Errors: {result.InitialBuildResult.ErrorCount}");
                    Console.WriteLine($"  Final Errors: {result.FinalBuildResult.ErrorCount}");
                    
                    var errorsResolved = Math.Max(0, result.InitialBuildResult.ErrorCount - result.FinalBuildResult.ErrorCount);
                    Console.WriteLine($"  Errors Resolved: {errorsResolved}");
                    Console.WriteLine();
                }
                
                // Resolution steps
                Console.WriteLine("Resolution Steps:");
                foreach (var step in result.ResolutionSteps)
                {
                    var stepIcon = step.Success ? "✅" : "❌";
                    Console.WriteLine($"  {stepIcon} {step.StepName}: {step.FilesModified.Count} files modified");
                }
                Console.WriteLine();
                
                // Generate report
                var report = orchestrator.GenerateProgressReport(result);
                var reportPath = Path.Combine(projectPath, "conflict_resolution_report.txt");
                await File.WriteAllTextAsync(reportPath, report);
                
                Console.WriteLine($"📊 Detailed report saved to: {reportPath}");
                Console.WriteLine();
                
                // Final status
                if (result.Success && result.FinalBuildResult?.ErrorCount == 0)
                {
                    Console.WriteLine("🎉 SUCCESS: All namespace conflicts resolved!");
                    Console.WriteLine("✅ Zero compilation errors achieved");
                }
                else if (result.Success)
                {
                    Console.WriteLine("⚠️  PARTIAL SUCCESS: Process completed with some remaining issues");
                }
                else
                {
                    Console.WriteLine("❌ FAILED: Conflict resolution encountered errors");
                }
                
                Console.WriteLine($"Completed at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fatal error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}