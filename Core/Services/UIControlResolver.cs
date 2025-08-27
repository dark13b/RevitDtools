using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RevitDtools.Core.Interfaces;
using RevitDtools.Utilities;

namespace RevitDtools.Core.Services
{
    /// <summary>
    /// Resolves UI control namespace conflicts between WPF and WinForms controls
    /// </summary>
    public class UIControlResolver
    {
        private readonly ILogger _logger;
        private Dictionary<string, string> _wpfControlAliases;
        private Dictionary<string, string> _winFormsControlAliases;

        public UIControlResolver(ILogger? logger = null)
        {
            _logger = logger ?? Logger.Instance;
            InitializeAliases();
        }

        /// <summary>
        /// Initialize control alias mappings
        /// </summary>
        private void InitializeAliases()
        {
            _wpfControlAliases = new Dictionary<string, string>
            {
                { "TextBox", "using WpfTextBox = System.Windows.Controls.TextBox;" },
                { "ComboBox", "using WpfComboBox = System.Windows.Controls.ComboBox;" },
                { "CheckBox", "using WpfCheckBox = System.Windows.Controls.CheckBox;" },
                { "Button", "using WpfButton = System.Windows.Controls.Button;" },
                { "ListBox", "using WpfListBox = System.Windows.Controls.ListBox;" },
                { "Label", "using WpfLabel = System.Windows.Controls.Label;" }
            };

            _winFormsControlAliases = new Dictionary<string, string>
            {
                { "FolderBrowserDialog", "using WinFormsFolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;" },
                { "OpenFileDialog", "using WinFormsOpenFileDialog = System.Windows.Forms.OpenFileDialog;" },
                { "SaveFileDialog", "using WinFormsSaveFileDialog = System.Windows.Forms.SaveFileDialog;" },
                { "DialogResult", "using WinFormsDialogResult = System.Windows.Forms.DialogResult;" }
            };
        }

        /// <summary>
        /// Resolve UI control conflicts in a single file
        /// </summary>
        public bool ResolveConflictsInFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning($"File not found: {filePath}");
                    return false;
                }

                var originalContent = File.ReadAllText(filePath);
                var modifiedContent = originalContent;
                var aliasesToAdd = new HashSet<string>();
                bool hasChanges = false;

                // Detect and resolve WinForms dialog conflicts
                if (DetectWinFormsDialogConflicts(modifiedContent))
                {
                    modifiedContent = ResolveWinFormsDialogConflicts(modifiedContent, aliasesToAdd);
                    hasChanges = true;
                }

                // Detect and resolve WPF control conflicts (if both WPF and WinForms are used)
                if (DetectWpfControlConflicts(modifiedContent))
                {
                    modifiedContent = ResolveWpfControlConflicts(modifiedContent, aliasesToAdd);
                    hasChanges = true;
                }

