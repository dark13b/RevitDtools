using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace RevitDtools.Core.Services
{
    /// <summary>
    /// Resolves MessageBox namespace conflicts by applying WpfMessageBox aliases
    /// </summary>
    public class MessageBoxResolver
    {
        private const string WpfMessageBoxAlias = "using WpfMessageBox = System.Windows.MessageBox;";
        private const string WinFormsMessageBoxAlias = "using WinFormsMessageBox = System.Windows.Forms.MessageBox;";
        
        /// <summary>
        /// Scans the project for MessageBox conflicts
        /// </summary>
        public List<MessageBoxConflict> DetectConflicts(string projectPath)
        {
            var conflicts = new List<MessageBoxConflict>();
            var csFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))
                .ToList();

            foreach (var filePath in csFiles)
            {
                try
                {
                    var content = File.ReadAllText(filePath);
                    var fileConflicts = DetectConflictsInFile(filePath, content);
                    conflicts.AddRange(fileConflicts);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error scanning file {filePath}: {ex.Message}");
                }
            }

            return conflicts;
        }

        /// <summary>
        /// Detects MessageBox conflicts in a single file
        /// </summary>
        private List<MessageBoxConflict> DetectConflictsInFile(string filePath, string content)
        {
            var conflicts = new List<MessageBoxConflict>();
            var lines = content.Split('\n');

            // Check if file has System.Windows using statement
            bool hasSystemWindows = content.Contains("using System.Windows;") || 
                                   content.Contains("using System.Windows.Controls;");
            
            // Check if file has System.Windows.Forms using statement
            bool hasWinForms = content.Contains("using System.Windows.Forms;");

            // Look for MessageBox usage patterns
            var messageBoxPattern = @"\bMessageBox\b";
            var matches = Regex.Matches(content, messageBoxPattern);

            foreach (Match match in matches)
            {
                var lineNumber = GetLineNumber(content, match.Index);
                var line = lines[lineNumber - 1].Trim();

                // Skip if it's already fully qualified
                if (line.Contains("System.Windows.MessageBox") || 
                    line.Contains("System.Windows.Forms.MessageBox") ||
                    line.Contains("WpfMessageBox") ||
                    line.Contains("WinFormsMessageBox"))
                {
                    continue;
                }

                // Check if this is a potential conflict
                if (hasSystemWindows || hasWinForms)
                {
                    conflicts.Add(new MessageBoxConflict
                    {
                        FilePath = filePath,
                        LineNumber = lineNumber,
                        LineContent = line,
                        HasSystemWindows = hasSystemWindows,
                        HasWinForms = hasWinForms,
                        ConflictType = DetermineConflictType(line, hasSystemWindows, hasWinForms)
                    });
                }
            }

            return conflicts;
        }

        /// <summary>
        /// Determines the type of MessageBox conflict
        /// </summary>
        private MessageBoxConflictType DetermineConflictType(string line, bool hasSystemWindows, bool hasWinForms)
        {
            // For now, default to WPF MessageBox as it's more common in WPF applications
            // This can be enhanced with more sophisticated analysis
            if (hasSystemWindows && !hasWinForms)
                return MessageBoxConflictType.WpfOnly;
            
            if (hasWinForms && !hasSystemWindows)
                return MessageBoxConflictType.WinFormsOnly;
            
            if (hasSystemWindows && hasWinForms)
                return MessageBoxConflictType.Ambiguous;

            return MessageBoxConflictType.WpfOnly; // Default to WPF
        }

        /// <summary>
        /// Resolves MessageBox conflicts in a file
        /// </summary>
        public MessageBoxResolutionResult ResolveConflicts(string filePath)
        {
            try
            {
                var originalContent = File.ReadAllText(filePath);
                var conflicts = DetectConflictsInFile(filePath, originalContent);
                
                if (!conflicts.Any())
                {
                    return new MessageBoxResolutionResult
                    {
                        FilePath = filePath,
                        Success = true,
                        ConflictsResolved = 0,
                        Message = "No MessageBox conflicts found"
                    };
                }

                var modifiedContent = ApplyMessageBoxAliases(originalContent, conflicts);
                
                // Create backup
                var backupPath = filePath + ".backup";
                File.WriteAllText(backupPath, originalContent);
                
                // Write modified content
                File.WriteAllText(filePath, modifiedContent);

                return new MessageBoxResolutionResult
                {
                    FilePath = filePath,
                    Success = true,
                    ConflictsResolved = conflicts.Count,
                    Message = $"Resolved {conflicts.Count} MessageBox conflicts",
                    BackupPath = backupPath
                };
            }
            catch (Exception ex)
            {
                return new MessageBoxResolutionResult
                {
                    FilePath = filePath,
                    Success = false,
                    ConflictsResolved = 0,
                    Message = $"Error resolving conflicts: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Applies MessageBox aliases to resolve conflicts
        /// </summary>
        private string ApplyMessageBoxAliases(string content, List<MessageBoxConflict> conflicts)
        {
            var modifiedContent = content;
            var aliasesToAdd = new HashSet<string>();

            // Determine which aliases to add based on conflict types
            foreach (var conflict in conflicts)
            {
                switch (conflict.ConflictType)
                {
                    case MessageBoxConflictType.WpfOnly:
                    case MessageBoxConflictType.Ambiguous:
                        aliasesToAdd.Add(WpfMessageBoxAlias);
                        break;
                    case MessageBoxConflictType.WinFormsOnly:
                        aliasesToAdd.Add(WinFormsMessageBoxAlias);
                        break;
                }
            }

            // Add WPF alias by default for most cases
            if (conflicts.Any())
            {
                aliasesToAdd.Add(WpfMessageBoxAlias);
            }

            // Replace MessageBox references with aliased versions
            modifiedContent = ReplaceMessageBoxReferences(modifiedContent);

            // Add aliases to the file
            modifiedContent = AddAliasesToFile(modifiedContent, aliasesToAdd);

            return modifiedContent;
        }

        /// <summary>
        /// Replaces MessageBox references with WpfMessageBox
        /// </summary>
        private string ReplaceMessageBoxReferences(string content)
        {
            // Replace MessageBox.Show calls (but not fully qualified ones)
            content = Regex.Replace(content, 
                @"(?<!System\.Windows\.)(?<!System\.Windows\.Forms\.)(?<!Wpf)(?<!WinForms)\bMessageBox\.Show\b", 
                "WpfMessageBox.Show");

            // Replace MessageBox type references (but not fully qualified ones)
            content = Regex.Replace(content, 
                @"(?<!System\.Windows\.)(?<!System\.Windows\.Forms\.)(?<!Wpf)(?<!WinForms)\bMessageBox\s+(\w+)", 
                "WpfMessageBox $1");

            // Replace MessageBox in method parameters
            content = Regex.Replace(content, 
                @"(?<!System\.Windows\.)(?<!System\.Windows\.Forms\.)(?<!Wpf)(?<!WinForms)\(MessageBox\s", 
                "(WpfMessageBox ");

            return content;
        }

        /// <summary>
        /// Adds using aliases to the top of the file
        /// </summary>
        private string AddAliasesToFile(string content, HashSet<string> aliases)
        {
            if (!aliases.Any()) return content;

            var lines = content.Split('\n').ToList();
            var insertIndex = FindUsingInsertionPoint(lines);

            // Insert aliases at the appropriate location
            foreach (var alias in aliases.OrderBy(a => a))
            {
                // Check if alias already exists
                if (!content.Contains(alias))
                {
                    lines.Insert(insertIndex, alias);
                    insertIndex++;
                }
            }

            return string.Join("\n", lines);
        }

        /// <summary>
        /// Finds the appropriate location to insert using statements
        /// </summary>
        private int FindUsingInsertionPoint(List<string> lines)
        {
            int lastUsingIndex = -1;
            
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith("using ") && !line.Contains("="))
                {
                    lastUsingIndex = i;
                }
                else if (line.StartsWith("namespace ") || 
                         line.StartsWith("public class ") || 
                         line.StartsWith("internal class "))
                {
                    break;
                }
            }

            return lastUsingIndex + 1;
        }

        /// <summary>
        /// Gets the line number for a character index in the content
        /// </summary>
        private int GetLineNumber(string content, int charIndex)
        {
            return content.Substring(0, charIndex).Count(c => c == '\n') + 1;
        }

        /// <summary>
        /// Resolves MessageBox conflicts in all files in the project
        /// </summary>
        public List<MessageBoxResolutionResult> ResolveAllConflicts(string projectPath)
        {
            var results = new List<MessageBoxResolutionResult>();
            var conflicts = DetectConflicts(projectPath);
            
            var fileGroups = conflicts.GroupBy(c => c.FilePath);
            
            foreach (var fileGroup in fileGroups)
            {
                var result = ResolveConflicts(fileGroup.Key);
                results.Add(result);
            }

            return results;
        }
    }

    /// <summary>
    /// Represents a MessageBox namespace conflict
    /// </summary>
    public class MessageBoxConflict
    {
        public string FilePath { get; set; }
        public int LineNumber { get; set; }
        public string LineContent { get; set; }
        public bool HasSystemWindows { get; set; }
        public bool HasWinForms { get; set; }
        public MessageBoxConflictType ConflictType { get; set; }
    }

    /// <summary>
    /// Types of MessageBox conflicts
    /// </summary>
    public enum MessageBoxConflictType
    {
        WpfOnly,
        WinFormsOnly,
        Ambiguous
    }

    /// <summary>
    /// Result of MessageBox conflict resolution
    /// </summary>
    public class MessageBoxResolutionResult
    {
        public string FilePath { get; set; }
        public bool Success { get; set; }
        public int ConflictsResolved { get; set; }
        public string Message { get; set; }
        public string BackupPath { get; set; }
    }
}