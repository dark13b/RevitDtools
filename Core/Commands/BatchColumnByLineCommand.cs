using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RevitDtools.Core.Interfaces;
using RevitDtools.Core.Models;
using RevitDtools.Core.Services;
using RevitDtools.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using RevitTaskDialog = Autodesk.Revit.UI.TaskDialog;

namespace RevitDtools.Core.Commands
{
    /// <summary>
    /// Batch command for creating structural columns from multiple rectangular detail line groups
    /// Automatically detects rectangles formed by 4 connected lines and creates columns inside them
    /// 
    /// UNIT HANDLING:
    /// - Revit API always works internally in feet regardless of document display units
    /// - All coordinates, dimensions, and parameters are in feet
    /// - Display formatting respects the document's unit settings
    /// - Validation limits are set in feet but are reasonable for any unit system
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class BatchColumnByLineCommand : IExternalCommand
    {
        private IFamilyManager _familyManager;
        private ILogger _logger;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Initialize services
            _logger = Logger.Instance;
            _familyManager = new FamilyManagementService(doc, _logger);

            try
            {
                _logger.LogInfo("Starting Batch Column By Line command", "Execute");

                // Get all detail lines from selection or user input
                var detailLines = GetDetailLinesFromUser(uidoc, doc);
                if (detailLines == null || detailLines.Count == 0)
                {
                    message = "No detail lines selected";
                    return Result.Cancelled;
                }

                _logger.LogInfo($"Processing {detailLines.Count} detail lines", "Execute");

                // Detect rectangular groups from the lines
                var rectangularGroups = DetectRectangularGroups(detailLines);
                if (rectangularGroups.Count == 0)
                {
                    RevitTaskDialog.Show("No Rectangles Found", 
                        $"Could not detect any complete rectangles from the {detailLines.Count} selected lines.\n\n" +
                        "Make sure:\n" +
                        "• Lines are connected end-to-end to form closed rectangles\n" +
                        "• Each rectangle consists of exactly 4 lines\n" +
                        "• Lines are properly aligned (horizontal and vertical)");
                    return Result.Failed;
                }

                _logger.LogInfo($"Detected {rectangularGroups.Count} rectangular groups", "Execute");

                // Show detailed confirmation dialog
                string detailsMessage = $"Analysis Results:\n";
                detailsMessage += $"• Total detail lines selected: {detailLines.Count}\n";
                detailsMessage += $"• Rectangles detected: {rectangularGroups.Count}\n";
                detailsMessage += $"• Lines used in rectangles: {rectangularGroups.Sum(g => g.Lines.Count)}\n";
                detailsMessage += $"• Unused lines: {detailLines.Count - rectangularGroups.Sum(g => g.Lines.Count)}\n\n";
                
                if (rectangularGroups.Count > 0)
                {
                    detailsMessage += "Rectangle Details:\n";
                    for (int i = 0; i < Math.Min(rectangularGroups.Count, 5); i++)
                    {
                        var rect = rectangularGroups[i];
                        detailsMessage += $"  {i + 1}. Size: {rect.Analysis.Width:F2}' × {rect.Analysis.Height:F2}' at ({rect.Analysis.CenterPoint.X:F1}, {rect.Analysis.CenterPoint.Y:F1})\n";
                    }
                    if (rectangularGroups.Count > 5)
                    {
                        detailsMessage += $"  ... and {rectangularGroups.Count - 5} more rectangles\n";
                    }
                }
                
                detailsMessage += $"\nThis will create {rectangularGroups.Count} structural columns (one inside each rectangle).\n\n";
                detailsMessage += "Continue with column creation?";

                var confirmResult = RevitTaskDialog.Show("Batch Column Creation", detailsMessage,
                    TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);

                if (confirmResult != TaskDialogResult.Yes)
                {
                    return Result.Cancelled;
                }

                // Process each rectangular group
                var results = ProcessRectangularGroups(doc, rectangularGroups);

                // Show results
                ShowResults(results, rectangularGroups.Count);

                _logger.LogInfo($"Batch Column By Line command completed: {results.SuccessCount} columns created", "Execute");
                return results.SuccessCount > 0 ? Result.Succeeded : Result.Failed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BatchColumnByLineCommand");
                message = $"Unexpected error: {ex.Message}";
                return Result.Failed;
            }
        }

