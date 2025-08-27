using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace RevitDtools.Core.Services
{
    /// <summary>
    /// Resolves TaskDialog namespace conflicts by applying RevitTaskDialog alias pattern
    /// </summary>
    public class TaskDialogResolver
    {
        private const string REVIT_TASK_DIALOG_ALIAS = "using RevitTaskDialog = Autodesk.Revit.UI.TaskDialog;";
        
        /// <summary>
        /// Processes a single file to resolve TaskDialog conflicts
        /// </summary>
        /// <param name="filePath">Path to the C# file to process</param>
        /// <returns>True if the file was modified, false otherwise</returns>
        public bool ProcessFile(string filePath)
        {
            if (!File.Exists(filePath) || !filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var originalContent = File.ReadAllText(filePath);
            var modifiedContent = originalContent;
            bool hasChanges = false;

            // Check if file contains TaskDialog usage
            if (ContainsTaskDialogUsage(originalContent))
            {
                // Add alias if not already present
                if (!originalContent.Contains("using RevitTaskDialog ="))
                {
                    modifiedContent = AddRevitTaskDialogAlias(modifiedContent);
                    hasChanges = true;
                }

                // Replace TaskDialog references with RevitTaskDialog
                var updatedContent = ReplaceTaskDialogReferences(modifiedContent);
                if (updatedContent != modifiedContent)
                {
                    modifiedContent = updatedContent;
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    File.WriteAllText(filePath, modifiedContent);
                }
            }

            return hasChanges;
        }

        /// <summary>
        /// Processes multiple files to resolve TaskDialog conflicts
        /// </summary>
        /// <param name="filePaths">Collection of file paths to process</param>
        /// <returns>List of files that were modified</returns>
        public List<string> ProcessFiles(IEnumerable<string> filePaths)
        {
            var modifiedFiles = new List<string>();

            foreach (var filePath in filePaths)
            {
                if (ProcessFile(filePath))
                {
                    modifiedFiles.Add(filePath);
                }
            }

            return modifiedFiles;
        }

        /// <summary>
        /// Scans a directory for C# files and processes them for TaskDialog conflicts
        /// </summary>
        /// <param name="directoryPath">Directory to scan</param>
        /// <param name="recursive">Whether to scan subdirectories</param>
        /// <returns>List of files that were modified</returns>
        public List<string> ProcessDirectory(string directoryPath, bool recursive = true)
        {
            if (!Directory.Exists(directoryPath))
            {
                return new List<string>();
            }

            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var csFiles = Directory.GetFiles(directoryPath, "*.cs", searchOption);

            return ProcessFiles(csFiles);
        }

        /// <summary>
        /// Checks if the content contains TaskDialog usage that needs resolution
        /// </summary>
        private bool ContainsTaskDialogUsage(string content)
        {
            // Look for TaskDialog usage patterns
            var patterns = new[]
            {
                @"\bTaskDialog\s+\w+",           // TaskDialog variable declarations
                @"\bnew\s+TaskDialog\b",         // TaskDialog instantiations
                @"\bTaskDialog\.Show\b",         // TaskDialog.Show calls
                @"\bTaskDialogResult\b",         // TaskDialogResult usage
                @"\bTaskDialogCommonButtons\b"   // TaskDialogCommonButtons usage
            };

            return patterns.Any(pattern => Regex.IsMatch(content, pattern));
        }

        /// <summary>
        /// Adds the RevitTaskDialog alias to the using statements
        /// </summary>
        private string AddRevitTaskDialogAlias(string content)
        {
            // Find the last using statement
            var usingMatches = Regex.Matches(content, @"^using\s+[^;]+;", RegexOptions.Multiline);
            
            if (usingMatches.Count > 0)
            {
                var lastUsing = usingMatches[usingMatches.Count - 1];
                var insertPosition = lastUsing.Index + lastUsing.Length;
                
                // Insert the alias after the last using statement
                content = content.Insert(insertPosition, Environment.NewLine + REVIT_TASK_DIALOG_ALIAS);
            }
            else
            {
                // If no using statements found, add at the beginning
                content = REVIT_TASK_DIALOG_ALIAS + Environment.NewLine + Environment.NewLine + content;
            }

            return content;
        }

        /// <summary>
        /// Replaces TaskDialog references with RevitTaskDialog
        /// </summary>
        private string ReplaceTaskDialogReferences(string content)
        {
            // Replace TaskDialog instantiations
            content = Regex.Replace(content, @"\bnew\s+TaskDialog\b", "new RevitTaskDialog");
            
            // Replace TaskDialog type references (variable declarations, parameters, etc.)
            content = Regex.Replace(content, @"\bTaskDialog\s+(\w+)", "RevitTaskDialog $1");
            
            // Replace TaskDialog static method calls
            content = Regex.Replace(content, @"\bTaskDialog\.", "RevitTaskDialog.");
            
            // Handle TaskDialog in var declarations
            content = Regex.Replace(content, @"(\bvar\s+\w+\s*=\s*)new\s+TaskDialog\b", "$1new RevitTaskDialog");

            return content;
        }

        /// <summary>
        /// Gets a summary of TaskDialog usage in a file
        /// </summary>
        /// <param name="filePath">Path to the file to analyze</param>
        /// <returns>Summary of TaskDialog usage</returns>
        public TaskDialogUsageSummary AnalyzeFile(string filePath)
        {
            var summary = new TaskDialogUsageSummary { FilePath = filePath };

            if (!File.Exists(filePath))
            {
                return summary;
            }

            var content = File.ReadAllText(filePath);
            
            // Count different types of TaskDialog usage
            summary.TaskDialogInstantiations = Regex.Matches(content, @"\bnew\s+TaskDialog\b").Count;
            summary.TaskDialogShowCalls = Regex.Matches(content, @"\bTaskDialog\.Show\b").Count;
            summary.TaskDialogVariables = Regex.Matches(content, @"\bTaskDialog\s+\w+").Count;
            summary.TaskDialogResultUsage = Regex.Matches(content, @"\bTaskDialogResult\b").Count;
            summary.TaskDialogCommonButtonsUsage = Regex.Matches(content, @"\bTaskDialogCommonButtons\b").Count;
            
            summary.HasTaskDialogUsage = summary.TaskDialogInstantiations > 0 || 
                                       summary.TaskDialogShowCalls > 0 || 
                                       summary.TaskDialogVariables > 0 ||
                                       summary.TaskDialogResultUsage > 0 ||
                                       summary.TaskDialogCommonButtonsUsage > 0;

            summary.HasRevitTaskDialogAlias = content.Contains("using RevitTaskDialog =");

            return summary;
        }
    }

    /// <summary>
    /// Summary of TaskDialog usage in a file
    /// </summary>
    public class TaskDialogUsageSummary
    {
        public string FilePath { get; set; }
        public bool HasTaskDialogUsage { get; set; }
        public bool HasRevitTaskDialogAlias { get; set; }
        public int TaskDialogInstantiations { get; set; }
        public int TaskDialogShowCalls { get; set; }
        public int TaskDialogVariables { get; set; }
        public int TaskDialogResultUsage { get; set; }
        public int TaskDialogCommonButtonsUsage { get; set; }

        public int TotalUsages => TaskDialogInstantiations + TaskDialogShowCalls + TaskDialogVariables + 
                                TaskDialogResultUsage + TaskDialogCommonButtonsUsage;
    }
}