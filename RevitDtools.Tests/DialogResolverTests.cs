using Microsoft.VisualStudio.TestTools.UnitTesting;
using RevitDtools.Core.Services;
using System.IO;
using System.Linq;

namespace RevitDtools.Tests
{
    [TestClass]
    public class DialogResolverTests
    {
        private DialogResolver _resolver;
        private string _testFilePath;

        [TestInitialize]
        public void Setup()
        {
            _resolver = new DialogResolver();
            _testFilePath = Path.GetTempFileName();
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(_testFilePath))
            {
                File.Delete(_testFilePath);
            }
        }

        [TestMethod]
        public void ResolveDialogConflicts_WithWinFormsOpenFileDialog_AppliesAlias()
        {
            // Arrange
            var testCode = @"using System;
using System.Windows.Forms;
using System.Windows;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                // Process file
            }
        }
    }
}";
            File.WriteAllText(_testFilePath, testCode);

            // Act
            var result = _resolver.ResolveDialogConflicts(_testFilePath);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.ChangesApplied);
            Assert.IsTrue(result.WinFormsDialogsResolved.Contains("OpenFileDialog"));
            Assert.IsTrue(result.WinFormsDialogsResolved.Contains("DialogResult"));
            Assert.IsTrue(result.AliasesAdded.Any(a => a.Contains("WinFormsOpenFileDialog")));
            Assert.IsTrue(result.AliasesAdded.Any(a => a.Contains("WinFormsDialogResult")));

            var modifiedContent = File.ReadAllText(_testFilePath);
            Assert.IsTrue(modifiedContent.Contains("using WinFormsOpenFileDialog = System.Windows.Forms.OpenFileDialog;"));
            Assert.IsTrue(modifiedContent.Contains("using WinFormsDialogResult = System.Windows.Forms.DialogResult;"));
            Assert.IsTrue(modifiedContent.Contains("new WinFormsOpenFileDialog()"));
            Assert.IsTrue(modifiedContent.Contains("WinFormsDialogResult.OK"));
        }

        [TestMethod]
        public void ResolveDialogConflicts_WithWinFormsSaveFileDialog_AppliesAlias()
        {
            // Arrange
            var testCode = @"using System;
using System.Windows.Forms;
using System.Windows;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = ""Text files (*.txt)|*.txt"";
            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                // Save file
            }
        }
    }
}";
            File.WriteAllText(_testFilePath, testCode);

            // Act
            var result = _resolver.ResolveDialogConflicts(_testFilePath);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.ChangesApplied);
            Assert.IsTrue(result.WinFormsDialogsResolved.Contains("SaveFileDialog"));
            Assert.IsTrue(result.AliasesAdded.Any(a => a.Contains("WinFormsSaveFileDialog")));

            var modifiedContent = File.ReadAllText(_testFilePath);
            Assert.IsTrue(modifiedContent.Contains("using WinFormsSaveFileDialog = System.Windows.Forms.SaveFileDialog;"));
            Assert.IsTrue(modifiedContent.Contains("WinFormsSaveFileDialog saveDialog = new WinFormsSaveFileDialog()"));
        }

        [TestMethod]
        public void ResolveDialogConflicts_WithWinFormsFolderBrowserDialog_AppliesAlias()
        {
            // Arrange
            var testCode = @"using System;
using System.Windows.Forms;
using System.Windows;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            var folderDialog = new FolderBrowserDialog();
            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                var selectedPath = folderDialog.SelectedPath;
            }
        }
    }
}";
            File.WriteAllText(_testFilePath, testCode);

            // Act
            var result = _resolver.ResolveDialogConflicts(_testFilePath);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.ChangesApplied);
            Assert.IsTrue(result.WinFormsDialogsResolved.Contains("FolderBrowserDialog"));
            Assert.IsTrue(result.AliasesAdded.Any(a => a.Contains("WinFormsFolderBrowserDialog")));

            var modifiedContent = File.ReadAllText(_testFilePath);
            Assert.IsTrue(modifiedContent.Contains("using WinFormsFolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;"));
            Assert.IsTrue(modifiedContent.Contains("new WinFormsFolderBrowserDialog()"));
        }

        [TestMethod]
        public void ResolveDialogConflicts_WithWpfDialogs_AppliesAlias()
        {
            // Arrange
            var testCode = @"using System;
using Microsoft.Win32;
using System.Windows.Forms;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            var openDialog = new Microsoft.Win32.OpenFileDialog();
            var saveDialog = new Microsoft.Win32.SaveFileDialog();
            
            if (openDialog.ShowDialog() == true)
            {
                // Process file
            }
        }
    }
}";
            File.WriteAllText(_testFilePath, testCode);

            // Act
            var result = _resolver.ResolveDialogConflicts(_testFilePath);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.ChangesApplied);
            Assert.IsTrue(result.WpfDialogsResolved.Contains("OpenFileDialog"));
            Assert.IsTrue(result.WpfDialogsResolved.Contains("SaveFileDialog"));
            Assert.IsTrue(result.AliasesAdded.Any(a => a.Contains("WpfOpenFileDialog")));
            Assert.IsTrue(result.AliasesAdded.Any(a => a.Contains("WpfSaveFileDialog")));

            var modifiedContent = File.ReadAllText(_testFilePath);
            Assert.IsTrue(modifiedContent.Contains("using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;"));
            Assert.IsTrue(modifiedContent.Contains("using WpfSaveFileDialog = Microsoft.Win32.SaveFileDialog;"));
            Assert.IsTrue(modifiedContent.Contains("new WpfOpenFileDialog()"));
            Assert.IsTrue(modifiedContent.Contains("new WpfSaveFileDialog()"));
        }

        [TestMethod]
        public void ResolveDialogConflicts_WithNoConflicts_ReturnsSuccessWithoutChanges()
        {
            // Arrange
            var testCode = @"using System;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            Console.WriteLine(""No dialogs here"");
        }
    }
}";
            File.WriteAllText(_testFilePath, testCode);

            // Act
            var result = _resolver.ResolveDialogConflicts(_testFilePath);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsFalse(result.ChangesApplied);
            Assert.AreEqual(0, result.AliasesAdded.Count);
            Assert.AreEqual(0, result.WinFormsDialogsResolved.Count);
            Assert.AreEqual(0, result.WpfDialogsResolved.Count);
        }

        [TestMethod]
        public void ResolveDialogConflicts_WithExistingAliases_DoesNotDuplicate()
        {
            // Arrange
            var testCode = @"using System;
using System.Windows.Forms;
using System.Windows;
using WinFormsOpenFileDialog = System.Windows.Forms.OpenFileDialog;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            var dialog = new WinFormsOpenFileDialog();
        }
    }
}";
            File.WriteAllText(_testFilePath, testCode);

            // Act
            var result = _resolver.ResolveDialogConflicts(_testFilePath);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsFalse(result.ChangesApplied);
            Assert.AreEqual(0, result.AliasesAdded.Count);
        }

        [TestMethod]
        public void ResolveDialogConflicts_WithNonExistentFile_ReturnsFailure()
        {
            // Arrange
            var nonExistentPath = "non_existent_file.cs";

            // Act
            var result = _resolver.ResolveDialogConflicts(nonExistentPath);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.AreEqual("File does not exist", result.ErrorMessage);
        }

        [TestMethod]
        public void ScanForDialogConflicts_WithConflictingFiles_ReturnsConflictInfo()
        {
            // Arrange
            var tempDir = Path.GetTempPath();
            var testFile1 = Path.Combine(tempDir, "test1.cs");
            var testFile2 = Path.Combine(tempDir, "test2.cs");

            var conflictingCode = @"using System.Windows.Forms;
using System.Windows;
public class Test { 
    void Method() { 
        var dialog = new OpenFileDialog(); 
    } 
}";

            var nonConflictingCode = @"using System;
public class Test { 
    void Method() { 
        Console.WriteLine(""Hello""); 
    } 
}";

            File.WriteAllText(testFile1, conflictingCode);
            File.WriteAllText(testFile2, nonConflictingCode);

            try
            {
                // Act
                var conflicts = _resolver.ScanForDialogConflicts(tempDir, "test*.cs");

                // Assert
                Assert.AreEqual(1, conflicts.Count);
                Assert.AreEqual(testFile1, conflicts[0].FilePath);
                Assert.IsTrue(conflicts[0].WinFormsDialogConflicts.Contains("OpenFileDialog"));
            }
            finally
            {
                // Cleanup
                if (File.Exists(testFile1)) File.Delete(testFile1);
                if (File.Exists(testFile2)) File.Delete(testFile2);
            }
        }

        [TestMethod]
        public void ResolveDialogConflicts_WithMixedDialogTypes_ResolvesAll()
        {
            // Arrange
            var testCode = @"using System;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Windows;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            // WinForms dialogs
            var winFormsOpen = new OpenFileDialog();
            var winFormsSave = new SaveFileDialog();
            var folderBrowser = new FolderBrowserDialog();
            
            // WPF dialogs
            var wpfOpen = new Microsoft.Win32.OpenFileDialog();
            var wpfSave = new Microsoft.Win32.SaveFileDialog();
            
            if (winFormsOpen.ShowDialog() == DialogResult.OK)
            {
                // Process
            }
            
            if (wpfOpen.ShowDialog() == true)
            {
                // Process
            }
        }
    }
}";
            File.WriteAllText(_testFilePath, testCode);

            // Act
            var result = _resolver.ResolveDialogConflicts(_testFilePath);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.ChangesApplied);
            
            // Check WinForms resolutions
            Assert.IsTrue(result.WinFormsDialogsResolved.Contains("OpenFileDialog"));
            Assert.IsTrue(result.WinFormsDialogsResolved.Contains("SaveFileDialog"));
            Assert.IsTrue(result.WinFormsDialogsResolved.Contains("FolderBrowserDialog"));
            Assert.IsTrue(result.WinFormsDialogsResolved.Contains("DialogResult"));
            
            // Check WPF resolutions
            Assert.IsTrue(result.WpfDialogsResolved.Contains("OpenFileDialog"));
            Assert.IsTrue(result.WpfDialogsResolved.Contains("SaveFileDialog"));

            var modifiedContent = File.ReadAllText(_testFilePath);
            
            // Verify WinForms aliases
            Assert.IsTrue(modifiedContent.Contains("using WinFormsOpenFileDialog = System.Windows.Forms.OpenFileDialog;"));
            Assert.IsTrue(modifiedContent.Contains("using WinFormsSaveFileDialog = System.Windows.Forms.SaveFileDialog;"));
            Assert.IsTrue(modifiedContent.Contains("using WinFormsFolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;"));
            Assert.IsTrue(modifiedContent.Contains("using WinFormsDialogResult = System.Windows.Forms.DialogResult;"));
            
            // Verify WPF aliases
            Assert.IsTrue(modifiedContent.Contains("using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;"));
            Assert.IsTrue(modifiedContent.Contains("using WpfSaveFileDialog = Microsoft.Win32.SaveFileDialog;"));
            
            // Verify replacements
            Assert.IsTrue(modifiedContent.Contains("new WinFormsOpenFileDialog()"));
            Assert.IsTrue(modifiedContent.Contains("new WinFormsSaveFileDialog()"));
            Assert.IsTrue(modifiedContent.Contains("new WinFormsFolderBrowserDialog()"));
            Assert.IsTrue(modifiedContent.Contains("new WpfOpenFileDialog()"));
            Assert.IsTrue(modifiedContent.Contains("new WpfSaveFileDialog()"));
            Assert.IsTrue(modifiedContent.Contains("WinFormsDialogResult.OK"));
        }
    }
}