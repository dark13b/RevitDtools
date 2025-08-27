using Microsoft.VisualStudio.TestTools.UnitTesting;
using RevitDtools.Core.Services;
using System.IO;
using System.Linq;

namespace RevitDtools.Tests
{
    [TestClass]
    public class ViewResolverTests
    {
        private ViewResolver _resolver;
        private string _testDirectory;

        [TestInitialize]
        public void Setup()
        {
            _resolver = new ViewResolver();
            _testDirectory = Path.Combine(Path.GetTempPath(), "ViewResolverTests");
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
        public void ProcessFile_WithViewVariableDeclaration_AddsAliasAndReplacesReference()
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
            View myView = GetView();
        }
    }
}";

            var testFile = Path.Combine(_testDirectory, "test.cs");
            File.WriteAllText(testFile, testCode);

            // Act
            var result = _resolver.ProcessFile(testFile);

            // Assert
            Assert.IsTrue(result);
            var modifiedContent = File.ReadAllText(testFile);
            Assert.IsTrue(modifiedContent.Contains("using RevitView = Autodesk.Revit.DB.View;"));
            Assert.IsTrue(modifiedContent.Contains("RevitView myView = GetView();"));
            Assert.IsFalse(modifiedContent.Contains("View myView = GetView();"));
        }

        [TestMethod]
        public void ProcessFile_WithViewMethodParameter_ReplacesParameter()
        {
            // Arrange
            var testCode = @"using System;
using Autodesk.Revit.DB;

namespace Test
{
    public class TestClass
    {
        public void ProcessView(View view)
        {
            // Process the view
        }
    }
}";

            var testFile = Path.Combine(_testDirectory, "test.cs");
            File.WriteAllText(testFile, testCode);

            // Act
            var result = _resolver.ProcessFile(testFile);

            // Assert
            Assert.IsTrue(result);
            var modifiedContent = File.ReadAllText(testFile);
            Assert.IsTrue(modifiedContent.Contains("using RevitView = Autodesk.Revit.DB.View;"));
            Assert.IsTrue(modifiedContent.Contains("public void ProcessView(RevitView view)"));
        }

        [TestMethod]
        public void ProcessFile_WithViewGeneric_ReplacesGeneric()
        {
            // Arrange
            var testCode = @"using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace Test
{
    public class TestClass
    {
        public List<View> GetViews()
        {
            return new List<View>();
        }
    }
}";

            var testFile = Path.Combine(_testDirectory, "test.cs");
            File.WriteAllText(testFile, testCode);

            // Act
            var result = _resolver.ProcessFile(testFile);

            // Assert
            Assert.IsTrue(result);
            var modifiedContent = File.ReadAllText(testFile);
            Assert.IsTrue(modifiedContent.Contains("using RevitView = Autodesk.Revit.DB.View;"));
            Assert.IsTrue(modifiedContent.Contains("public List<RevitView> GetViews()"));
            Assert.IsTrue(modifiedContent.Contains("return new List<RevitView>();"));
        }

        [TestMethod]
        public void ProcessFile_WithViewArray_ReplacesArray()
        {
            // Arrange
            var testCode = @"using System;
using Autodesk.Revit.DB;

namespace Test
{
    public class TestClass
    {
        public View[] GetViewArray()
        {
            return new View[10];
        }
    }
}";

            var testFile = Path.Combine(_testDirectory, "test.cs");
            File.WriteAllText(testFile, testCode);

            // Act
            var result = _resolver.ProcessFile(testFile);

            // Assert
            Assert.IsTrue(result);
            var modifiedContent = File.ReadAllText(testFile);
            Assert.IsTrue(modifiedContent.Contains("using RevitView = Autodesk.Revit.DB.View;"));
            Assert.IsTrue(modifiedContent.Contains("public RevitView[] GetViewArray()"));
            Assert.IsTrue(modifiedContent.Contains("return new RevitView[10];"));
        }

        [TestMethod]
        public void ProcessFile_WithViewCasting_ReplacesCasting()
        {
            // Arrange
            var testCode = @"using System;
using Autodesk.Revit.DB;

namespace Test
{
    public class TestClass
    {
        public void TestCasting(Element element)
        {
            if (element is View)
            {
                var view = element as View;
            }
        }
    }
}";

            var testFile = Path.Combine(_testDirectory, "test.cs");
            File.WriteAllText(testFile, testCode);

            // Act
            var result = _resolver.ProcessFile(testFile);

            // Assert
            Assert.IsTrue(result);
            var modifiedContent = File.ReadAllText(testFile);
            Assert.IsTrue(modifiedContent.Contains("using RevitView = Autodesk.Revit.DB.View;"));
            Assert.IsTrue(modifiedContent.Contains("if (element is RevitView)"));
            Assert.IsTrue(modifiedContent.Contains("var view = element as RevitView;"));
        }

        [TestMethod]
        public void ProcessFile_WithoutRevitContext_DoesNotModify()
        {
            // Arrange
            var testCode = @"using System;
using System.Windows;

namespace Test
{
    public class TestClass
    {
        public void TestMethod()
        {
            View myView = new View(); // This is not a Revit View
        }
    }
}";

            var testFile = Path.Combine(_testDirectory, "test.cs");
            File.WriteAllText(testFile, testCode);

            // Act
            var result = _resolver.ProcessFile(testFile);

            // Assert
            Assert.IsFalse(result);
            var content = File.ReadAllText(testFile);
            Assert.IsFalse(content.Contains("using RevitView ="));
            Assert.IsTrue(content.Contains("View myView = new View();"));
        }

        [TestMethod]
        public void ProcessFile_WithExistingAlias_DoesNotAddDuplicateAlias()
        {
            // Arrange
            var testCode = @"using System;
using Autodesk.Revit.DB;
using RevitView = Autodesk.Revit.DB.View;

namespace Test
{
    public class TestClass
    {
        public void TestMethod()
        {
            View myView = GetView();
        }
    }
}";

            var testFile = Path.Combine(_testDirectory, "test.cs");
            File.WriteAllText(testFile, testCode);

            // Act
            var result = _resolver.ProcessFile(testFile);

            // Assert
            Assert.IsTrue(result); // Should still modify references
            var modifiedContent = File.ReadAllText(testFile);
            
            // Should not add duplicate alias
            var aliasCount = modifiedContent.Split(new[] { "using RevitView = Autodesk.Revit.DB.View;" }, System.StringSplitOptions.None).Length - 1;
            Assert.AreEqual(1, aliasCount);
            
            // Should still replace references
            Assert.IsTrue(modifiedContent.Contains("RevitView myView = GetView();"));
        }

        [TestMethod]
        public void ProcessFile_WithMultipleViewUsages_ReplacesAll()
        {
            // Arrange
            var testCode = @"using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace Test
{
    public class TestClass
    {
        public void ProcessViews(View primaryView, IEnumerable<View> views)
        {
            View[] viewArray = new View[5];
            List<View> viewList = new List<View>();
            
            foreach (var view in views)
            {
                if (view is View)
                {
                    viewList.Add(view as View);
                }
            }
        }
    }
}";

            var testFile = Path.Combine(_testDirectory, "test.cs");
            File.WriteAllText(testFile, testCode);

            // Act
            var result = _resolver.ProcessFile(testFile);

            // Assert
            Assert.IsTrue(result);
            var modifiedContent = File.ReadAllText(testFile);
            Assert.IsTrue(modifiedContent.Contains("using RevitView = Autodesk.Revit.DB.View;"));
            Assert.IsTrue(modifiedContent.Contains("public void ProcessViews(RevitView primaryView, IEnumerable<RevitView> views)"));
            Assert.IsTrue(modifiedContent.Contains("RevitView[] viewArray = new RevitView[5];"));
            Assert.IsTrue(modifiedContent.Contains("List<RevitView> viewList = new List<RevitView>();"));
            Assert.IsTrue(modifiedContent.Contains("if (view is RevitView)"));
            Assert.IsTrue(modifiedContent.Contains("viewList.Add(view as RevitView);"));
        }

        [TestMethod]
        public void AnalyzeFile_WithViewUsage_ReturnsCorrectSummary()
        {
            // Arrange
            var testCode = @"using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace Test
{
    public class TestClass
    {
        public void ProcessViews(View primaryView, IEnumerable<View> views)
        {
            View[] viewArray = new View[5];
            List<View> viewList = new List<View>();
        }
    }
}";

            var testFile = Path.Combine(_testDirectory, "test.cs");
            File.WriteAllText(testFile, testCode);

            // Act
            var summary = _resolver.AnalyzeFile(testFile);

            // Assert
            Assert.IsTrue(summary.HasViewUsage);
            Assert.IsTrue(summary.HasRevitContext);
            Assert.IsFalse(summary.HasRevitViewAlias);
            Assert.IsTrue(summary.ViewVariables > 0);
            Assert.IsTrue(summary.ViewParameters > 0);
            Assert.IsTrue(summary.ViewArrays > 0);
            Assert.IsTrue(summary.ViewCollections > 0);
            Assert.IsTrue(summary.TotalUsages > 0);
        }

        [TestMethod]
        public void ProcessDirectory_WithMultipleFiles_ProcessesAllFiles()
        {
            // Arrange
            var file1Code = @"using Autodesk.Revit.DB;
namespace Test { public class Test1 { public void Method(View view) { } } }";

            var file2Code = @"using Autodesk.Revit.DB;
namespace Test { public class Test2 { public List<View> GetViews() { return null; } } }";

            var file1 = Path.Combine(_testDirectory, "test1.cs");
            var file2 = Path.Combine(_testDirectory, "test2.cs");
            File.WriteAllText(file1, file1Code);
            File.WriteAllText(file2, file2Code);

            // Act
            var modifiedFiles = _resolver.ProcessDirectory(_testDirectory);

            // Assert
            Assert.AreEqual(2, modifiedFiles.Count);
            Assert.IsTrue(modifiedFiles.Contains(file1));
            Assert.IsTrue(modifiedFiles.Contains(file2));

            // Verify both files were modified
            var content1 = File.ReadAllText(file1);
            var content2 = File.ReadAllText(file2);
            Assert.IsTrue(content1.Contains("using RevitView = Autodesk.Revit.DB.View;"));
            Assert.IsTrue(content2.Contains("using RevitView = Autodesk.Revit.DB.View;"));
        }
    }
}