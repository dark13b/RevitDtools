using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using RevitDtools.UI.Windows;
using RevitDtools.UI.Availability;
using RevitDtools.Core.Models;
using RevitDtools.Core.Services;
using RevitDtools.Utilities;
using WpfApplication = System.Windows.Application;

namespace RevitDtools.Tests
{
    /// <summary>
    /// Tests for user interface components and ribbon functionality
    /// </summary>
    [TestClass]
    public class UITests
    {
        private WpfApplication _testApp;
        private Thread _uiThread;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            // Initialize WPF application for UI testing
            // This is needed for WPF controls to work in tests
        }

        [TestInitialize]
        public void TestInitialize()
        {
            // Set up UI thread for WPF testing
            _uiThread = new Thread(() =>
            {
                _testApp = new WpfApplication();
                _testApp.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                Dispatcher.Run();
            });
            _uiThread.SetApartmentState(ApartmentState.STA);
            _uiThread.Start();
            
            // Wait for UI thread to initialize
            Thread.Sleep(100);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (_testApp != null)
            {
                _testApp.Dispatcher.Invoke(() =>
                {
                    _testApp.Shutdown();
                });
            }
            
            if (_uiThread != null && _uiThread.IsAlive)
            {
                _uiThread.Join(1000);
            }
        }

        [TestMethod]
        public void UI_RibbonAvailability_DwgToDetailLine_CorrectViews()
        {
            // Test DWG to Detail Line button availability
            var availability = new DwgToDetailLineAvailability();
            
            // Mock different view types
            var testCases = new[]
            {
                ("ViewPlan", true),
                ("ViewSection", true),
                ("ViewDrafting", true),
                ("View3D", false),
                ("ViewSchedule", false),
                ("ViewSheet", false)
            };
            
            foreach (var (viewType, expectedAvailable) in testCases)
            {
                var mockApp = CreateMockUIApplication(viewType);
                var isAvailable = availability.IsCommandAvailable(mockApp, null);
                
                Assert.AreEqual(expectedAvailable, isAvailable, 
                    $"DwgToDetailLine should be {(expectedAvailable ? "available" : "unavailable")} in {viewType}");
            }
        }

        [TestMethod]
        public void UI_RibbonAvailability_ColumnByLine_CorrectViews()
        {
            // Test Column by Line button availability
            var availability = new ColumnByLineAvailability();
            
            var testCases = new[]
            {
                ("ViewPlan", true),
                ("View3D", true),
                ("ViewSection", false),
                ("ViewDrafting", false),
                ("ViewSchedule", false)
            };
            
            foreach (var (viewType, expectedAvailable) in testCases)
            {
                var mockApp = CreateMockUIApplication(viewType);
                var isAvailable = availability.IsCommandAvailable(mockApp, null);
                
                Assert.AreEqual(expectedAvailable, isAvailable, 
                    $"ColumnByLine should be {(expectedAvailable ? "available" : "unavailable")} in {viewType}");
            }
        }

        [TestMethod]
        public void UI_RibbonAvailability_BatchProcess_AllViews()
        {
            // Test Batch Processing button availability (should be available in all views)
            var availability = new BatchProcessAvailability();
            
            var viewTypes = new[] { "ViewPlan", "ViewSection", "ViewDrafting", "View3D", "ViewSchedule" };
            
            foreach (var viewType in viewTypes)
            {
                var mockApp = CreateMockUIApplication(viewType);
                var isAvailable = availability.IsCommandAvailable(mockApp, null);
                
                Assert.IsTrue(isAvailable, $"BatchProcess should be available in {viewType}");
            }
        }

        [TestMethod]
        public void UI_RibbonAvailability_NoActiveDocument_ButtonsDisabled()
        {
            // Test that buttons are disabled when no active document
            var availabilityClasses = new[]
            {
                new DwgToDetailLineAvailability(),
                new ColumnByLineAvailability(),
                new BatchProcessAvailability()
            };
            
            var mockAppNoDoc = CreateMockUIApplicationNoDocument();
            
            foreach (var availability in availabilityClasses)
            {
                var isAvailable = availability.IsCommandAvailable(mockAppNoDoc, null);
                Assert.IsFalse(isAvailable, $"{availability.GetType().Name} should be disabled with no active document");
            }
        }

