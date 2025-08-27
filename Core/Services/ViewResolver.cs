using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace RevitDtools.Core.Services
{
    /// <summary>
    /// Resolves View namespace conflicts by applying RevitView alias pattern
    /// </summary>
    public class ViewResolver
    {
        private const string REVIT_VIEW_ALIAS = "using RevitView = Autodesk.Revit.DB.View;";
        
        /// <summary>
        /// Processes a single file to resolve View conflicts
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

            // Check if file contains View usage in Revit context
            if (ContainsRevitViewUsage(originalContent))
            {
                // Add alias if not already present
                if (!originalContent.Contains("using RevitView ="))
                {
                    modifiedContent = AddRevitViewAlias(modifiedContent);
                    hasChanges = true;
                }

                // Replace View references with RevitView
                var updatedContent = ReplaceViewReferences(modifiedContent);
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
        /// Processes multiple files to resolve View conflicts
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
        /// Scans a directory for C# files and processes them for View conflicts
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
        /// Checks if the content contains View usage in Revit context that needs resolution
        /// </summary>
        private bool ContainsRevitViewUsage(string content)
        {
            // Look for View usage patterns in Revit context
            var patterns = new[]
            {
                @"\bView\s+\w+",                    // View variable declarations
                @"\bnew\s+View\b",                  // View instantiations (rare but possible)
                @"\(View\s+\w+\)",                  // View method parameters
                @"<View>",                          // View in generics
                @"\bView\s*\[\]",                   // View arrays
                @"\bICollection<View>",             // View collections
                @"\bIEnumerable<View>",             // View enumerables
                @"\bList<View>",                    // View lists
                @":\s*View\b",                      // View inheritance/implementation
                @"\bas\s+View\b",                   // View casting
                @"\bis\s+View\b"                    // View type checking
            };

            // Only consider it a Revit View if the file also contains Revit-related imports or usage
            bool hasRevitContext = content.Contains("Autodesk.Revit") || 
                                 content.Contains("using Autodesk.Revit") ||
                                 content.Contains("Element") ||
                                 content.Contains("Document") ||
                                 content.Contains("Transaction");

            return hasRevitContext && patterns.Any(pattern => Regex.IsMatch(content, pattern));
        }

        /// <summary>
        /// Adds the RevitView alias to the using statements
        /// </summary>
        private string AddRevitViewAlias(string content)
        {
            // Find the last using statement
            var usingMatches = Regex.Matches(content, @"^using\s+[^;]+;", RegexOptions.Multiline);
            
            if (usingMatches.Count > 0)
            {
                var lastUsing = usingMatches[usingMatches.Count - 1];
                var insertPosition = lastUsing.Index + lastUsing.Length;
                
                // Insert the alias after the last using statement
                content = content.Insert(insertPosition, Environment.NewLine + REVIT_VIEW_ALIAS);
            }
            else
            {
                // If no using statements found, add at the beginning
                content = REVIT_VIEW_ALIAS + Environment.NewLine + Environment.NewLine + content;
            }

            return content;
        }

        /// <summary>
        /// Replaces View references with RevitView in Revit context
        /// </summary>
        private string ReplaceViewReferences(string content)
        {
            // Replace View instantiations (rare but possible)
            content = Regex.Replace(content, @"\bnew\s+View\b", "new RevitView");
            
            // Replace View type references in variable declarations
            content = Regex.Replace(content, @"\bView\s+(\w+)\s*=", "RevitView $1 =");
            
            // Replace View in method parameters
            content = Regex.Replace(content, @"\(View\s+(\w+)\)", "(RevitView $1)");
            
            // Replace View in method parameters with multiple parameters
            content = Regex.Replace(content, @",\s*View\s+(\w+)", ", RevitView $1");
            content = Regex.Replace(content, @"\(\s*View\s+(\w+),", "(RevitView $1,");
            
            // Replace View in generic constraints
            content = Regex.Replace(content, @"<View>", "<RevitView>");
            content = Regex.Replace(content, @"<View,", "<RevitView,");
            content = Regex.Replace(content, @",\s*View>", ", RevitView>");
            
            // Replace View arrays
            content = Regex.Replace(content, @"\bView\s*\[\]", "RevitView[]");
            
            // Replace View in collections
            content = Regex.Replace(content, @"\bICollection<View>", "ICollection<RevitView>");
            content = Regex.Replace(content, @"\bIEnumerable<View>", "IEnumerable<RevitView>");
            content = Regex.Replace(content, @"\bList<View>", "List<RevitView>");
            content = Regex.Replace(content, @"\bIList<View>", "IList<RevitView>");
            
            // Replace View in inheritance/implementation
            content = Regex.Replace(content, @":\s*View\b", ": RevitView");
            
            // Replace View in casting
            content = Regex.Replace(content, @"\bas\s+View\b", "as RevitView");
            content = Regex.Replace(content, @"\bis\s+View\b", "is RevitView");
            content = Regex.Replace(content, @"\(View\)", "(RevitView)");

            return content;
        }

        /// <summary>
        /// Gets a summary of View usage in a file
        /// </summary>
        /// <param name="filePath">Path to the file to analyze</param>
        /// <returns>Summary of View usage</returns>
        public ViewUsageSummary AnalyzeFile(string filePath)
        {
            var summary = new ViewUsageSummary { FilePath = filePath };

            if (!File.Exists(filePath))
            {
                return summary;
            }

            var content = File.ReadAllText(filePath);
            
            // Count different types of View usage
            summary.ViewVariables = Regex.Matches(content, @"\bView\s+\w+").Count;
            summary.ViewInstantiations = Regex.Matches(content, @"\bnew\s+View\b").Count;
            summary.ViewParameters = Regex.Matches(content, @"\(View\s+\w+\)").Count;
            summary.ViewGenerics = Regex.Matches(content, @"<View>").Count;
            summary.ViewArrays = Regex.Matches(content, @"\bView\s*\[\]").Count;
            summary.ViewCollections = Regex.Matches(content, @"\b(ICollection|IEnumerable|List|IList)<View>").Count;
            summary.ViewCasting = Regex.Matches(content, @"\b(as|is)\s+View\b").Count;
            
            summary.HasViewUsage = summary.ViewVariables > 0 || 
                                 summary.ViewInstantiations > 0 || 
                                 summary.ViewParameters > 0 ||
                                 summary.ViewGenerics > 0 ||
                                 summary.ViewArrays > 0 ||
                                 summary.ViewCollections > 0 ||
                                 summary.ViewCasting > 0;

            summary.HasRevitViewAlias = content.Contains("using RevitView =");
            summary.HasRevitContext = content.Contains("Autodesk.Revit") || 
                                    content.Contains("using Autodesk.Revit") ||
                                    content.Contains("Element") ||
                                    content.Contains("Document") ||
                                    content.Contains("Transaction");

            return summary;
        }
    }

    /// <summary>
    /// Summary of View usage in a file
    /// </summary>
    public class ViewUsageSummary
    {
        public string FilePath { get; set; }
        public bool HasViewUsage { get; set; }
        public bool HasRevitViewAlias { get; set; }
        public bool HasRevitContext { get; set; }
        public int ViewVariables { get; set; }
        public int ViewInstantiations { get; set; }
        public int ViewParameters { get; set; }
        public int ViewGenerics { get; set; }
        public int ViewArrays { get; set; }
        public int ViewCollections { get; set; }
        public int ViewCasting { get; set; }

        public int TotalUsages => ViewVariables + ViewInstantiations + ViewParameters + 
                                ViewGenerics + ViewArrays + ViewCollections + ViewCasting;
    }
}