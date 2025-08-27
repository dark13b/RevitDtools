using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RevitDtools.Core.Services;

namespace RevitDtools
{
    /// <summary>
    /// Tool to resolve TaskDialog namespace conflicts across the project
    /// </summary>
    public class TaskDialogResolutionTool
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("TaskDialog Namespace Conflict Resolution Tool");
            Console.WriteLine("============================================");
            
            var resolver = new TaskDialogResolver();
            var projectRoot = Directory.GetCurrentDirectory();
            
            Console.WriteLine($"Scanning project directory: {projectRoot}");
            Console.WriteLine();

            // First, analyze all files to show current state
            Console.WriteLine("Analyzing current TaskDialog usage...");
            AnalyzeProject(resolver, projectRoot);
            
            Console.WriteLine();
            Console.WriteLine("Applying TaskDialog conflict resolution...");
            
            // Apply the resolution
            var modifiedFiles = resolver.ProcessDirectory(projectRoot, recursive: true);
            
            Console.WriteLine($"Modified {modifiedFiles.Count} files:");
            foreach (var file in modifiedFiles)
            {
                Console.WriteLine($"  - {Path.GetRelativePath(projectRoot, file)}");
            }
            
            Console.WriteLine();
            Console.WriteLine("Re-analyzing after resolution...");
            AnalyzeProject(resolver, projectRoot);
            
            Console.WriteLine();
            Console.WriteLine("TaskDialog conflict resolution completed!");
        }

        private static void AnalyzeProject(TaskDialogResolver resolver, string projectRoot)
        {
            var csFiles = Directory.GetFiles(projectRoot, "*.cs", SearchOption.AllDirectories)
                                 .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))
                                 .ToList();

            var filesWithTaskDialog = new List<TaskDialogUsageSummary>();
            
            foreach (var file in csFiles)
            {
                var summary = resolver.AnalyzeFile(file);
                if (summary.HasTaskDialogUsage)
                {
                    filesWithTaskDialog.Add(summary);
                }
            }

            Console.WriteLine($"Found {filesWithTaskDialog.Count} files with TaskDialog usage:");
            
            foreach (var summary in filesWithTaskDialog)
            {
                var relativePath = Path.GetRelativePath(projectRoot, summary.FilePath);
                Console.WriteLine($"  {relativePath}:");
                Console.WriteLine($"    - Instantiations: {summary.TaskDialogInstantiations}");
                Console.WriteLine($"    - Show calls: {summary.TaskDialogShowCalls}");
                Console.WriteLine($"    - Variables: {summary.TaskDialogVariables}");
                Console.WriteLine($"    - Result usage: {summary.TaskDialogResultUsage}");
                Console.WriteLine($"    - CommonButtons usage: {summary.TaskDialogCommonButtonsUsage}");
                Console.WriteLine($"    - Has RevitTaskDialog alias: {summary.HasRevitTaskDialogAlias}");
                Console.WriteLine($"    - Total usages: {summary.TotalUsages}");
                Console.WriteLine();
            }
        }
    }
}