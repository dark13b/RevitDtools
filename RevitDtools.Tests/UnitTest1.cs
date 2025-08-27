using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RevitDtools;

namespace RevitDtools.Tests
{
    /// <summary>
    /// Unit tests for RevitDtools - demonstrates MSTest framework setup
    /// Tests basic functionality and class existence to verify build system
    /// </summary>
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            // Basic test to verify the test framework is working
            Assert.IsTrue(true, "Basic test framework verification should pass");
        }

        [TestMethod]
        public void TestAppClassExists()
        {
            // Test that the main App class exists and can be instantiated
            var appType = typeof(App);
            Assert.IsNotNull(appType, "App class type should exist");
            Assert.AreEqual("RevitDtools.App", appType.FullName, "App class should have correct full name");
        }

        [TestMethod]
        public void TestDetailLineSelectionFilterExists()
        {
            // Test that the DetailLineSelectionFilter class exists and can be instantiated
            var filterType = typeof(DetailLineSelectionFilter);
            Assert.IsNotNull(filterType, "DetailLineSelectionFilter class type should exist");
            
            var filter = new DetailLineSelectionFilter();
            Assert.IsNotNull(filter, "DetailLineSelectionFilter should be instantiable");
        }

        [TestMethod]
        public void TestDwgImportSelectionFilterExists()
        {
            // Test that the DwgImportSelectionFilter class exists and can be instantiated
            var filterType = typeof(DwgImportSelectionFilter);
            Assert.IsNotNull(filterType, "DwgImportSelectionFilter class type should exist");
            
            var filter = new DwgImportSelectionFilter();
            Assert.IsNotNull(filter, "DwgImportSelectionFilter should be instantiable");
        }

        [TestMethod]
        public void TestCommandClassesExist()
        {
            // Test that the main command classes exist
            var dwgToDetailLineType = typeof(DwgToDetailLineCommand);
            var columnByLineType = typeof(ColumnByLineCommand);
            
            Assert.IsNotNull(dwgToDetailLineType, "DwgToDetailLineCommand class should exist");
            Assert.IsNotNull(columnByLineType, "ColumnByLineCommand class should exist");
        }

        [TestMethod]
        public void TestUIWindowClassesExist()
        {
            // Test that the UI window classes exist
            var dwgLayersWindowType = typeof(DwgLayersWindow);
            var columnCreatorWindowType = typeof(ColumnCreatorWindow);
            
            Assert.IsNotNull(dwgLayersWindowType, "DwgLayersWindow class should exist");
            Assert.IsNotNull(columnCreatorWindowType, "ColumnCreatorWindow class should exist");
        }
    }
}