                // Add aliases to the file if changes were made
                if (hasChanges && aliasesToAdd.Any())
                {
                    modifiedContent = AddAliasesToFile(modifiedContent, aliasesToAdd);
                    
                    // Create backup before modifying
                    var backupPath = filePath + ".backup";
                    File.Copy(filePath, backupPath, true);
                    
                    File.WriteAllText(filePath, modifiedContent);
                    
                    _logger.LogInfo($"Resolved UI control conflicts in: {filePath}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resolving UI control conflicts in file: {filePath}");
                return false;
            }
        }

        /// <summary>
        /// Resolve UI control conflicts in multiple files
        /// </summary>
        public int ResolveConflictsInFiles(IEnumerable<string> filePaths)
        {
            int resolvedCount = 0;
            
            foreach (var filePath in filePaths)
            {
                if (ResolveConflictsInFile(filePath))
                {
                    resolvedCount++;
                }
            }

            _logger.LogInfo($"Resolved UI control conflicts in {resolvedCount} files");
            return resolvedCount;
        }

        /// <summary>
        /// Detect WinForms dialog conflicts
        /// </summary>
        private bool DetectWinFormsDialogConflicts(string content)
        {
            // Look for System.Windows.Forms usage without aliases
            var patterns = new[]
            {
                @"new\s+System\.Windows\.Forms\.FolderBrowserDialog\s*\(",
                @"System\.Windows\.Forms\.DialogResult\.",
                @"new\s+System\.Windows\.Forms\.OpenFileDialog\s*\(",
                @"new\s+System\.Windows\.Forms\.SaveFileDialog\s*\("
            };

            return patterns.Any(pattern => Regex.IsMatch(content, pattern));
        }

        /// <summary>
        /// Detect WPF control conflicts
        /// </summary>
        private bool DetectWpfControlConflicts(string content)
        {
            // Check if file has both WPF and potential WinForms imports
            bool hasWpfControls = content.Contains("using System.Windows.Controls;");
            bool hasWinForms = content.Contains("System.Windows.Forms");
            
            // Look for unqualified control usage that could be ambiguous
            if (hasWpfControls && hasWinForms)
            {
                var ambiguousPatterns = new[]
                {
                    @"\bnew\s+(TextBox|ComboBox|CheckBox|Button|ListBox|Label)\s*\(",
                    @"\b(TextBox|ComboBox|CheckBox|Button|ListBox|Label)\s+\w+\s*[=;]"
                };

                return ambiguousPatterns.Any(pattern => Regex.IsMatch(content, pattern));
            }

            return false;
        }

        /// <summary>
        /// Resolve WinForms dialog conflicts
        /// </summary>
        private string ResolveWinFormsDialogConflicts(string content, HashSet<string> aliasesToAdd)
        {
            var modifiedContent = content;

            // Replace FolderBrowserDialog
            if (Regex.IsMatch(modifiedContent, @"new\s+System\.Windows\.Forms\.FolderBrowserDialog\s*\("))
            {
                modifiedContent = Regex.Replace(modifiedContent, 
                    @"new\s+System\.Windows\.Forms\.FolderBrowserDialog\s*\(",
                    "new WinFormsFolderBrowserDialog(");
                aliasesToAdd.Add(_winFormsControlAliases["FolderBrowserDialog"]);
            }

            // Replace DialogResult
            if (Regex.IsMatch(modifiedContent, @"System\.Windows\.Forms\.DialogResult\."))
            {
                modifiedContent = Regex.Replace(modifiedContent,
                    @"System\.Windows\.Forms\.DialogResult\.",
                    "WinFormsDialogResult.");
                aliasesToAdd.Add(_winFormsControlAliases["DialogResult"]);
            }

            // Replace OpenFileDialog
            if (Regex.IsMatch(modifiedContent, @"new\s+System\.Windows\.Forms\.OpenFileDialog\s*\("))
            {
                modifiedContent = Regex.Replace(modifiedContent,
                    @"new\s+System\.Windows\.Forms\.OpenFileDialog\s*\(",
                    "new WinFormsOpenFileDialog(");
                aliasesToAdd.Add(_winFormsControlAliases["OpenFileDialog"]);
            }

            // Replace SaveFileDialog
            if (Regex.IsMatch(modifiedContent, @"new\s+System\.Windows\.Forms\.SaveFileDialog\s*\("))
            {
                modifiedContent = Regex.Replace(modifiedContent,
                    @"new\s+System\.Windows\.Forms\.SaveFileDialog\s*\(",
                    "new WinFormsSaveFileDialog(");
                aliasesToAdd.Add(_winFormsControlAliases["SaveFileDialog"]);
            }

            return modifiedContent;
        }

        /// <summary>
        /// Resolve WPF control conflicts
        /// </summary>
        private string ResolveWpfControlConflicts(string content, HashSet<string> aliasesToAdd)
        {
            var modifiedContent = content;

            // Replace ambiguous WPF control instantiations
            foreach (var controlType in _wpfControlAliases.Keys)
            {
                var pattern = $@"\bnew\s+{controlType}\s*\(";
                if (Regex.IsMatch(modifiedContent, pattern))
                {
                    modifiedContent = Regex.Replace(modifiedContent, pattern, $"new Wpf{controlType}(");
                    aliasesToAdd.Add(_wpfControlAliases[controlType]);
                }

                // Replace control type declarations
                var typePattern = $@"\b{controlType}\s+(\w+)\s*[=;]";
                if (Regex.IsMatch(modifiedContent, typePattern))
                {
                    modifiedContent = Regex.Replace(modifiedContent, typePattern, $"Wpf{controlType} $1$2");
                    aliasesToAdd.Add(_wpfControlAliases[controlType]);
                }
            }

            return modifiedContent;
        }

        /// <summary>
        /// Add aliases to the top of the file
        /// </summary>
        private string AddAliasesToFile(string content, HashSet<string> aliases)
        {
            if (!aliases.Any()) return content;

            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            
            // Find the position to insert aliases (after existing using statements)
            int insertPosition = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith("using ") && !line.Contains("="))
                {
                    insertPosition = i + 1;
                }
                else if (line.StartsWith("namespace ") || line.StartsWith("public ") || line.StartsWith("internal "))
                {
                    break;
                }
            }

            // Insert aliases
            var sortedAliases = aliases.OrderBy(a => a).ToList();
            for (int i = 0; i < sortedAliases.Count; i++)
            {
                lines.Insert(insertPosition + i, sortedAliases[i]);
            }

            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Scan project for UI control conflicts
        /// </summary>
        public List<string> ScanForConflicts(string projectPath)
        {
            var conflictFiles = new List<string>();

            try
            {
                var csFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))
                    .ToList();

                foreach (var file in csFiles)
                {
                    var content = File.ReadAllText(file);
                    
                    if (DetectWinFormsDialogConflicts(content) || DetectWpfControlConflicts(content))
                    {
                        conflictFiles.Add(file);
                    }
                }

                _logger.LogInfo($"Found UI control conflicts in {conflictFiles.Count} files");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning for UI control conflicts");
            }

            return conflictFiles;
        }

        /// <summary>
        /// Generate conflict report
        /// </summary>
        public string GenerateConflictReport(string projectPath)
        {
            var conflictFiles = ScanForConflicts(projectPath);
            var report = new System.Text.StringBuilder();

            report.AppendLine("UI Control Conflict Report");
            report.AppendLine("========================");
            report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Project Path: {projectPath}");
            report.AppendLine();

            if (!conflictFiles.Any())
            {
                report.AppendLine("No UI control conflicts detected.");
                return report.ToString();
            }

            report.AppendLine($"Files with UI control conflicts: {conflictFiles.Count}");
            report.AppendLine();

            foreach (var file in conflictFiles)
            {
                report.AppendLine($"File: {Path.GetRelativePath(projectPath, file)}");
                
                var content = File.ReadAllText(file);
                
                // Analyze specific conflicts
                if (DetectWinFormsDialogConflicts(content))
                {
                    report.AppendLine("  - WinForms dialog conflicts detected");
                }
                
                if (DetectWpfControlConflicts(content))
                {
                    report.AppendLine("  - WPF control conflicts detected");
                }
                
                report.AppendLine();
            }

            return report.ToString();
        }
    }
}