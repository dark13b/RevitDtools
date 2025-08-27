using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RevitDtools.Core.Services;

namespace RevitDtools.Tests
{
    [TestClass]
    public class MessageBoxResolverTests
    {
        private MessageBoxResolver _resolver;
        private string _testDirectory;

        [TestInitialize]
        public void Setup()
        {
            _resolver = new MessageBoxResolver();
            _testDirectory = Path.Combine(Path.GetTempPath(), "MessageBoxResolverTests");
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
        public void DetectConflicts_ShouldFindMessageBoxConflicts()
        {
            // Arrange
            var testFile = Path.Combine(_testDirectory, "TestFile.cs");
            var content = @"using System;
using System.Windows;
using System.Windows.Forms;

namespace Test
{
    public class TestClass
    {
        public void TestMethod()
        {
            MessageBox.Show(""Test message"");
            var result = MessageBox.Show(""Confirm"", ""Title"", MessageBoxButton.YesNo);
        }
    }
}";
            File.WriteAllText(testFile, content);

            // Act
            var conflicts = _resolver.DetectConflicts(_testDirectory);

            // Assert
            Assert.IsTrue(conflicts.Count > 0, "Should detect MessageBox conflicts");
            Assert.IsTrue(conflicts.Any(c => c.LineContent.Contains("MessageBox.Show")), 
                "Should find MessageBox.Show calls");
        }

        [TestMethod]
        public void DetectConflicts_ShouldNotFindConflictsInFullyQualifiedCalls()
        {
            // Arrange
            var testFile = Path.Combine(_testDirectory, "TestFile.cs");
            var content = @"using System;

namespace Test
{
    public class TestClass
    {
        public void TestMethod()
        {
            System.Windows.MessageBox.Show(""Test message"");
            System.Windows.Forms.MessageBox.Show(""Another message"");
        }
    }
}";
            File.WriteAllText(testFile, content);

            // Act
            var conflicts = _resolver.DetectConflicts(_testDirectory);

            // Assert
            Assert.AreEqual(0, conflicts.Count, "Should not detect conflicts in fully qualified calls");
        }

        [TestMethod]
        public void ResolveConflicts_ShouldAddAliasAndUpdateCalls()
        {
            // Arrange
            var testFile = Path.Combine(_testDirectory, "TestFile.cs");
            var originalContent = @"using System;
using System.Windows;
using System.Windows.Forms;

namespace Test
{
    public class TestClass
    {
        public void TestMethod()
        {
            MessageBox.Show(""Test message"");
        }
    }
}";
            File.WriteAllText(testFile, originalContent);

            // Act
            var result = _resolver.ResolveConflicts(testFile);

            // Assert
            Assert.IsTrue(result.Success, "Resolution should succeed");
            Assert.IsTrue(result.ConflictsResolved > 0, "Should resolve conflicts");

            var modifiedContent = File.ReadAllText(testFile);
            Assert.IsTrue(modifiedContent.Contains("using WpfMessageBox = System.Windows.MessageBox;"), 
                "Should add WpfMessageBox alias");
            Assert.IsTrue(modifiedContent.Contains("WpfMessageBox.Show"), 
                "Should replace MessageBox.Show with WpfMessageBox.Show");
            Assert.IsFalse(modifiedContent.Contains("MessageBox.Show(\"Test message\")"), 
                "Should not contain unqualified MessageBox.Show calls");
        }

        [TestMethod]
        public void ResolveConflicts_ShouldCreateBackupFile()
        {
            // Arrange
            var testFile = Path.Combine(_testDirectory, "TestFile.cs");
            var originalContent = @"using System;
using System.Windows;

namespace Test
{
    public class TestClass
    {
        public void TestMethod()
        {
            MessageBox.Show(""Test message"");
        }
    }
}";
            File.WriteAllText(testFile, originalContent);

            // Act
            var result = _resolver.ResolveConflicts(testFile);

            // Assert
            Assert.IsTrue(result.Success, "Resolution should succeed");
            Assert.IsTrue(File.Exists(result.BackupPath), "Should create backup file");
            
            var backupContent = File.ReadAllText(result.BackupPath);
            Assert.AreEqual(originalContent, backupContent, "Backup should contain original content");
        }

        [TestMethod]
        public void ResolveConflicts_ShouldHandleFileWithNoConflicts()
        {
            // Arrange
            var testFile = Path.Combine(_testDirectory, "TestFile.cs");
            var content = @"using System;

namespace Test
{
    public class TestClass
    {
        public void TestMethod()
        {
            Console.WriteLine(""No MessageBox here"");
        }
    }
}";
            File.WriteAllText(testFile, content);

            // Act
            var result = _resolver.ResolveConflicts(testFile);

            // Assert
            Assert.IsTrue(result.Success, "Should succeed even with no conflicts");
            Assert.AreEqual(0, result.ConflictsResolved, "Should report zero conflicts resolved");
        }

        [TestMethod]
        public void ResolveAllConflicts_ShouldProcessMultipleFiles()
        {
            // Arrange
            var testFile1 = Path.Combine(_testDirectory, "TestFile1.cs");
            var testFile2 = Path.Combine(_testDirectory, "TestFile2.cs");
            
            var content1 = @"using System.Windows;
namespace Test { public class Test1 { public void Method() { MessageBox.Show(""Test1""); } } }";
            
            var content2 = @"using System.Windows;
namespace Test { public class Test2 { public void Method() { MessageBox.Show(""Test2""); } } }";

            File.WriteAllText(testFile1, content1);
            File.WriteAllText(testFile2, content2);

            // Act
            var results = _resolver.ResolveAllConflicts(_testDirectory);

            // Assert
            Assert.AreEqual(2, results.Count, "Should process both files");
            Assert.IsTrue(results.All(r => r.Success), "All resolutions should succeed");
            Assert.IsTrue(results.All(r => r.ConflictsResolved > 0), "All files should have conflicts resolved");
        }

        [TestMethod]
        public void MessageBoxConflictType_ShouldBeCorrectlyDetermined()
        {
            // Arrange
            var testFile = Path.Combine(_testDirectory, "TestFile.cs");
            var content = @"using System.Windows;
using System.Windows.Forms;

namespace Test
{
    public class TestClass
    {
        public void TestMethod()
        {
            MessageBox.Show(""Ambiguous call"");
        }
    }
}";
            File.WriteAllText(testFile, content);

            // Act
            var conflicts = _resolver.DetectConflicts(_testDirectory);

            // Assert
            Assert.IsTrue(conflicts.Count > 0, "Should detect conflicts");
            var conflict = conflicts.First();
            Assert.IsTrue(conflict.HasSystemWindows, "Should detect System.Windows using");
            Assert.IsTrue(conflict.HasWinForms, "Should detect System.Windows.Forms using");
            Assert.AreEqual(MessageBoxConflictType.Ambiguous, conflict.ConflictType, 
                "Should identify as ambiguous conflict");
        }
    }
}