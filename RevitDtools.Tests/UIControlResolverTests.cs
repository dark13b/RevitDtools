using Microsoft.VisualStudio.TestTools.UnitTesting;
using RevitDtools.Core.Services;
using RevitDtools.Utilities;
using System;
using System.IO;
using System.Linq;

namespace RevitDtools.Tests
{
    [TestClass]
    public class UIControlResolverTests
    {
        private UIControlResolver _resolver;
        private TestLogger _logger;
        private string _testDirectory;

        [TestInitialize]
        public void Setup()
        {
            _logger = new TestLogger();
            _resolver = new UIControlResolver(_logger);
            _testDirectory = Path.Combine(Path.GetTempPath(), "UIControlResolverTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [TestMethod]
        public void ResolveConflictsInFile_WithWinFormsDialogConflicts_ResolvesCorrectly()
        {
            // Arrange
            var testCode = @"using System;
using System.Windows;
using Microsoft.Win32;

namespace Test
{
    public class TestClass
    {
        public void TestMethod()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // Do something
            }
        }
    }
}";

            var testFile = Path.Combine(_testDirectory, "TestFile.cs");
            File.WriteAllText(testFile, testCode);

            // Act
            var result = _resolver.ResolveConflictsInFile(testFile);

            // Assert
            Assert.IsTrue(result);
            
            var modifiedContent = File.ReadAllText(testFile);
            Assert.IsTrue(modifiedContent.Contains("using WinFormsFolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;"));
            Assert.IsTrue(modifiedContent.Contains("using WinFormsDialogResult = System.Windows.Forms.DialogResult;"));
            Assert.IsTrue(modifiedContent.Contains("new WinFormsFolderBrowserDialog()"));
            Assert.IsTrue(modifiedContent.Contains("WinFormsDialogResult.OK"));
        }

        [TestMethod]
        public void ResolveConflictsInFile_WithWpfControlConflicts_ResolvesCorrectly()
        {
            // Arrange
            var testCode = @"using System;
using System.Windows;
using System.Windows.Controls;

namespace Test
{
    public class TestClass
    {
        private TextBox _textBox;
        private ComboBox _comboBox;
        
        public void TestMethod()
        {
            var button = new Button();
            var checkBox = new CheckBox();
        }
    }
}";

            var testFile = Path.Combine(_testDirectory, "TestFile.cs");
            File.WriteAllText(testFile, testCode);

            // Act
            var result = _resolver.ResolveConflictsInFile(testFile);

            // Assert - Since there's no WinForms conflict, WPF controls should not be aliased
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ResolveConflictsInFile_WithMixedConflicts_ResolvesCorrectly()
        {
            // Arrange
            var testCode = @"using System;
using System.Windows;
using System.Windows.Controls;

namespace Test
{
    public class TestClass
    {
        private TextBox _textBox;
        
        public void TestMethod()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            var button = new Button();
        }
    }
}";

            var testFile = Path.Combine(_testDirectory, "TestFile.cs");
            File.WriteAllText(testFile, testCode);

            // Act
            var result = _resolver.ResolveConflictsInFile(testFile);

            // Assert
            Assert.IsTrue(result);
            
            var modifiedContent = File.ReadAllText(testFile);
            Assert.IsTrue(modifiedContent.Contains("using WinFormsFolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;"));
            Assert.IsTrue(modifiedContent.Contains("new WinFormsFolderBrowserDialog()"));
            
            // WPF controls should be aliased when WinForms is also present
            Assert.IsTrue(modifiedContent.Contains("using WpfButton = System.Windows.Controls.Button;"));
            Assert.IsTrue(modifiedContent.Contains("using WpfTextBox = System.Windows.Controls.TextBox;"));
            Assert.IsTrue(modifiedContent.Contains("new WpfButton()"));
        }

        [TestMethod]
        public void ResolveConflictsInFile_WithNoConflicts_ReturnsFalse()
        {
            // Arrange
            var testCode = @"using System;
using Autodesk.Revit.DB;

namespace Test
{
    public class TestClass
    {
        public void TestMethod()
        {
            // No UI controls here
        }
    }
}";

            var testFile = Path.Combine(_testDirectory, "TestFile.cs");
            File.WriteAllText(testFile, testCode);

            // Act
            var result = _resolver.ResolveConflictsInFile(testFile);

            // Assert
            Assert.IsFalse(result);
            
            var modifiedContent = File.ReadAllText(testFile);
            Assert.AreEqual(testCode, modifiedContent);
        }

        [TestMethod]
        public void ResolveConflictsInFile_WithNonExistentFile_ReturnsFalse()
        {
            // Arrange
            var nonExistentFile = Path.Combine(_testDirectory, "NonExistent.cs");

            // Act
            var result = _resolver.ResolveConflictsInFile(nonExistentFile);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ScanForConflicts_WithMultipleFiles_ReturnsCorrectFiles()
        {
            // Arrange
            var conflictFile = Path.Combine(_testDirectory, "ConflictFile.cs");
            var conflictCode = @"using System.Windows.Controls;
namespace Test {
    public class Test {
        public void Method() {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
        }
    }
}";
            File.WriteAllText(conflictFile, conflictCode);

            var cleanFile = Path.Combine(_testDirectory, "CleanFile.cs");
            var cleanCode = @"using System;
namespace Test {
    public class Test {
        public void Method() {
            // No conflicts
        }
    }
}";
            File.WriteAllText(cleanFile, cleanCode);

            // Act
            var conflictFiles = _resolver.ScanForConflicts(_testDirectory);

            // Assert
            Assert.AreEqual(1, conflictFiles.Count);
            Assert.IsTrue(conflictFiles.Any(f => f.EndsWith("ConflictFile.cs")));
        }

        [TestMethod]
        public void GenerateConflictReport_WithConflicts_GeneratesCorrectReport()
        {
            // Arrange
            var conflictFile = Path.Combine(_testDirectory, "ConflictFile.cs");
            var conflictCode = @"using System.Windows.Controls;
namespace Test {
    public class Test {
        public void Method() {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
        }
    }
}";
            File.WriteAllText(conflictFile, conflictCode);

            // Act
            var report = _resolver.GenerateConflictReport(_testDirectory);

            // Assert
            Assert.IsTrue(report.Contains("UI Control Conflict Report"));
            Assert.IsTrue(report.Contains("Files with UI control conflicts: 1"));
            Assert.IsTrue(report.Contains("ConflictFile.cs"));
            Assert.IsTrue(report.Contains("WinForms dialog conflicts detected"));
        }

        [TestMethod]
        public void ResolveConflictsInFiles_WithMultipleFiles_ResolvesCorrectly()
        {
            // Arrange
            var files = new[]
            {
                Path.Combine(_testDirectory, "File1.cs"),
                Path.Combine(_testDirectory, "File2.cs")
            };

            var conflictCode = @"using System.Windows.Controls;
namespace Test {
    public class Test {
        public void Method() {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
        }
    }
}";

            foreach (var file in files)
            {
                File.WriteAllText(file, conflictCode);
            }

            // Act
            var resolvedCount = _resolver.ResolveConflictsInFiles(files);

            // Assert
            Assert.AreEqual(2, resolvedCount);
            
            foreach (var file in files)
            {
                var content = File.ReadAllText(file);
                Assert.IsTrue(content.Contains("using WinFormsFolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;"));
            }
        }
    }
}