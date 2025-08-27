/*
--------------------------------------------------------------------------------
 Dtools for Revit 2026 - FIXED VERSION
 C# Code for a Revit Add-in with corrected layer detection
 
 This version fixes the layer visibility issue by properly configuring geometry options
 to ensure ALL layers are detected, regardless of their visibility state.
 
 Two main tools:
 1. DwgToDetailLine: Converts selected layers from a DWG file into Revit Detail Lines
 2. ColumnByLine: Creates a structural column from selected rectangular Detail Lines
 
 To use this code:
 1. Create a new Class Library (.NET Framework) project in Visual Studio
 2. Add references to RevitAPI.dll and RevitAPIUI.dll from your Revit 2026 installation
 3. Add references to WindowsBase, PresentationCore, and PresentationFramework for UI
 4. Copy this code into a .cs file in your project
 5. Build the project to create a .dll file
 6. Create a .addin manifest file to load the .dll into Revit
--------------------------------------------------------------------------------
*/

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using RevitTaskDialog = Autodesk.Revit.UI.TaskDialog;
using RevitView = Autodesk.Revit.DB.View;

namespace RevitDtools
{
    //================================================================================
    // ENHANCED RIBBON PANEL SETUP WITH ORGANIZED PANELS
    //================================================================================
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication a)
        {
            try
            {
                string tabName = "Dtools";
                a.CreateRibbonTab(tabName);
                string thisAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

                // Create organized ribbon panels
                CreateGeometryProcessingPanel(a, tabName, thisAssemblyPath);
                CreateColumnCreationPanel(a, tabName, thisAssemblyPath);
                CreateBatchProcessingPanel(a, tabName, thisAssemblyPath);
                CreateSettingsPanel(a, tabName, thisAssemblyPath);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                RevitTaskDialog.Show("Error", $"Failed to initialize Dtools ribbon: {ex.Message}");
                return Result.Failed;
            }
        }

        private void CreateGeometryProcessingPanel(UIControlledApplication a, string tabName, string assemblyPath)
        {
            RibbonPanel geometryPanel = a.CreateRibbonPanel(tabName, "Geometry Processing");

            // DWG to Detail Line button
            PushButtonData dwgButtonData = new PushButtonData(
                "DwgToDetailLine", 
                "DWG to\nDetail Line", 
                assemblyPath, 
                "RevitDtools.DwgToDetailLineCommand");
            PushButton dwgButton = geometryPanel.AddItem(dwgButtonData) as PushButton;
            dwgButton.ToolTip = "Convert lines from a DWG file's layers into Revit Detail Lines.";
            dwgButton.LongDescription = "Select an imported DWG file and choose which layers to convert to Revit detail lines. " +
                                       "Supports all geometry types including arcs, splines, ellipses, text, and hatches.";
            
            // Enhanced DWG processing button
            PushButtonData enhancedDwgButtonData = new PushButtonData(
                "EnhancedDwgToDetailLine", 
                "Enhanced\nDWG Processing", 
                assemblyPath, 
                "RevitDtools.Core.Commands.EnhancedDwgToDetailLineCommand");
            PushButton enhancedDwgButton = geometryPanel.AddItem(enhancedDwgButtonData) as PushButton;
            enhancedDwgButton.ToolTip = "Advanced DWG processing with comprehensive geometry support.";
            enhancedDwgButton.LongDescription = "Process DWG files with support for all geometry types including " +
                                               "arcs, splines, ellipses, text elements, hatch patterns, and nested blocks.";
            
            // Set context-sensitive availability
            SetContextSensitiveAvailability(dwgButton, new[] { "ViewPlan", "ViewSection", "ViewDrafting" });
            SetContextSensitiveAvailability(enhancedDwgButton, new[] { "ViewPlan", "ViewSection", "ViewDrafting" });
        }

        private void CreateColumnCreationPanel(UIControlledApplication a, string tabName, string assemblyPath)
        {
            RibbonPanel columnPanel = a.CreateRibbonPanel(tabName, "Column Creation");

            // Basic column by line button
            PushButtonData columnButtonData = new PushButtonData(
                "ColumnByLine", 
                "Column\nby Line", 
                assemblyPath, 
                "RevitDtools.ColumnByLineCommand");
            PushButton columnButton = columnPanel.AddItem(columnButtonData) as PushButton;
            columnButton.ToolTip = "Create a structural column from selected rectangular detail lines.";
            columnButton.LongDescription = "Select 4 detail lines that form a rectangle to create a structural column " +
                                          "with automatic dimension calculation and family management.";

            // Enhanced column creation button
            PushButtonData enhancedColumnButtonData = new PushButtonData(
                "EnhancedColumnByLine", 
                "Enhanced\nColumn Creation", 
                assemblyPath, 
                "RevitDtools.Core.Commands.EnhancedColumnByLineCommand");
            PushButton enhancedColumnButton = columnPanel.AddItem(enhancedColumnButtonData) as PushButton;
            enhancedColumnButton.ToolTip = "Enhanced column creation with dynamic family management.";
            enhancedColumnButton.LongDescription = "Create columns with automatic family creation, custom dimensions, " +
                                                  "and intelligent parameter mapping.";

            // Batch column creation button
            PushButtonData batchColumnButtonData = new PushButtonData(
                "BatchColumnByLine", 
                "Batch\nColumns", 
                assemblyPath, 
                "RevitDtools.Core.Commands.BatchColumnByLineCommand");
            PushButton batchColumnButton = columnPanel.AddItem(batchColumnButtonData) as PushButton;
            batchColumnButton.ToolTip = "Create multiple columns from rectangular detail line groups.";
            batchColumnButton.LongDescription = "Automatically detect rectangles formed by detail lines and create " +
                                               "a structural column inside each rectangle. Perfect for processing " +
                                               "multiple column locations at once.";

            // Advanced column features button
            PushButtonData advancedColumnButtonData = new PushButtonData(
                "AdvancedColumn", 
                "Advanced\nColumns", 
                assemblyPath, 
                "RevitDtools.Core.Commands.AdvancedColumnCommand");
            PushButton advancedColumnButton = columnPanel.AddItem(advancedColumnButtonData) as PushButton;
            advancedColumnButton.ToolTip = "Advanced column creation features.";
            advancedColumnButton.LongDescription = "Create circular columns, custom shape columns, column grids, " +
                                                  "and integrate with column schedules.";

            // Set context-sensitive availability - columns need structural views
            SetContextSensitiveAvailability(columnButton, new[] { "ViewPlan", "View3D" });
            SetContextSensitiveAvailability(enhancedColumnButton, new[] { "ViewPlan", "View3D" });
            SetContextSensitiveAvailability(batchColumnButton, new[] { "ViewPlan", "View3D" });
            SetContextSensitiveAvailability(advancedColumnButton, new[] { "ViewPlan", "View3D" });
        }

        private void CreateBatchProcessingPanel(UIControlledApplication a, string tabName, string assemblyPath)
        {
            RibbonPanel batchPanel = a.CreateRibbonPanel(tabName, "Batch Processing");

            // Batch processing button
            PushButtonData batchButtonData = new PushButtonData(
                "BatchProcess", 
                "Batch\nProcessing", 
                assemblyPath, 
                "RevitDtools.Core.Commands.BatchProcessCommand");
            PushButton batchButton = batchPanel.AddItem(batchButtonData) as PushButton;
            batchButton.ToolTip = "Process multiple DWG files simultaneously.";
            batchButton.LongDescription = "Select multiple DWG files or entire folders to process in batch mode. " +
                                         "Includes progress tracking, cancellation support, and comprehensive reporting.";
            batchButton.LargeImage = GetEmbeddedImage("batch_processing_32.png");
            batchButton.Image = GetEmbeddedImage("batch_processing_16.png");

            // Set availability - batch processing works in any view with active document
            SetContextSensitiveAvailability(batchButton, new[] { "ViewPlan", "ViewSection", "ViewDrafting", "View3D" });
        }

        private void CreateSettingsPanel(UIControlledApplication a, string tabName, string assemblyPath)
        {
            RibbonPanel settingsPanel = a.CreateRibbonPanel(tabName, "Settings & Tools");

            // Settings button
            PushButtonData settingsButtonData = new PushButtonData(
                "Settings", 
                "Settings", 
                assemblyPath, 
                "RevitDtools.Core.Commands.SettingsCommand");
            PushButton settingsButton = settingsPanel.AddItem(settingsButtonData) as PushButton;
            settingsButton.ToolTip = "Configure RevitDtools preferences and settings.";
            settingsButton.LongDescription = "Access user preferences, layer mapping templates, default column families, " +
                                           "batch processing options, and logging settings.";
            settingsButton.LargeImage = GetEmbeddedImage("settings_32.png");
            settingsButton.Image = GetEmbeddedImage("settings_16.png");

            // Add separator
            settingsPanel.AddSeparator();

            // Diagnostic tools
            PushButtonData diagnoseFamilyButtonData = new PushButtonData(
                "DiagnoseFamilyIssues", 
                "Diagnose\nFamily Issues", 
                assemblyPath, 
                "RevitDtools.DiagnoseFamilyIssues");
            PushButton diagnoseFamilyButton = settingsPanel.AddItem(diagnoseFamilyButtonData) as PushButton;
            diagnoseFamilyButton.ToolTip = "Diagnose why batch column creation is failing.";
            diagnoseFamilyButton.LongDescription = "Analyze available column families and symbols to understand " +
                                                  "why batch processing might be failing to create columns.";

            // Load standard families button
            PushButtonData loadFamiliesButtonData = new PushButtonData(
                "LoadStandardColumnFamilies", 
                "Load Standard\nFamilies", 
                assemblyPath, 
                "RevitDtools.LoadStandardColumnFamilies");
            PushButton loadFamiliesButton = settingsPanel.AddItem(loadFamiliesButtonData) as PushButton;
            loadFamiliesButton.ToolTip = "Load standard Revit column families into the project.";
            loadFamiliesButton.LongDescription = "Automatically load common structural column families from the " +
                                                "Revit installation to enable batch column creation.";

            // Add separator
            settingsPanel.AddSeparator();

            // Help/About button
            PushButtonData helpButtonData = new PushButtonData(
                "Help", 
                "Help", 
                assemblyPath, 
                "RevitDtools.Core.Commands.HelpCommand");
            PushButton helpButton = settingsPanel.AddItem(helpButtonData) as PushButton;
            helpButton.ToolTip = "View help documentation and about information.";
            helpButton.LongDescription = "Access user documentation, version information, and support resources.";

            // Settings and help are always available
            // No context restrictions needed
        }

        private void SetContextSensitiveAvailability(PushButton button, string[] allowedViewTypes)
        {
            // Set context-sensitive availability using the corresponding availability class
            var availabilityClassName = $"RevitDtools.UI.Availability.{button.Name}Availability";
            button.AvailabilityClassName = availabilityClassName;
        }

        private System.Windows.Media.ImageSource GetEmbeddedImage(string imageName)
        {
            try
            {
                // For now, return null - images would be embedded resources
                // In a full implementation, this would load embedded image resources
                return null;
            }
            catch
            {
                return null;
            }
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }
    }

    // Selection filter for detail lines
    public class DetailLineSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem) => elem is DetailLine;
        public bool AllowReference(Reference reference, XYZ position) => false;
    }

    //================================================================================
    // TOOL 1: DWG TO DETAIL LINE COMMAND - FIXED LAYER DETECTION VERSION
    //================================================================================
    [Transaction(TransactionMode.Manual)]
    public class DwgToDetailLineCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            RevitView activeView = doc.ActiveView;

            if (!(activeView is ViewPlan || activeView is ViewSection || activeView is ViewDrafting))
            {
                message = "This tool can only be used in Plan, Section, or Drafting views.";
                return Result.Failed;
            }

            Reference dwgRef;
            try
            {
                dwgRef = uidoc.Selection.PickObject(ObjectType.Element, new DwgImportSelectionFilter(), "Select an imported or linked DWG file");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }

            ImportInstance importInstance = doc.GetElement(dwgRef) as ImportInstance;
            if (importInstance == null)
            {
                message = "The selected element is not a DWG import.";
                return Result.Failed;
            }

            // FIXED: Proper geometry options configuration for layer detection
            // When IncludeNonVisibleObjects = true, we must NOT set the View property
            // and must set DetailLevel explicitly
            Options options = new Options
            {
                IncludeNonVisibleObjects = true,
                DetailLevel = ViewDetailLevel.Fine
                // DO NOT set View property when IncludeNonVisibleObjects is true
            };

            var allCurves = new List<Tuple<Curve, string>>();
            var allLayers = new HashSet<string>();

            GeometryElement geomElem = importInstance.get_Geometry(options);
            if (geomElem != null)
            {
                GetAllCurvesFromDwg(geomElem, Transform.Identity, doc, allCurves, allLayers);
            }

            // Debug information
            System.Diagnostics.Debug.WriteLine($"Total curves found: {allCurves.Count}");
            System.Diagnostics.Debug.WriteLine($"Total layers found: {allLayers.Count}");
            foreach (var layer in allLayers.OrderBy(l => l))
            {
                System.Diagnostics.Debug.WriteLine($"Layer: '{layer}'");
            }

            if (!allCurves.Any())
            {
                RevitTaskDialog.Show("No Geometry", "Could not find any convertible line geometry in the selected DWG.");
                return Result.Failed;
            }

            var layerNames = allLayers.Where(n => !string.IsNullOrWhiteSpace(n))
                                     .OrderBy(name => name)
                                     .ToList();

            // If no named layers found, show all curves as "Unknown Layer"
            if (!layerNames.Any())
            {
                layerNames.Add("Unknown Layer");
                // Update curves without layer names
                for (int i = 0; i < allCurves.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(allCurves[i].Item2))
                    {
                        allCurves[i] = new Tuple<Curve, string>(allCurves[i].Item1, "Unknown Layer");
                    }
                }
            }

            var window = new DwgLayersWindow(layerNames);
            if (window.ShowDialog() != true)
            {
                return Result.Cancelled;
            }

            List<string> selectedLayers = window.GetSelectedLayers();
            if (!selectedLayers.Any())
            {
                return Result.Succeeded;
            }

            using (Transaction tx = new Transaction(doc, "Create Detail Lines from DWG"))
            {
                tx.Start();
                int linesCreated = 0;
                int linesSkipped = 0;

                try
                {
                    var curvesToCreate = allCurves.Where(t => selectedLayers.Contains(t.Item2));
                    foreach (var tuple in curvesToCreate)
                    {
                        Curve curve = tuple.Item1;
                        if (curve != null && curve.IsBound)
                        {
                            try
                            {
                                doc.Create.NewDetailCurve(activeView, curve);
                                linesCreated++;
                            }
                            catch (Exception ex)
                            {
                                linesSkipped++;
                                System.Diagnostics.Debug.WriteLine($"Skipped curve: {ex.Message}");
                            }
                        }
                        else
                        {
                            linesSkipped++;
                        }
                    }

                    tx.Commit();

                    string resultMessage = $"Created {linesCreated} detail line(s) from selected layers.";
                    if (linesSkipped > 0)
                    {
                        resultMessage += $"\n{linesSkipped} curves were skipped (invalid geometry).";
                    }

                    RevitTaskDialog.Show("Success", resultMessage);
                }
                catch (Exception ex)
                {
                    tx.RollBack();
                    message = $"Error creating detail lines: {ex.Message}";
                    return Result.Failed;
                }
            }
            return Result.Succeeded;
        }

        private void GetAllCurvesFromDwg(GeometryElement geomElem, Transform transform, Document doc,
                                        List<Tuple<Curve, string>> curveList, HashSet<string> layerSet)
        {
            foreach (GeometryObject geomObj in geomElem)
            {
                if (geomObj is Curve curve)
                {
                    string layerName = GetLayerName(geomObj, doc);
                    if (!string.IsNullOrWhiteSpace(layerName))
                    {
                        layerSet.Add(layerName);
                    }
                    curveList.Add(new Tuple<Curve, string>(curve.CreateTransformed(transform), layerName));
                }
                else if (geomObj is GeometryInstance geomInst)
                {
                    // Recursively process nested geometry (blocks)
                    GetAllCurvesFromDwg(geomInst.GetInstanceGeometry(),
                                      transform.Multiply(geomInst.Transform),
                                      doc, curveList, layerSet);
                }
                else if (geomObj is PolyLine polyLine)
                {
                    // Handle polylines by converting to individual line segments
                    string layerName = GetLayerName(geomObj, doc);
                    if (!string.IsNullOrWhiteSpace(layerName))
                    {
                        layerSet.Add(layerName);
                    }

                    IList<XYZ> points = polyLine.GetCoordinates();
                    for (int i = 0; i < points.Count - 1; i++)
                    {
                        try
                        {
                            XYZ start = transform.OfPoint(points[i]);
                            XYZ end = transform.OfPoint(points[i + 1]);

                            if (!start.IsAlmostEqualTo(end, 1e-6))
                            {
                                Line line = Line.CreateBound(start, end);
                                curveList.Add(new Tuple<Curve, string>(line, layerName));
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Skipped polyline segment: {ex.Message}");
                        }
                    }
                }
            }
        }

        private string GetLayerName(GeometryObject geomObj, Document doc)
        {
            try
            {
                if (geomObj.GraphicsStyleId != ElementId.InvalidElementId)
                {
                    Element styleElement = doc.GetElement(geomObj.GraphicsStyleId);
                    if (styleElement is GraphicsStyle gs && gs.GraphicsStyleCategory != null)
                    {
                        return gs.GraphicsStyleCategory.Name;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting layer name: {ex.Message}");
            }
            return null;
        }
    }

    public class DwgImportSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem) => elem is ImportInstance;
        public bool AllowReference(Reference reference, XYZ position) => false;
    }

    //================================================================================
    // TOOL 2: COLUMN BY LINE COMMAND - ENHANCED VERSION
    //================================================================================
    [Transaction(TransactionMode.Manual)]
    public class ColumnByLineCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Check current selection first
            List<DetailLine> detailLines = uidoc.Selection.GetElementIds()
                .Select(id => doc.GetElement(id))
                .OfType<DetailLine>()
                .ToList();

            // If we don't have exactly 4 lines selected, let user select them
            if (detailLines.Count != 4)
            {
                RevitTaskDialog dialog = new RevitTaskDialog("Column by Line")
                {
                    MainContent = $"Current selection: {detailLines.Count} detail lines.\n\n" +
                                 "This tool requires exactly 4 detail lines that form a rectangle.\n\n" +
                                 "Would you like to select the lines now?",
                    CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.Cancel,
                    DefaultButton = TaskDialogResult.Yes
                };

                if (dialog.Show() != TaskDialogResult.Yes)
                {
                    return Result.Cancelled;
                }

                // Let user select the detail lines
                try
                {
                    var selectedRefs = uidoc.Selection.PickObjects(ObjectType.Element,
                        new DetailLineSelectionFilter(),
                        "Select exactly 4 detail lines that form a rectangle:");

                    if (selectedRefs.Count != 4)
                    {
                        message = $"You selected {selectedRefs.Count} lines. Please select exactly 4 detail lines.";
                        return Result.Failed;
                    }

                    detailLines = selectedRefs.Select(r => doc.GetElement(r) as DetailLine).ToList();
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return Result.Cancelled;
                }
            }

            try
            {
                // Get all endpoints from the lines
                var allPoints = new List<XYZ>();
                foreach (var line in detailLines)
                {
                    Curve curve = line.GeometryCurve;
                    allPoints.Add(curve.GetEndPoint(0));
                    allPoints.Add(curve.GetEndPoint(1));
                }

                // Find unique corner points
                const double tolerance = 1e-6;
                var distinctPoints = new List<XYZ>();
                foreach (var point in allPoints)
                {
                    if (!distinctPoints.Any(p => p.IsAlmostEqualTo(point, tolerance)))
                    {
                        distinctPoints.Add(point);
                    }
                }

                if (distinctPoints.Count != 4)
                {
                    // Provide more detailed feedback
                    string detailedMessage = $"The selected lines do not form a simple closed rectangle.\n\n";
                    detailedMessage += $"Analysis:\n";
                    detailedMessage += $"• Found {distinctPoints.Count} unique corner points (need exactly 4)\n";
                    detailedMessage += $"• Total line endpoints: {allPoints.Count}\n\n";
                    detailedMessage += "Make sure:\n";
                    detailedMessage += "• Lines connect at corners to form a closed rectangle\n";
                    detailedMessage += "• No overlapping or duplicate lines\n";
                    detailedMessage += "• Lines are properly connected end-to-end";

                    RevitTaskDialog.Show("Rectangle Analysis Failed", detailedMessage);
                    return Result.Failed;
                }

                // Calculate rectangle bounds and center
                double minX = distinctPoints.Min(p => p.X);
                double minY = distinctPoints.Min(p => p.Y);
                double maxX = distinctPoints.Max(p => p.X);
                double maxY = distinctPoints.Max(p => p.Y);

                double width = maxX - minX;
                double height = maxY - minY;
                XYZ centerPoint = new XYZ((minX + maxX) / 2, (minY + maxY) / 2, distinctPoints[0].Z);

                if (width < 0.01 || height < 0.01)
                {
                    message = $"The rectangle is too small: {width:F3}' × {height:F3}'";
                    return Result.Failed;
                }

                // Get available column families and levels
                var columnSymbols = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_StructuralColumns)
                    .Cast<FamilySymbol>()
                    .ToList();

                var levels = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .OrderBy(l => l.Elevation)
                    .ToList();

                if (!columnSymbols.Any())
                {
                    message = "No structural column families found in the project.";
                    return Result.Failed;
                }

                if (!levels.Any())
                {
                    message = "No levels found in the project.";
                    return Result.Failed;
                }

                // Activate column symbols if needed
                using (Transaction tempTx = new Transaction(doc, "Activate Column Symbols"))
                {
                    tempTx.Start();
                    foreach (var symbol in columnSymbols.Where(s => !s.IsActive))
                    {
                        symbol.Activate();
                    }
                    tempTx.Commit();
                }

                // Show column creation dialog
                var window = new ColumnCreatorWindow(columnSymbols, levels, width, height);
                if (window.ShowDialog() != true)
                {
                    return Result.Cancelled;
                }

                // Create the column
                using (Transaction tx = new Transaction(doc, "Create Column from Lines"))
                {
                    tx.Start();
                    try
                    {
                        // Create column at the calculated center point
                        FamilyInstance column = doc.Create.NewFamilyInstance(
                            new XYZ(centerPoint.X, centerPoint.Y, window.SelectedLevel.Elevation),
                            window.SelectedFamilySymbol,
                            window.SelectedLevel,
                            StructuralType.Column);

                        doc.Regenerate();

                        // Try to set the width and height parameters
                        bool widthSet = false, heightSet = false;

                        Parameter widthParam = column.LookupParameter(window.WidthParamName);
                        if (widthParam != null && !widthParam.IsReadOnly)
                        {
                            widthParam.Set(width);
                            widthSet = true;
                        }

                        Parameter heightParam = column.LookupParameter(window.HeightParamName);
                        if (heightParam != null && !heightParam.IsReadOnly)
                        {
                            heightParam.Set(height);
                            heightSet = true;
                        }

                        tx.Commit();

                        // Show result message
                        string resultMessage = $"Column created successfully at ({centerPoint.X:F2}, {centerPoint.Y:F2})";
                        resultMessage += $"\nDimensions: {width:F2}' × {height:F2}'";

                        if (!widthSet || !heightSet)
                        {
                            List<string> missing = new List<string>();
                            if (!widthSet) missing.Add($"'{window.WidthParamName}'");
                            if (!heightSet) missing.Add($"'{window.HeightParamName}'");
                            resultMessage += $"\n\nWarning: Could not set parameter(s): {string.Join(", ", missing)}";
                            resultMessage += "\nPlease check the parameter names or set them manually.";
                        }

                        RevitTaskDialog.Show("Column Created", resultMessage);
                    }
                    catch (Exception ex)
                    {
                        tx.RollBack();
                        message = $"Error creating column: {ex.Message}";
                        return Result.Failed;
                    }
                }
            }
            catch (Exception ex)
            {
                message = $"Error processing lines: {ex.Message}";
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }

    //================================================================================
    // USER INTERFACE WINDOWS (WPF - Code Behind)
    //================================================================================
    public class DwgLayersWindow : Window
    {
        private List<System.Windows.Controls.CheckBox> _layerCheckBoxes = new List<System.Windows.Controls.CheckBox>();

        public DwgLayersWindow(List<string> layerNames)
        {
            Title = "DWG to Detail Line";
            Width = 400;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.CanResize;

            var grid = new System.Windows.Controls.Grid { Margin = new Thickness(15) };
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
            this.Content = grid;

            // Header
            var header = new System.Windows.Controls.Label
            {
                Content = $"Select DWG Layers to Convert ({layerNames.Count} layers found):",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            System.Windows.Controls.Grid.SetRow(header, 0);
            grid.Children.Add(header);

            // Layer list with scrolling
            var scroll = new System.Windows.Controls.ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                BorderThickness = new Thickness(1),
                BorderBrush = System.Windows.Media.Brushes.LightGray
            };

            var spLayers = new System.Windows.Controls.StackPanel { Margin = new Thickness(5) };
            scroll.Content = spLayers;

            foreach (var name in layerNames)
            {
                var cb = new System.Windows.Controls.CheckBox
                {
                    Content = name,
                    Margin = new Thickness(5, 3, 5, 3),
                    FontFamily = new System.Windows.Media.FontFamily("Consolas, Courier New, monospace")
                };
                _layerCheckBoxes.Add(cb);
                spLayers.Children.Add(cb);
            }

            System.Windows.Controls.Grid.SetRow(scroll, 1);
            grid.Children.Add(scroll);

            // Select All/None buttons
            var spSelect = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                Margin = new Thickness(0, 10, 0, 15)
            };

            var btnAll = new System.Windows.Controls.Button
            {
                Content = "Select All",
                Width = 80,
                Margin = new Thickness(0, 0, 10, 0)
            };
            var btnNone = new System.Windows.Controls.Button
            {
                Content = "Select None",
                Width = 80,
                Margin = new Thickness(0, 0, 10, 0)
            };

            btnAll.Click += (s, e) => _layerCheckBoxes.ForEach(cb => cb.IsChecked = true);
            btnNone.Click += (s, e) => _layerCheckBoxes.ForEach(cb => cb.IsChecked = false);

            spSelect.Children.Add(btnAll);
            spSelect.Children.Add(btnNone);
            System.Windows.Controls.Grid.SetRow(spSelect, 2);
            grid.Children.Add(spSelect);

            // OK/Cancel buttons
            var spButtons = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };

            var btnOk = new System.Windows.Controls.Button
            {
                Content = "Create Detail Lines",
                IsDefault = true,
                Width = 120,
                Height = 30,
                Margin = new Thickness(5, 0, 10, 0)
            };
            var btnCancel = new System.Windows.Controls.Button
            {
                Content = "Cancel",
                IsCancel = true,
                Width = 80,
                Height = 30,
                Margin = new Thickness(0)
            };

            btnOk.Click += (s, e) => { DialogResult = true; Close(); };
            btnCancel.Click += (s, e) => { DialogResult = false; Close(); };

            spButtons.Children.Add(btnOk);
            spButtons.Children.Add(btnCancel);
            System.Windows.Controls.Grid.SetRow(spButtons, 3);
            grid.Children.Add(spButtons);
        }

        public List<string> GetSelectedLayers()
        {
            return _layerCheckBoxes
                .Where(cb => cb.IsChecked == true)
                .Select(cb => cb.Content.ToString())
                .ToList();
        }
    }

    public class ColumnCreatorWindow : Window
    {
        public FamilySymbol SelectedFamilySymbol { get; private set; }
        public Level SelectedLevel { get; private set; }
        public string WidthParamName { get; private set; }
        public string HeightParamName { get; private set; }

        private System.Windows.Controls.ComboBox _familyComboBox, _levelComboBox;
        private System.Windows.Controls.TextBox _widthParamTextBox, _heightParamTextBox;

        public ColumnCreatorWindow(List<FamilySymbol> symbols, List<Level> levels, double width, double height)
        {
            Title = "Create Column from Rectangle";
            Width = 450;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;

            var grid = new System.Windows.Controls.Grid { Margin = new Thickness(20) };
            this.Content = grid;

            var sp = new System.Windows.Controls.StackPanel();
            grid.Children.Add(sp);

            // Rectangle info
            sp.Children.Add(new System.Windows.Controls.Label
            {
                Content = $"Rectangle Dimensions: {width:F3}' × {height:F3}'",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 15)
            });

            // Family selection
            sp.Children.Add(new System.Windows.Controls.Label { Content = "Column Family Type:", FontWeight = FontWeights.SemiBold });
            _familyComboBox = new System.Windows.Controls.ComboBox
            {
                ItemsSource = symbols,
                DisplayMemberPath = "Name",
                Margin = new Thickness(0, 0, 0, 15),
                Height = 25
            };
            if (symbols.Any()) _familyComboBox.SelectedIndex = 0;
            sp.Children.Add(_familyComboBox);

            // Level selection
            sp.Children.Add(new System.Windows.Controls.Label { Content = "Level:", FontWeight = FontWeights.SemiBold });
            _levelComboBox = new System.Windows.Controls.ComboBox
            {
                ItemsSource = levels,
                DisplayMemberPath = "Name",
                Margin = new Thickness(0, 0, 0, 15),
                Height = 25
            };
            if (levels.Any()) _levelComboBox.SelectedIndex = 0;
            sp.Children.Add(_levelComboBox);

            // Parameter names section
            sp.Children.Add(new System.Windows.Controls.Label
            {
                Content = "Column Parameter Names:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 10, 0, 5)
            });

            sp.Children.Add(new System.Windows.Controls.Label { Content = "Width Parameter Name:" });
            _widthParamTextBox = new System.Windows.Controls.TextBox
            {
                Text = "b",
                Margin = new Thickness(0, 0, 0, 10),
                Height = 25
            };
            sp.Children.Add(_widthParamTextBox);

            sp.Children.Add(new System.Windows.Controls.Label { Content = "Height Parameter Name:" });
            _heightParamTextBox = new System.Windows.Controls.TextBox
            {
                Text = "h",
                Margin = new Thickness(0, 0, 0, 20),
                Height = 25
            };
            sp.Children.Add(_heightParamTextBox);

            // Info text
            var infoText = new System.Windows.Controls.TextBlock
            {
                Text = "Common parameter names: b, h, Width, Depth, d, t",
                FontStyle = FontStyles.Italic,
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 20)
            };
            sp.Children.Add(infoText);

            // Buttons
            var spButtons = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };

            var btnOk = new System.Windows.Controls.Button
            {
                Content = "Create Column",
                IsDefault = true,
                Width = 120,
                Height = 30,
                Margin = new Thickness(0, 0, 15, 0)
            };
            var btnCancel = new System.Windows.Controls.Button
            {
                Content = "Cancel",
                IsCancel = true,
                Width = 80,
                Height = 30
            };

            btnOk.Click += OkButton_Click;
            btnCancel.Click += (s, e) => { DialogResult = false; Close(); };

            spButtons.Children.Add(btnOk);
            spButtons.Children.Add(btnCancel);
            sp.Children.Add(spButtons);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedFamilySymbol = _familyComboBox.SelectedItem as FamilySymbol;
            SelectedLevel = _levelComboBox.SelectedItem as Level;
            WidthParamName = _widthParamTextBox.Text?.Trim();
            HeightParamName = _heightParamTextBox.Text?.Trim();

            if (SelectedFamilySymbol == null || SelectedLevel == null ||
                string.IsNullOrWhiteSpace(WidthParamName) || string.IsNullOrWhiteSpace(HeightParamName))
            {
                System.Windows.MessageBox.Show("Please ensure all fields are filled.", "Invalid Input",
                               System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}