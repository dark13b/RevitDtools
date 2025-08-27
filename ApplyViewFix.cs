using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RevitDtools.Core.Services;

namespace RevitDtools
{
    /// <summary>
    /// Application tool to apply View namespace conflict resolution across the project
    /// </summary>
    public class ApplyViewFix
    {
        private readonly ViewResolver _resolver;

        public ApplyViewFix()
        {
            _resolver = new ViewResolver();
        }

        /// <summary>
        /// Main entry point for applying View conflict fixes
        /// </summary>
        public static void Main(string[] args)
        {
            var tool = new ApplyViewFix();
            
            try
            {
                Console.WriteLine("=== RevitDtools View Conflict Resolution Tool ===");
                Console.WriteLine();

                // Get the project root directory
                var projectRoot = GetProjectRoot();
                if (string.IsNullOrEmpty(projectRoot))
                {
                    Console.WriteLine("Error: Could not find project root directory.");
                    return;
                }

                Console.WriteLine($"Project root: {projectRoot}");
                Console.WriteLine();

                // Analyze current state
                Console.WriteLine("Analyzing current View usage...");
                var analysisResults = tool.AnalyzeProject(projectRoot);
                tool.DisplayAnalysisResults(analysisResults);

                // Apply fixes
                Console.WriteLine("Applying View conflict resolution...");
                var modifiedFiles = tool.ApplyFixes(projectRoot);
                
                Console.WriteLine($"Modified {modifiedFiles.Count} files:");
                foreach (var file in modifiedFiles)
                {
                    Console.WriteLine($"  - {Path.GetRelativePath(projectRoot, file)}");
                }

                Console.WriteLine();
                Console.WriteLine("View conflict resolution completed successfully!");
                Console.WriteLine("Please build the project to verify that View-related errors are resolved.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Analyzes the project for View usage
        /// </summary>
        public List<ViewUsageSummary> AnalyzeProject(string projectRoot)
        {
            var results = new List<ViewUsageSummary>();
            var csFiles = Directory.GetFiles(projectRoot, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))
                .ToList();

            foreach (var file in csFiles)
            {
                var summary = _resolver.AnalyzeFile(file);
                if (summary.HasViewUsage && summary.HasRevitContext)
                {
                    results.Add(summary);
                }
            }

            return results;
        }

        /// <summary>
        /// Applies View conflict fixes to the project
        /// </summary>
        public List<string> ApplyFixes(string projectRoot)
        {
            return _resolver.ProcessDirectory(projectRoot, recursive: true);
        }

        /// <summary>
        /// Displays analysis results
        /// </summary>
        private void DisplayAnalysisResults(List<ViewUsageSummary> results)
        {
            Console.WriteLine($"Found {results.Count} files with View usage in Revit context:");
            Console.WriteLine();

            var totalUsages = 0;
            var filesWithAlias = 0;

            foreach (var result in results)
            {
                var relativePath = Path.GetFileName(result.FilePath);
                Console.WriteLine($"  {relativePath}:");
                Console.WriteLine($"    - View variables: {result.ViewVariables}");
                Console.WriteLine($"    - View parameters: {result.ViewParameters}");
                Console.WriteLine($"    - View generics: {result.ViewGenerics}");
                Console.WriteLine($"    - View arrays: {result.ViewArrays}");
                Console.WriteLine($"    - View collections: {result.ViewCollections}");
                Console.WriteLine($"    - View casting: {result.ViewCasting}");
                Console.WriteLine($"    - Total usages: {result.TotalUsages}");
                Console.WriteLine($"    - Has RevitView alias: {result.HasRevitViewAlias}");
                Console.WriteLine();

                totalUsages += result.TotalUsages;
                if (result.HasRevitViewAlias)
                {
                    filesWithAlias++;
                }
            }

            Console.WriteLine($"Summary:");
            Console.WriteLine($"  - Total files with View usage: {results.Count}");
            Console.WriteLine($"  - Total View usages: {totalUsages}");
            Console.WriteLine($"  - Files already with RevitView alias: {filesWithAlias}");
            Console.WriteLine($"  - Files needing fixes: {results.Count - filesWithAlias}");
            Console.WriteLine();
        }

        /// <summary>
        /// Gets the project root directory
        /// </summary>
        private static string GetProjectRoot()
        {
            var currentDir = Directory.GetCurrentDirectory();
            
            // Look for .csproj file
            while (!string.IsNullOrEmpty(currentDir))
            {
                if (Directory.GetFiles(currentDir, "*.csproj").Any())
                {
                    return currentDir;
                }
                
                var parent = Directory.GetParent(currentDir);
                currentDir = parent?.FullName;
            }

            return Directory.GetCurrentDirectory();
        }
    }
}