        #region Private Methods

        private List<DetailLine> GetDetailLinesFromUser(UIDocument uidoc, Document doc)
        {
            try
            {
                // Check current selection first
                List<DetailLine> detailLines = uidoc.Selection.GetElementIds()
                    .Select(id => doc.GetElement(id))
                    .OfType<DetailLine>()
                    .ToList();

                // If no lines selected, let user select them
                if (detailLines.Count == 0)
                {
                    RevitTaskDialog dialog = new RevitTaskDialog("Batch Column by Line")
                    {
                        MainContent = "This tool will automatically detect rectangles formed by detail lines " +
                                     "and create a structural column inside each rectangle.\n\n" +
                                     "Select all the detail lines that form rectangles:",
                        CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.Cancel,
                        DefaultButton = TaskDialogResult.Yes
                    };

                    if (dialog.Show() != TaskDialogResult.Yes)
                    {
                        return null;
                    }

                    // Let user select multiple detail lines
                    var selectedRefs = uidoc.Selection.PickObjects(ObjectType.Element,
                        new DetailLineSelectionFilter(),
                        "Select all detail lines that form rectangles (use Ctrl+Click for multiple selection):");

                    detailLines = selectedRefs.Select(r => doc.GetElement(r) as DetailLine).ToList();
                }

                return detailLines;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetDetailLinesFromUser");
                return null;
            }
        }

        private List<RectangularGroup> DetectRectangularGroups(List<DetailLine> detailLines)
        {
            var rectangularGroups = new List<RectangularGroup>();
            var usedLines = new HashSet<ElementId>();
            // Tolerance for point matching - Revit internal units are feet, so this is about 0.0003mm
            const double tolerance = 1e-6;

            _logger.LogInfo($"Starting rectangle detection with {detailLines.Count} detail lines", "DetectRectangularGroups");

            foreach (var line in detailLines)
            {
                if (usedLines.Contains(line.Id))
                {
                    _logger.LogInfo($"Skipping line {line.Id} - already used in a rectangle", "DetectRectangularGroups");
                    continue;
                }

                _logger.LogInfo($"Attempting to find rectangle starting from line {line.Id}", "DetectRectangularGroups");
                
                // Try to find a rectangle starting from this line
                var rectangle = FindRectangleFromLine(line, detailLines, usedLines, tolerance);
                if (rectangle != null)
                {
                    _logger.LogInfo($"Found rectangle with {rectangle.Lines.Count} lines, center at ({rectangle.Analysis.CenterPoint.X:F2}, {rectangle.Analysis.CenterPoint.Y:F2}), size {rectangle.Analysis.Width:F3}' × {rectangle.Analysis.Height:F3}'");
                    rectangularGroups.Add(rectangle);
                    
                    // Mark all lines in this rectangle as used
                    foreach (var rectLine in rectangle.Lines)
                    {
                        usedLines.Add(rectLine.Id);
                        _logger.LogInfo($"Marking line {rectLine.Id} as used", "DetectRectangularGroups");
                    }
                }
                else
                {
                    _logger.LogInfo($"No rectangle found starting from line {line.Id}", "DetectRectangularGroups");
                }
            }

            _logger.LogInfo($"Rectangle detection complete: found {rectangularGroups.Count} rectangles from {detailLines.Count} lines", "DetectRectangularGroups");
            return rectangularGroups;
        }

