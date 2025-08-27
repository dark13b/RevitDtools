using System;
using System.IO;
using System.Linq;
using RevitDtools.Core.Services;
using RevitDtools.Utilities;

namespace RevitDtools
{
    /// <summary>
    /// Utility class to apply UI control conflict resolution to the project
    /// </summary>
    public class ApplyUIControlFix
    {
        public static void Main(string[] args)
        {
            var logger = Logger.Instance;
            var resolver = new UIControlResolver(logger);
            var projectPath = Directory.GetCurrentDirectory();

            Console.WriteLine("UI Control Conflict Resolution Tool");
            Console.WriteLine("==================================");
            Console.WriteLine($"Project Path: {projectPath}");
            Console.WriteLine();

            try
            {
                // Generate initial conflict report
                Console.WriteLine("Scanning for UI control conflicts...");
                var report = resolver.GenerateConflictReport(projectPath);
                Console.WriteLine(report);

                // Find files with conflicts
                var conflictFiles = resolver.ScanForConflicts(projectPath);
                
                if (!conflictFiles.Any())
                {
                    Console.WriteLine("No UI control conflicts found.");
                    return;
                }

                Console.WriteLine($"Found {conflictFiles.Count} files with UI control conflicts:");
                foreach (var file in conflictFiles)
                {
                    Console.WriteLine($"  - {Path.GetRelativePath(projectPath, file)}");
                }
                Console.WriteLine();

                // Apply fixes
                Console.WriteLine("Applying UI control conflict resolution...");
                var resolvedCount = resolver.ResolveConflictsInFiles(conflictFiles);
                
                Console.WriteLine($"Successfully resolved conflicts in {resolvedCount} files.");
                
                // Generate final report
                Console.WriteLine();
                Console.WriteLine("Final conflict scan...");
                var finalReport = resolver.GenerateConflictReport(projectPath);
                Console.WriteLine(finalReport);

                Console.WriteLine("UI control conflict resolution completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during UI control conflict resolution: {ex.Message}");
                Logger.LogError(ex, "UI control conflict resolution failed");
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}