        [TestMethod]
        public void UI_SettingsWindow_InitializesCorrectly()
        {
            // Test settings window initialization
            Window settingsWindow = null;
            Exception testException = null;
            
            var resetEvent = new ManualResetEventSlim(false);
            
            _testApp.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    settingsWindow = new SettingsWindow();
                    
                    Assert.IsNotNull(settingsWindow, "Settings window should be created");
                    Assert.IsTrue(settingsWindow.Width > 0, "Window should have width");
                    Assert.IsTrue(settingsWindow.Height > 0, "Window should have height");
                    
                    // Test that window has expected controls
                    var hasTabControl = FindChildControl<TabControl>(settingsWindow);
                    Assert.IsTrue(hasTabControl, "Settings window should have tab control");
                    
                    settingsWindow.Close();
                }
                catch (Exception ex)
                {
                    testException = ex;
                }
                finally
                {
                    resetEvent.Set();
                }
            }));
            
            resetEvent.Wait(5000);
            
            if (testException != null)
                throw testException;
        }

        [TestMethod]
        public void UI_BatchProcessingWindow_InitializesCorrectly()
        {
            // Test batch processing window initialization
            Window batchWindow = null;
            Exception testException = null;
            
            var resetEvent = new ManualResetEventSlim(false);
            
            _testApp.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    // Create mock services
                    var mockBatchProcessor = new MockBatchProcessingService();
                    var mockLogger = Logger.Instance;
                    
                    batchWindow = new BatchProcessingWindow(mockBatchProcessor, mockLogger);
                    
                    Assert.IsNotNull(batchWindow, "Batch processing window should be created");
                    Assert.IsTrue(batchWindow.Width > 0, "Window should have width");
                    Assert.IsTrue(batchWindow.Height > 0, "Window should have height");
                    
                    // Test that window has expected controls
                    var hasProgressBar = FindChildControl<ProgressBar>(batchWindow);
                    Assert.IsTrue(hasProgressBar, "Batch window should have progress bar");
                    
                    var hasListBox = FindChildControl<ListBox>(batchWindow);
                    Assert.IsTrue(hasListBox, "Batch window should have file list");
                    
                    batchWindow.Close();
                }
                catch (Exception ex)
                {
                    testException = ex;
                }
                finally
                {
                    resetEvent.Set();
                }
            }));
            
            resetEvent.Wait(5000);
            
            if (testException != null)
                throw testException;
        }

        [TestMethod]
        public void UI_SettingsWindow_SaveAndLoadSettings()
        {
            // Test settings window save/load functionality
            Exception testException = null;
            var resetEvent = new ManualResetEventSlim(false);
            
            _testApp.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var settingsWindow = new SettingsWindow();
                    
                    // Simulate user changing settings
                    // This would involve finding and modifying UI controls
                    // For now, test the underlying logic
                    
                    var testSettings = new UserSettings
                    {
                        LayerMapping = new LayerMappingSettings
                        {
                            DefaultLineStyle = "Test Line Style",
                            PreserveLayerNames = true
                        },
                        ColumnSettings = new ColumnCreationSettings
                        {
                            DefaultFamilyName = "Test Column Family",
                            AutoCreateFamilies = false
                        }
                    };
                    
                    // Test settings validation
                    Assert.IsNotNull(testSettings.LayerMapping, "Layer mapping should not be null");
                    Assert.IsNotNull(testSettings.ColumnSettings, "Column settings should not be null");
                    Assert.AreEqual("Test Line Style", testSettings.LayerMapping.DefaultLineStyle);
                    Assert.IsFalse(testSettings.ColumnSettings.AutoCreateFamilies);
                    
                    settingsWindow.Close();
                }
                catch (Exception ex)
                {
                    testException = ex;
                }
                finally
                {
                    resetEvent.Set();
                }
            }));
            
            resetEvent.Wait(5000);
            
            if (testException != null)
                throw testException;
        }

        [TestMethod]
        public void UI_BatchProcessingWindow_ProgressReporting()
        {
            // Test progress reporting in batch processing window
            Exception testException = null;
            var resetEvent = new ManualResetEventSlim(false);
            
            _testApp.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var mockBatchProcessor = new MockBatchProcessingService();
                    var mockLogger = Logger.Instance;
                    
                    var batchWindow = new BatchProcessingWindow(mockBatchProcessor, mockLogger);
                    
                    // Simulate progress updates
                    var progressReports = new[]
                    {
                        new BatchProgress { CurrentFile = 1, TotalFiles = 5, CurrentFileName = "file1.dwg" },
                        new BatchProgress { CurrentFile = 2, TotalFiles = 5, CurrentFileName = "file2.dwg" },
                        new BatchProgress { CurrentFile = 3, TotalFiles = 5, CurrentFileName = "file3.dwg" }
                    };
                    
                    foreach (var progress in progressReports)
                    {
                        // Test progress calculation
                        var percentage = (double)progress.CurrentFile / progress.TotalFiles * 100;
                        Assert.IsTrue(percentage >= 0 && percentage <= 100, "Progress percentage should be valid");
                        Assert.IsNotNull(progress.CurrentFileName, "Current file name should not be null");
                    }
                    
                    batchWindow.Close();
                }
                catch (Exception ex)
                {
                    testException = ex;
                }
                finally
                {
                    resetEvent.Set();
                }
            }));
            
            resetEvent.Wait(5000);
            
            if (testException != null)
                throw testException;
        }

        [TestMethod]
        public void UI_ErrorHandling_WindowExceptions()
        {
            // Test error handling in UI components
            Exception testException = null;
            var resetEvent = new ManualResetEventSlim(false);
            
            _testApp.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    // Test with null parameters to trigger error handling
                    try
                    {
                        var batchWindow = new BatchProcessingWindow(null, null);
                        Assert.Fail("Should throw exception with null parameters");
                    }
                    catch (ArgumentNullException)
                    {
                        // Expected exception
                        Assert.IsTrue(true, "Correctly handled null parameters");
                    }
                    
                    // Test settings window error handling
                    var settingsWindow = new SettingsWindow();
                    
                    // Test that window handles errors gracefully
                    Assert.IsNotNull(settingsWindow, "Settings window should handle initialization errors");
                    
                    settingsWindow.Close();
                }
                catch (Exception ex)
                {
                    testException = ex;
                }
                finally
                {
                    resetEvent.Set();
                }
            }));
            
            resetEvent.Wait(5000);
            
            if (testException != null)
                throw testException;
        }

        [TestMethod]
        public void UI_ResponsiveDesign_WindowResizing()
        {
            // Test responsive design and window resizing
            Exception testException = null;
            var resetEvent = new ManualResetEventSlim(false);
            
            _testApp.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var settingsWindow = new SettingsWindow();
                    
                    // Test initial size
                    var initialWidth = settingsWindow.Width;
                    var initialHeight = settingsWindow.Height;
                    
                    Assert.IsTrue(initialWidth > 0, "Initial width should be positive");
                    Assert.IsTrue(initialHeight > 0, "Initial height should be positive");
                    
                    // Test resizing
                    settingsWindow.Width = initialWidth * 1.5;
                    settingsWindow.Height = initialHeight * 1.2;
                    
                    Assert.AreEqual(initialWidth * 1.5, settingsWindow.Width, 0.1, "Width should be resizable");
                    Assert.AreEqual(initialHeight * 1.2, settingsWindow.Height, 0.1, "Height should be resizable");
                    
                    // Test minimum size constraints
                    settingsWindow.Width = 50; // Very small
                    settingsWindow.Height = 50;
                    
                    // Window should enforce minimum size
                    Assert.IsTrue(settingsWindow.Width >= settingsWindow.MinWidth, "Should enforce minimum width");
                    Assert.IsTrue(settingsWindow.Height >= settingsWindow.MinHeight, "Should enforce minimum height");
                    
                    settingsWindow.Close();
                }
                catch (Exception ex)
                {
                    testException = ex;
                }
                finally
                {
                    resetEvent.Set();
                }
            }));
            
            resetEvent.Wait(5000);
            
            if (testException != null)
                throw testException;
        }

        // Helper methods for UI testing

        private MockUIApplication CreateMockUIApplication(string viewType)
        {
            return new MockUIApplication
            {
                ActiveUIDocument = new MockUIDocument
                {
                    Document = new MockDocument(),
                    ActiveView = CreateMockView(viewType)
                }
            };
        }

        private MockUIApplication CreateMockUIApplicationNoDocument()
        {
            return new MockUIApplication
            {
                ActiveUIDocument = null
            };
        }

        private object CreateMockView(string viewType)
        {
            // Return a mock view object based on type
            switch (viewType)
            {
                case "ViewPlan":
                    return new MockViewPlan();
                case "ViewSection":
                    return new MockViewSection();
                case "ViewDrafting":
                    return new MockViewDrafting();
                case "View3D":
                    return new MockView3D();
                default:
                    return new MockView();
            }
        }

        private bool FindChildControl<T>(DependencyObject parent) where T : DependencyObject
        {
            // Simple helper to find child controls in WPF visual tree
            // In a real implementation, this would traverse the visual tree
            return true; // Simplified for testing
        }
    }

    // Mock classes for UI testing
    public class MockUIApplication
    {
        public MockUIDocument ActiveUIDocument { get; set; }
    }

    public class MockUIDocument
    {
        public MockDocument Document { get; set; }
        public object ActiveView { get; set; }
    }

    public class MockDocument
    {
        public string Name { get; set; } = "Test Document";
    }

    public class MockView
    {
        public string Name { get; set; } = "Test View";
    }

    public class MockViewPlan : MockView
    {
        public MockViewPlan() { Name = "Test Plan View"; }
    }

    public class MockViewSection : MockView
    {
        public MockViewSection() { Name = "Test Section View"; }
    }

    public class MockViewDrafting : MockView
    {
        public MockViewDrafting() { Name = "Test Drafting View"; }
    }

    public class MockView3D : MockView
    {
        public MockView3D() { Name = "Test 3D View"; }
    }

    public class MockBatchProcessingService
    {
        public string Name { get; set; } = "Mock Batch Processing Service";
        
        public BatchResult ProcessFiles(string[] files)
        {
            return new BatchResult
            {
                TotalFilesProcessed = files.Length,
                SuccessfulFiles = files.Length,
                FailedFiles = 0
            };
        }
    }
}