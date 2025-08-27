using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace RevitDtools.Core.Services
{
    /// <summary>
    /// Resolves file dialog namespace conflicts by applying consistent WinForms dialog aliases
    /// </summary>
    public class DialogResolver
    {
        private readonly Dictionary<string, string> _winFormsDialogAliases;
        private readonly Dictionary<string, string> _wpfDialogAliases;

        public DialogResolver()
        {
            _winFormsDialogAliases = new Dictionary<string, string>
            {
                { "OpenFileDialog", "WinFormsOpenFileDialog" },
                { "SaveFileDialog", "WinFormsSaveFileDialog" },
                { "FolderBrowserDialog", "WinFormsFolderBrowserDialog" },
                { "DialogResult", "WinFormsDialogResult" },
                { "ColorDialog", "WinFormsColorDialog" },
                { "FontDialog", "WinFormsFontDialog" },
                { "PrintDialog", "WinFormsPrintDialog" }
            };

            _wpfDialogAliases = new Dictionary<string, string>
            {
                { "OpenFileDialog", "WpfOpenFileDialog" },
                { "SaveFileDialog", "WpfSaveFileDialog" }
            };
        }

        /// <summary>
        /// Resolves file dialog conflicts in the specified file
        /// </summary>
        public DialogResolutionResult ResolveDialogConflicts(string filePath)
        {
            var result = new DialogResolutionResult
            {
                FilePath = filePath,
                Success = false
            };

            try
            {
                if (!File.Exists(filePath))
                {
                    result.ErrorMessage = "File does not exist";
                    return result;
                }

                var originalContent = File.ReadAllText(filePath);
                var modifiedContent = originalContent;
                var aliasesToAdd = new HashSet<string>();

                // Detect and resolve WinForms dialog conflicts
                var winFormsConflicts = DetectWinFormsDialogConflicts(originalContent);
                if (winFormsConflicts.Any())
                {
                    modifiedContent = ResolveWinFormsDialogConflicts(modifiedContent, winFormsConflicts, aliasesToAdd);
                    result.WinFormsDialogsResolved.AddRange(winFormsConflicts);
                }

                // Detect and resolve WPF dialog conflicts (Microsoft.Win32)
                var wpfConflicts = DetectWpfDialogConflicts(originalContent);
                if (wpfConflicts.Any())
                {
                    modifiedContent = ResolveWpfDialogConflicts(modifiedContent, wpfConflicts, aliasesToAdd);
                    result.WpfDialogsResolved.AddRange(wpfConflicts);
                }

                // Add aliases to the file if any conflicts were found
                if (aliasesToAdd.Any())
                {
                    modifiedContent = AddAliasesToFile(modifiedContent, aliasesToAdd);
                    result.AliasesAdded.AddRange(aliasesToAdd);
                }

                // Write the modified content back to the file
                if (modifiedContent != originalContent)
                {
                    File.WriteAllText(filePath, modifiedContent);
                    result.Success = true;
                    result.ChangesApplied = true;
                }
                else
                {
                    result.Success = true;
                    result.ChangesApplied = false;
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Detects WinForms dialog conflicts in the content
        /// </summary>
        private List<string> DetectWinFormsDialogConflicts(string content)
        {
            var conflicts = new List<string>();

            // Check for using statements that indicate WinForms usage
            var hasWinFormsUsing = Regex.IsMatch(content, @"using\s+System\.Windows\.Forms");
            var hasWpfUsing = Regex.IsMatch(content, @"using\s+System\.Windows") && !Regex.IsMatch(content, @"using\s+System\.Windows\.Forms");

            if (hasWinFormsUsing && hasWpfUsing)
            {
                // Check for specific dialog usage patterns
                foreach (var dialogType in _winFormsDialogAliases.Keys)
                {
                    if (Regex.IsMatch(content, $@"\b{dialogType}\b") && !Regex.IsMatch(content, $@"WinForms{dialogType}"))
                    {
                        conflicts.Add(dialogType);
                    }
                }
            }

            return conflicts;
        }

        /// <summary>
        /// Detects WPF dialog conflicts in the content
        /// </summary>
        private List<string> DetectWpfDialogConflicts(string content)
        {
            var conflicts = new List<string>();

            // Check for Microsoft.Win32 usage alongside WinForms
            var hasMicrosoftWin32Using = Regex.IsMatch(content, @"using\s+Microsoft\.Win32");
            var hasWinFormsUsing = Regex.IsMatch(content, @"using\s+System\.Windows\.Forms");

            if (hasMicrosoftWin32Using && hasWinFormsUsing)
            {
                // Check for specific dialog usage patterns
                foreach (var dialogType in _wpfDialogAliases.Keys)
                {
                    if (Regex.IsMatch(content, $@"\b{dialogType}\b") && !Regex.IsMatch(content, $@"Wpf{dialogType}"))
                    {
                        conflicts.Add(dialogType);
                    }
                }
            }

            return conflicts;
        }

        /// <summary>
        /// Resolves WinForms dialog conflicts by applying aliases
        /// </summary>
        private string ResolveWinFormsDialogConflicts(string content, List<string> conflicts, HashSet<string> aliasesToAdd)
        {
            var modifiedContent = content;

            foreach (var conflict in conflicts)
            {
                if (_winFormsDialogAliases.TryGetValue(conflict, out var alias))
                {
                    // Add the alias using statement
                    aliasesToAdd.Add($"using {alias} = System.Windows.Forms.{conflict};");

                    // Replace dialog instantiations
                    modifiedContent = Regex.Replace(modifiedContent, 
                        $@"\bnew {conflict}\b", 
                        $"new {alias}");

                    // Replace dialog type references
                    modifiedContent = Regex.Replace(modifiedContent, 
                        $@"\b{conflict}\s+(\w+)", 
                        $"{alias} $1");

                    // Replace dialog variable declarations
                    modifiedContent = Regex.Replace(modifiedContent, 
                        $@"var\s+(\w+)\s*=\s*new\s+{conflict}\b", 
                        $"var $1 = new {alias}");
                }
            }

            return modifiedContent;
        }

        /// <summary>
        /// Resolves WPF dialog conflicts by applying aliases
        /// </summary>
        private string ResolveWpfDialogConflicts(string content, List<string> conflicts, HashSet<string> aliasesToAdd)
        {
            var modifiedContent = content;

            foreach (var conflict in conflicts)
            {
                if (_wpfDialogAliases.TryGetValue(conflict, out var alias))
                {
                    // Add the alias using statement
                    aliasesToAdd.Add($"using {alias} = Microsoft.Win32.{conflict};");

                    // Replace dialog instantiations with Microsoft.Win32 prefix
                    modifiedContent = Regex.Replace(modifiedContent, 
                        $@"\bnew Microsoft\.Win32\.{conflict}\b", 
                        $"new {alias}");

                    // Replace dialog type references with Microsoft.Win32 prefix
                    modifiedContent = Regex.Replace(modifiedContent, 
                        $@"\bMicrosoft\.Win32\.{conflict}\s+(\w+)", 
                        $"{alias} $1");

                    // Replace variable declarations with Microsoft.Win32 prefix
                    modifiedContent = Regex.Replace(modifiedContent, 
                        $@"var\s+(\w+)\s*=\s*new\s+Microsoft\.Win32\.{conflict}\b", 
                        $"var $1 = new {alias}");
                }
            }

            return modifiedContent;
        }

        /// <summary>
        /// Adds alias using statements to the top of the file
        /// </summary>
        private string AddAliasesToFile(string content, HashSet<string> aliases)
        {
            if (!aliases.Any()) return content;

            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var insertIndex = FindUsingInsertionPoint(lines);

            // Insert aliases at the appropriate location
            foreach (var alias in aliases.OrderBy(a => a))
            {
                lines.Insert(insertIndex, alias);
                insertIndex++;
            }

            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Finds the appropriate insertion point for using statements
        /// </summary>
        private int FindUsingInsertionPoint(List<string> lines)
        {
            var lastUsingIndex = -1;
            
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith("using ") && !line.Contains("="))
                {
                    lastUsingIndex = i;
                }
                else if (line.StartsWith("namespace ") || line.StartsWith("public ") || line.StartsWith("internal "))
                {
                    break;
                }
            }

            return lastUsingIndex + 1;
        }

        /// <summary>
        /// Scans a directory for files with dialog conflicts
        /// </summary>
        public List<DialogConflictInfo> ScanForDialogConflicts(string directoryPath, string searchPattern = "*.cs")
        {
            var conflicts = new List<DialogConflictInfo>();

            try
            {
                var files = Directory.GetFiles(directoryPath, searchPattern, SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    var content = File.ReadAllText(file);
                    var winFormsConflicts = DetectWinFormsDialogConflicts(content);
                    var wpfConflicts = DetectWpfDialogConflicts(content);

                    if (winFormsConflicts.Any() || wpfConflicts.Any())
                    {
                        conflicts.Add(new DialogConflictInfo
                        {
                            FilePath = file,
                            WinFormsDialogConflicts = winFormsConflicts,
                            WpfDialogConflicts = wpfConflicts
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but continue processing
                Console.WriteLine($"Error scanning directory {directoryPath}: {ex.Message}");
            }

            return conflicts;
        }
    }

    /// <summary>
    /// Result of dialog conflict resolution
    /// </summary>
    public class DialogResolutionResult
    {
        public string FilePath { get; set; } = string.Empty;
        public bool Success { get; set; }
        public bool ChangesApplied { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> AliasesAdded { get; set; } = new List<string>();
        public List<string> WinFormsDialogsResolved { get; set; } = new List<string>();
        public List<string> WpfDialogsResolved { get; set; } = new List<string>();
    }

    /// <summary>
    /// Information about dialog conflicts in a file
    /// </summary>
    public class DialogConflictInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public List<string> WinFormsDialogConflicts { get; set; } = new List<string>();
        public List<string> WpfDialogConflicts { get; set; } = new List<string>();
    }
}