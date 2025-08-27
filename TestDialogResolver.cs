using System;
using System.IO;
using System.Linq;

namespace RevitDtools.Test
{
    /// <summary>
    /// Simple test program for DialogResolver
    /// </summary>
    public class TestDialogResolver
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== Testing DialogResolver ===");
            
            // Create a simple DialogResolver implementation for testing
            var resolver = new SimpleDialogResolver();
            
            var testFile = "TestDialogConflicts.cs";
            if (!File.Exists(testFile))
            {
                Console.WriteLine($"Test file {testFile} not found.");
                return;
            }
            
            Console.WriteLine($"Processing test file: {testFile}");
            
            var result = resolver.ResolveDialogConflicts(testFile);
            
            if (result.Success)
            {
                Console.WriteLine("✓ Dialog resolution completed successfully");
                
                if (result.ChangesApplied)
                {
                    Console.WriteLine($"✓ Changes applied to {testFile}");
                    Console.WriteLine($"Aliases added: {result.AliasesAdded.Count}");
                    Console.WriteLine($"WinForms dialogs resolved: {result.WinFormsDialogsResolved.Count}");
                    Console.WriteLine($"WPF dialogs resolved: {result.WpfDialogsResolved.Count}");
                    
                    Console.WriteLine("\nModified file content:");
                    Console.WriteLine(File.ReadAllText(testFile));
                }
                else
                {
                    Console.WriteLine("No changes needed - file already properly aliased");
                }
            }
            else
            {
                Console.WriteLine($"✗ Dialog resolution failed: {result.ErrorMessage}");
            }
        }
    }
    
    /// <summary>
    /// Simple implementation of DialogResolver for testing
    /// </summary>
    public class SimpleDialogResolver
    {
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
                
                var content = File.ReadAllText(filePath);
                var modifiedContent = content;
                var aliasesAdded = 0;
                
                // Check for WinForms dialog conflicts
                if (content.Contains("using System.Windows.Forms;") && 
                    (content.Contains("using Microsoft.Win32;") || content.Contains("using System.Windows;")))
                {
                    // Add WinForms aliases
                    if (content.Contains("new OpenFileDialog()") && !content.Contains("WinFormsOpenFileDialog"))
                    {
                        modifiedContent = "using WinFormsOpenFileDialog = System.Windows.Forms.OpenFileDialog;\n" + modifiedContent;
                        modifiedContent = modifiedContent.Replace("new OpenFileDialog()", "new WinFormsOpenFileDialog()");
                        result.WinFormsDialogsResolved.Add("OpenFileDialog");
                        result.AliasesAdded.Add("WinFormsOpenFileDialog");
                        aliasesAdded++;
                    }
                    
                    if (content.Contains("new SaveFileDialog()") && !content.Contains("WinFormsSaveFileDialog"))
                    {
                        modifiedContent = "using WinFormsSaveFileDialog = System.Windows.Forms.SaveFileDialog;\n" + modifiedContent;
                        modifiedContent = modifiedContent.Replace("new SaveFileDialog()", "new WinFormsSaveFileDialog()");
                        result.WinFormsDialogsResolved.Add("SaveFileDialog");
                        result.AliasesAdded.Add("WinFormsSaveFileDialog");
                        aliasesAdded++;
                    }
                    
                    if (content.Contains("new FolderBrowserDialog()") && !content.Contains("WinFormsFolderBrowserDialog"))
                    {
                        modifiedContent = "using WinFormsFolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;\n" + modifiedContent;
                        modifiedContent = modifiedContent.Replace("new FolderBrowserDialog()", "new WinFormsFolderBrowserDialog()");
                        result.WinFormsDialogsResolved.Add("FolderBrowserDialog");
                        result.AliasesAdded.Add("WinFormsFolderBrowserDialog");
                        aliasesAdded++;
                    }
                    
                    if (content.Contains("DialogResult.OK") && !content.Contains("WinFormsDialogResult"))
                    {
                        modifiedContent = "using WinFormsDialogResult = System.Windows.Forms.DialogResult;\n" + modifiedContent;
                        modifiedContent = modifiedContent.Replace("DialogResult.OK", "WinFormsDialogResult.OK");
                        result.WinFormsDialogsResolved.Add("DialogResult");
                        result.AliasesAdded.Add("WinFormsDialogResult");
                        aliasesAdded++;
                    }
                }
                
                // Check for WPF dialog conflicts
                if (content.Contains("using Microsoft.Win32;") && content.Contains("using System.Windows.Forms;"))
                {
                    if (content.Contains("new Microsoft.Win32.OpenFileDialog()") && !content.Contains("WpfOpenFileDialog"))
                    {
                        modifiedContent = "using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;\n" + modifiedContent;
                        modifiedContent = modifiedContent.Replace("new Microsoft.Win32.OpenFileDialog()", "new WpfOpenFileDialog()");
                        result.WpfDialogsResolved.Add("OpenFileDialog");
                        result.AliasesAdded.Add("WpfOpenFileDialog");
                        aliasesAdded++;
                    }
                    
                    if (content.Contains("new Microsoft.Win32.SaveFileDialog()") && !content.Contains("WpfSaveFileDialog"))
                    {
                        modifiedContent = "using WpfSaveFileDialog = Microsoft.Win32.SaveFileDialog;\n" + modifiedContent;
                        modifiedContent = modifiedContent.Replace("new Microsoft.Win32.SaveFileDialog()", "new WpfSaveFileDialog()");
                        result.WpfDialogsResolved.Add("SaveFileDialog");
                        result.AliasesAdded.Add("WpfSaveFileDialog");
                        aliasesAdded++;
                    }
                }
                
                if (aliasesAdded > 0)
                {
                    File.WriteAllText(filePath, modifiedContent);
                    result.ChangesApplied = true;
                }
                
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
            }
            
            return result;
        }
    }
    
    public class DialogResolutionResult
    {
        public string FilePath { get; set; } = string.Empty;
        public bool Success { get; set; }
        public bool ChangesApplied { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public System.Collections.Generic.List<string> AliasesAdded { get; set; } = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<string> WinFormsDialogsResolved { get; set; } = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<string> WpfDialogsResolved { get; set; } = new System.Collections.Generic.List<string>();
    }
}