        private RectangularGroup FindRectangleFromLine(DetailLine startLine, List<DetailLine> allLines, 
            HashSet<ElementId> usedLines, double tolerance)
        {
            try
            {
                var rectangleLines = new List<DetailLine> { startLine };
                var currentLine = startLine;
                var startPoint = currentLine.GeometryCurve.GetEndPoint(0);
                var currentEndPoint = currentLine.GeometryCurve.GetEndPoint(1);

                // Try to find 3 more connected lines to complete the rectangle
                for (int i = 0; i < 3; i++)
                {
                    var nextLine = FindConnectedLine(currentEndPoint, allLines, rectangleLines, usedLines, tolerance);
                    if (nextLine == null)
                        return null;

                    rectangleLines.Add(nextLine);
                    
                    // Update current position
                    var nextCurve = nextLine.GeometryCurve;
                    if (nextCurve.GetEndPoint(0).IsAlmostEqualTo(currentEndPoint, tolerance))
                    {
                        currentEndPoint = nextCurve.GetEndPoint(1);
                    }
                    else if (nextCurve.GetEndPoint(1).IsAlmostEqualTo(currentEndPoint, tolerance))
                    {
                        currentEndPoint = nextCurve.GetEndPoint(0);
                    }
                    else
                    {
                        return null; // Not properly connected
                    }

                    currentLine = nextLine;
                }

                // Check if the last line connects back to the start
                if (!currentEndPoint.IsAlmostEqualTo(startPoint, tolerance))
                    return null;

                // Validate that this forms a proper rectangle
                var analysis = AnalyzeRectangle(rectangleLines);
                if (!analysis.IsValid)
                    return null;

                return new RectangularGroup
                {
                    Lines = rectangleLines,
                    Analysis = analysis
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error finding rectangle from line {startLine.Id}: {ex.Message}", "FindRectangleFromLine");
                return null;
            }
        }

        private DetailLine FindConnectedLine(XYZ point, List<DetailLine> allLines, 
            List<DetailLine> excludeLines, HashSet<ElementId> usedLines, double tolerance)
        {
            foreach (var line in allLines)
            {
                if (excludeLines.Contains(line) || usedLines.Contains(line.Id))
                    continue;

                var curve = line.GeometryCurve;
                if (curve.GetEndPoint(0).IsAlmostEqualTo(point, tolerance) ||
                    curve.GetEndPoint(1).IsAlmostEqualTo(point, tolerance))
                {
                    return line;
                }
            }
            return null;
        }

        private RectangleAnalysis AnalyzeRectangle(List<DetailLine> detailLines)
        {
            try
            {
                if (detailLines.Count != 4)
                {
                    _logger.LogWarning($"Rectangle analysis failed: expected 4 lines, got {detailLines.Count}", "AnalyzeRectangle");
                    return new RectangleAnalysis { IsValid = false };
                }

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
                    _logger.LogWarning($"Rectangle analysis failed: expected 4 unique corners, got {distinctPoints.Count}", "AnalyzeRectangle");
                    return new RectangleAnalysis { IsValid = false };
                }

                // Calculate rectangle bounds and center
                double minX = distinctPoints.Min(p => p.X);
                double minY = distinctPoints.Min(p => p.Y);
                double maxX = distinctPoints.Max(p => p.X);
                double maxY = distinctPoints.Max(p => p.Y);

                double width = maxX - minX;
                double height = maxY - minY;
                XYZ centerPoint = new XYZ((minX + maxX) / 2, (minY + maxY) / 2, distinctPoints[0].Z);

                // Validate that this is actually rectangular (not just any 4-sided shape)
                if (!IsActuallyRectangular(distinctPoints, tolerance))
                {
                    _logger.LogWarning($"Shape analysis failed: 4 points do not form a proper rectangle", "AnalyzeRectangle");
                    return new RectangleAnalysis { IsValid = false };
                }

                // Get document units for validation (all internal Revit units are in feet)
                // Convert reasonable limits from feet to document units
                double minDimension = 0.01; // 0.01 feet minimum (about 3mm)
                double maxDimension = 50.0;  // 50 feet maximum (about 15m)

                // Validate dimensions (Revit internal units are always feet)
                if (width < minDimension || height < minDimension || width > maxDimension || height > maxDimension)
                {
                    _logger.LogWarning($"Rectangle size validation failed: {width:F3}' × {height:F3}' (limits: {minDimension:F3}' to {maxDimension:F1}')", "AnalyzeRectangle");
                    return new RectangleAnalysis { IsValid = false };
                }

                _logger.LogInfo($"Valid rectangle analyzed: {width:F3}' × {height:F3}' at ({centerPoint.X:F2}, {centerPoint.Y:F2})", "AnalyzeRectangle");
                return new RectangleAnalysis
                {
                    IsValid = true,
                    Width = width,
                    Height = height,
                    CenterPoint = centerPoint,
                    CornerPoints = distinctPoints
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AnalyzeRectangle");
                return new RectangleAnalysis { IsValid = false };
            }
        }

        /// <summary>
        /// Validates that 4 points actually form a rectangle (not just any quadrilateral)
        /// </summary>
        private bool IsActuallyRectangular(List<XYZ> points, double tolerance)
        {
            if (points.Count != 4) return false;

            // Sort points to get them in order (bottom-left, bottom-right, top-right, top-left)
            var sortedPoints = points.OrderBy(p => p.Y).ThenBy(p => p.X).ToList();
            
            // For a rectangle, we should have two pairs of points with same Y coordinates
            // and two pairs with same X coordinates
            var yGroups = sortedPoints.GroupBy(p => Math.Round(p.Y / tolerance) * tolerance).ToList();
            var xGroups = sortedPoints.GroupBy(p => Math.Round(p.X / tolerance) * tolerance).ToList();

            // Should have exactly 2 Y levels and 2 X levels
            if (yGroups.Count != 2 || xGroups.Count != 2) return false;

            // Each Y level should have exactly 2 points, each X level should have exactly 2 points
            if (yGroups.Any(g => g.Count() != 2) || xGroups.Any(g => g.Count() != 2)) return false;

            return true;
        }

        private BatchProcessingResult ProcessRectangularGroups(Document doc, List<RectangularGroup> rectangularGroups)
        {
            var result = new BatchProcessingResult();
            
            // First, prepare all family symbols outside of the main transaction
            var symbolCache = new Dictionary<string, FamilySymbol>();
            
            foreach (var group in rectangularGroups)
            {
                string key = $"{group.Analysis.Width:F3}x{group.Analysis.Height:F3}";
                if (!symbolCache.ContainsKey(key))
                {
                    var symbol = GetOrCreateFamilySymbol(group.Analysis.Width, group.Analysis.Height);
                    symbolCache[key] = symbol;
                }
            }
            
            // Activate all symbols in a separate transaction if needed
            var symbolsToActivate = symbolCache.Values.Where(s => s != null && !s.IsActive).ToList();
            if (symbolsToActivate.Any())
            {
                using (var activationTransaction = new Transaction(doc, "Activate Column Symbols"))
                {
                    activationTransaction.Start();
                    foreach (var symbol in symbolsToActivate)
                    {
                        try
                        {
                            symbol.Activate();
                            _logger.LogInfo($"Activated symbol '{symbol.Name}'", "ProcessRectangularGroups");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Could not activate symbol '{symbol.Name}': {ex.Message}", "ProcessRectangularGroups");
                        }
                    }
                    activationTransaction.Commit();
                }
            }
            
            // Now create columns in the main transaction
            using (var transaction = new Transaction(doc, "Create Batch Columns"))
            {
                transaction.Start();

                foreach (var group in rectangularGroups)
                {
                    try
                    {
                        // Get the cached family symbol
                        string key = $"{group.Analysis.Width:F3}x{group.Analysis.Height:F3}";
                        var familySymbol = symbolCache.ContainsKey(key) ? symbolCache[key] : null;
                        
                        if (familySymbol == null)
                        {
                            result.Failures.Add($"Could not find family for {group.Analysis.Width:F3}' x {group.Analysis.Height:F3}'");
                            continue;
                        }

                        // Get the level for column placement
                        var level = GetColumnLevel(doc, group.Analysis.CenterPoint);
                        if (level == null)
                        {
                            result.Failures.Add($"Could not determine level for column at ({group.Analysis.CenterPoint.X:F2}, {group.Analysis.CenterPoint.Y:F2})");
                            continue;
                        }

                        // Create the column
                        FamilyInstance column = doc.Create.NewFamilyInstance(
                            new XYZ(group.Analysis.CenterPoint.X, group.Analysis.CenterPoint.Y, level.Elevation),
                            familySymbol,
                            level,
                            StructuralType.Column);

                        if (column != null)
                        {
                            result.SuccessCount++;
                            result.CreatedColumns.Add(new ColumnInfo
                            {
                                Column = column,
                                Width = group.Analysis.Width,
                                Height = group.Analysis.Height,
                                CenterPoint = group.Analysis.CenterPoint
                            });
                        }
                        else
                        {
                            result.Failures.Add($"Failed to create column at ({group.Analysis.CenterPoint.X:F2}, {group.Analysis.CenterPoint.Y:F2})");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Failures.Add($"Error creating column: {ex.Message}");
                        _logger.LogError(ex, "ProcessRectangularGroup");
                    }
                }

                if (result.SuccessCount > 0)
                {
                    doc.Regenerate();
                    transaction.Commit();
                }
                else
                {
                    transaction.RollBack();
                }
            }

            return result;
        }

        private FamilySymbol GetOrCreateFamilySymbol(double width, double height)
        {
            try
            {
                var symbol = _familyManager.FindOrCreateSymbol(width, height);
                
                // If we couldn't find or create a symbol, try fallback options
                if (symbol == null)
                {
                    _logger.LogWarning($"Could not find or create symbol for {width:F3}' x {height:F3}', trying fallbacks", "GetOrCreateFamilySymbol");
                    
                    // Fallback 1: Try to find any existing symbol with similar proportions
                    symbol = FindSimilarSymbol(width, height);
                    
                    // Fallback 2: Use the first available column symbol
                    if (symbol == null)
                    {
                        var availableSymbols = _familyManager.GetAvailableColumnSymbols();
                        symbol = availableSymbols.FirstOrDefault();
                        if (symbol != null)
                        {
                            _logger.LogWarning($"Using fallback symbol '{symbol.Name}' from family '{symbol.Family.Name}'", "GetOrCreateFamilySymbol");
                        }
                    }
                }
                
                // Activate symbol if needed - but don't start a new transaction if we're already in one
                if (symbol != null && !symbol.IsActive)
                {
                    try
                    {
                        symbol.Activate();
                        _logger.LogInfo($"Activated symbol '{symbol.Name}'", "GetOrCreateFamilySymbol");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Could not activate symbol '{symbol.Name}': {ex.Message}", "GetOrCreateFamilySymbol");
                        // Continue anyway - the symbol might still work
                    }
                }

                return symbol;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOrCreateFamilySymbol");
                return null;
            }
        }

        private FamilySymbol FindSimilarSymbol(double targetWidth, double targetHeight)
        {
            try
            {
                var symbols = _familyManager.GetAvailableColumnSymbols();
                FamilySymbol bestMatch = null;
                double bestScore = double.MaxValue;

                foreach (var symbol in symbols)
                {
                    if (TryGetSymbolDimensions(symbol, out double symbolWidth, out double symbolHeight))
                    {
                        // Calculate similarity score based on area and aspect ratio
                        double targetArea = targetWidth * targetHeight;
                        double symbolArea = symbolWidth * symbolHeight;
                        double targetRatio = targetWidth / targetHeight;
                        double symbolRatio = symbolWidth / symbolHeight;

                        double areaScore = Math.Abs(targetArea - symbolArea) / targetArea;
                        double ratioScore = Math.Abs(targetRatio - symbolRatio) / Math.Max(targetRatio, symbolRatio);
                        double totalScore = areaScore + ratioScore;

                        if (totalScore < bestScore)
                        {
                            bestScore = totalScore;
                            bestMatch = symbol;
                        }
                    }
                }

                if (bestMatch != null)
                {
                    _logger.LogInfo($"Found similar symbol '{bestMatch.Name}' for dimensions {targetWidth:F3}' x {targetHeight:F3}' (similarity score: {bestScore:F3})", "FindSimilarSymbol");
                }

                return bestMatch;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FindSimilarSymbol");
                return null;
            }
        }

        private bool TryGetSymbolDimensions(FamilySymbol symbol, out double width, out double height)
        {
            width = 0;
            height = 0;

            try
            {
                // Common parameter names for width and height
                var widthNames = new[] { "b", "Width", "Depth", "d" };
                var heightNames = new[] { "h", "Height", "t" };

                foreach (var widthName in widthNames)
                {
                    var widthParam = symbol.LookupParameter(widthName);
                    if (widthParam != null && widthParam.HasValue)
                    {
                        width = widthParam.AsDouble();
                        break;
                    }
                }

                foreach (var heightName in heightNames)
                {
                    var heightParam = symbol.LookupParameter(heightName);
                    if (heightParam != null && heightParam.HasValue)
                    {
                        height = heightParam.AsDouble();
                        break;
                    }
                }

                return width > 0 && height > 0;
            }
            catch
            {
                return false;
            }
        }

        private Level GetColumnLevel(Document doc, XYZ centerPoint)
        {
            try
            {
                var levels = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .OrderBy(l => l.Elevation)
                    .ToList();

                if (!levels.Any())
                    return null;

                // Find the level closest to the center point elevation
                Level bestLevel = levels.First();
                double minDistance = Math.Abs(bestLevel.Elevation - centerPoint.Z);

                foreach (var level in levels)
                {
                    double distance = Math.Abs(level.Elevation - centerPoint.Z);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        bestLevel = level;
                    }
                }

                return bestLevel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetColumnLevel");
                return null;
            }
        }

        private void ShowResults(BatchProcessingResult results, int totalRectangles)
        {
            string message = $"Batch Column Creation Results:\n\n";
            message += $"📊 Summary:\n";
            message += $"• Rectangles detected: {totalRectangles}\n";
            message += $"• Columns successfully created: {results.SuccessCount}\n";
            message += $"• Failed attempts: {results.Failures.Count}\n\n";

            if (results.CreatedColumns.Any())
            {
                message += $"✅ Successfully Created Columns ({results.SuccessCount}):\n";
                for (int i = 0; i < results.CreatedColumns.Count; i++)
                {
                    var col = results.CreatedColumns[i];
                    // Format dimensions using Revit's internal units (feet)
                    // Note: Revit API always works in feet internally, display will be converted by Revit
                    string widthStr = FormatDimension(col.Width);
                    string heightStr = FormatDimension(col.Height);
                    message += $"  {i + 1}. Column {widthStr} × {heightStr} at center ({col.CenterPoint.X:F2}, {col.CenterPoint.Y:F2})\n";
                }
                message += $"\n💡 Note: Each column was placed at the center of its corresponding rectangle.\n";
            }

            if (results.Failures.Any())
            {
                message += $"\n❌ Failures ({results.Failures.Count}):\n";
                foreach (var failure in results.Failures.Take(5)) // Show first 5 failures
                {
                    message += $"  • {failure}\n";
                }
                if (results.Failures.Count > 5)
                {
                    message += $"  • ... and {results.Failures.Count - 5} more failures\n";
                }
            }

            if (results.SuccessCount > 0)
            {
                message += $"\n🎯 Result: Created {results.SuccessCount} columns from {totalRectangles} detected rectangles.";
            }

            RevitTaskDialog.Show("Batch Column Creation Complete", message);
        }

        /// <summary>
        /// Format dimension value for display (Revit internal units are feet)
        /// </summary>
        private string FormatDimension(double feetValue)
        {
            // Revit API works internally in feet, but we'll format appropriately
            // For small values, show more precision
            if (feetValue < 1.0)
            {
                return $"{feetValue:F3}";
            }
            else
            {
                return $"{feetValue:F2}";
            }
        }

        #endregion

        #region Helper Classes

        private class RectangularGroup
        {
            public List<DetailLine> Lines { get; set; }
            public RectangleAnalysis Analysis { get; set; }
        }

        private class RectangleAnalysis
        {
            public bool IsValid { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public XYZ CenterPoint { get; set; }
            public List<XYZ> CornerPoints { get; set; }
        }

        private class BatchProcessingResult
        {
            public int SuccessCount { get; set; }
            public List<ColumnInfo> CreatedColumns { get; set; } = new List<ColumnInfo>();
            public List<string> Failures { get; set; } = new List<string>();
        }

        private class ColumnInfo
        {
            public FamilyInstance Column { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public XYZ CenterPoint { get; set; }
        }

        private class DetailLineSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem) => elem is DetailLine;
            public bool AllowReference(Reference reference, XYZ position) => false;
        }

        #endregion
    }
}