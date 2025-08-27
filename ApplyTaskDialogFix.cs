using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

public class TaskDialogFixer
{
    private const string REVIT_TASK_DIALOG_ALIAS = "using RevitTaskDialog = Autodesk.Revit.UI.TaskDialog;";
    
    public static void Main()
    {
        Console.WriteLine("Applying TaskDialog fixes...");
        
        var filesToProcess = new[]
        {
            "DtoolsCommands.cs",
            "Core/Commands/SettingsCommand.cs",
            "Core/Commands/EnhancedDwgToDetailLineCommand.cs",
            "Core/Commands/HelpCommand.cs",
            "Core/Commands/EnhancedColumnByLineCommand.cs",
            "Core/Commands/CustomShapeColumnCommand.cs",
            "Core/Commands/ColumnGridCommand.cs"
        };

        foreach (var filePath in filesToProcess)
        {
            if (File.Exists(filePath))
            {
                ProcessFile(filePath);
                Console.WriteLine($"Processed: {filePath}");
            }
            else
            {
                Console.WriteLine($"File not found: {filePath}");
            }
        }
        
        Console.WriteLine("TaskDialog fixes applied!");
    }

    private static void ProcessFile(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var originalContent = content;

        // Check if file contains TaskDialog usage
        if (!ContainsTaskDialogUsage(content))
        {
            return;
        }

        // Add alias if not already present
        if (!content.Contains("using RevitTaskDialog ="))
        {
            content = AddRevitTaskDialogAlias(content);
        }

        // Replace TaskDialog references with RevitTaskDialog
        content = ReplaceTaskDialogReferences(content);

        // Only write if content changed
        if (content != originalContent)
        {
            File.WriteAllText(filePath, content);
        }
    }

    private static bool ContainsTaskDialogUsage(string content)
    {
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

    private static string AddRevitTaskDialogAlias(string content)
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

    private static string ReplaceTaskDialogReferences(string content)
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
}