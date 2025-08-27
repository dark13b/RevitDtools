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
    /// Enhanced command for creating structural columns from rectangular detail lines
    /// Uses dynamic family management for automatic family creation and reuse
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class EnhancedColumnByLineCommand : IExternalCommand
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
                _logger.LogInfo("Starting Enhanced Column By Line command");

                // Get detail lines from selection or user input
                var detailLines = GetDetailLinesFromUser(uidoc, doc);
                if (detailLines == null || detailLines.Count != 4)
                {
                    message = "Operation cancelled or invalid selection";
                    return Result.Cancelled;
                }

                // Analyze the rectangle formed by the detail lines
                var rectangleAnalysis = AnalyzeRectangle(detailLines);
                if (!rectangleAnalysis.IsValid)
                {
                    message = rectangleAnalysis.ErrorMessage;
                    return Result.Failed;
                }

                _logger.LogInfo($"Rectangle analyzed: {rectangleAnalysis.Width:F3}' x {rectangleAnalysis.Height:F3}' at ({rectangleAnalysis.CenterPoint.X:F2}, {rectangleAnalysis.CenterPoint.Y:F2})");

                // Get or create appropriate family symbol
                var familySymbol = GetOrCreateFamilySymbol(rectangleAnalysis.Width, rectangleAnalysis.Height);
                if (familySymbol == null)
                {
                    message = "Could not find or create appropriate column family symbol";
                    return Result.Failed;
                }

                // Get the level for column placement
                var level = GetColumnLevel(doc, rectangleAnalysis.CenterPoint);
                if (level == null)
                {
                    message = "Could not determine appropriate level for column placement";
                    return Result.Failed;
                }

                // Create the column
                var columnResult = CreateColumn(doc, familySymbol, level, rectangleAnalysis);
                if (!columnResult.Success)
                {
                    message = columnResult.Message;
                    return Result.Failed;
                }

                // Show success message
                RevitTaskDialog.Show("Column Created Successfully", 
                    $"Column created at ({rectangleAnalysis.CenterPoint.X:F2}, {rectangleAnalysis.CenterPoint.Y:F2})\n" +
                    $"Dimensions: {rectangleAnalysis.Width:F3}' × {rectangleAnalysis.Height:F3}'\n" +
                    $"Family: {familySymbol.Family.Name}\n" +
                    $"Symbol: {familySymbol.Name}\n" +
                    $"Level: {level.Name}");

                _logger.LogInfo("Enhanced Column By Line command completed successfully");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EnhancedColumnByLineCommand");
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

                // If we don't have exactly 4 lines selected, let user select them
                if (detailLines.Count != 4)
                {
                    RevitTaskDialog dialog = new RevitTaskDialog("Enhanced Column by Line")
                    {
                        MainContent = $"Current selection: {detailLines.Count} detail lines.\n\n" +
                                     "This tool requires exactly 4 detail lines that form a rectangle.\n\n" +
                                     "The tool will automatically:\n" +
                                     "• Find or create the appropriate column family\n" +
                                     "• Set the correct dimensions\n" +
                                     "• Reuse existing families when possible\n\n" +
                                     "Would you like to select the lines now?",
                        CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.Cancel,
                        DefaultButton = TaskDialogResult.Yes
                    };

                    if (dialog.Show() != TaskDialogResult.Yes)
                    {
                        return null;
                    }

                    // Let user select the detail lines
                    var selectedRefs = uidoc.Selection.PickObjects(ObjectType.Element,
                        new DetailLineSelectionFilter(),
                        "Select exactly 4 detail lines that form a rectangle:");

                    if (selectedRefs.Count != 4)
                    {
                        RevitTaskDialog.Show("Invalid Selection", 
                            $"You selected {selectedRefs.Count} lines. Please select exactly 4 detail lines.");
                        return null;
                    }

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

        private RectangleAnalysis AnalyzeRectangle(List<DetailLine> detailLines)
        {
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
                    return new RectangleAnalysis
                    {
                        IsValid = false,
                        ErrorMessage = $"The selected lines do not form a simple closed rectangle.\n\n" +
                                     $"Analysis:\n" +
                                     $"• Found {distinctPoints.Count} unique corner points (need exactly 4)\n" +
                                     $"• Total line endpoints: {allPoints.Count}\n\n" +
                                     "Make sure:\n" +
                                     "• Lines connect at corners to form a closed rectangle\n" +
                                     "• No overlapping or duplicate lines\n" +
                                     "• Lines are properly connected end-to-end"
                    };
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
                    return new RectangleAnalysis
                    {
                        IsValid = false,
                        ErrorMessage = $"The rectangle is too small: {width:F3}' × {height:F3}'"
                    };
                }

                // Validate reasonable dimensions
                if (width > 10.0 || height > 10.0)
                {
                    return new RectangleAnalysis
                    {
                        IsValid = false,
                        ErrorMessage = $"The rectangle is too large: {width:F3}' × {height:F3}' (maximum 10' in each dimension)"
                    };
                }

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
                return new RectangleAnalysis
                {
                    IsValid = false,
                    ErrorMessage = $"Error analyzing rectangle: {ex.Message}"
                };
            }
        }

        private FamilySymbol GetOrCreateFamilySymbol(double width, double height)
        {
            try
            {
                _logger.LogInfo($"Finding or creating family symbol for dimensions {width:F3}' x {height:F3}'");

                // Use the family manager to find or create the symbol
                var symbol = _familyManager.FindOrCreateSymbol(width, height);
                
                if (symbol != null)
                {
                    // Ensure the symbol is active
                    if (!symbol.IsActive)
                    {
                        using (var transaction = new Transaction(symbol.Document, "Activate Column Symbol"))
                        {
                            transaction.Start();
                            symbol.Activate();
                            transaction.Commit();
                        }
                    }

                    _logger.LogInfo($"Using family symbol: {symbol.Family.Name} - {symbol.Name}");
                    return symbol;
                }

                _logger.LogWarning("Could not find or create appropriate family symbol");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOrCreateFamilySymbol");
                return null;
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
                {
                    _logger.LogWarning("No levels found in project");
                    return null;
                }

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

                _logger.LogInfo($"Selected level: {bestLevel.Name} (elevation: {bestLevel.Elevation:F2}')");
                return bestLevel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetColumnLevel");
                return null;
            }
        }

        private ProcessingResult CreateColumn(Document doc, FamilySymbol familySymbol, Level level, RectangleAnalysis rectangleAnalysis)
        {
            using (var transaction = new Transaction(doc, "Create Enhanced Column"))
            {
                try
                {
                    transaction.Start();

                    // Create column at the calculated center point
                    FamilyInstance column = doc.Create.NewFamilyInstance(
                        new XYZ(rectangleAnalysis.CenterPoint.X, rectangleAnalysis.CenterPoint.Y, level.Elevation),
                        familySymbol,
                        level,
                        StructuralType.Column);

                    if (column == null)
                    {
                        transaction.RollBack();
                        return ProcessingResult.CreateFailure("Failed to create column instance");
                    }

                    doc.Regenerate();

                    // Try to set additional parameters if available
                    SetAdditionalColumnParameters(column, rectangleAnalysis);

                    transaction.Commit();

                    _logger.LogInfo($"Column created successfully: Id {column.Id}");
                    return ProcessingResult.CreateSuccess(1, $"Column created at ({rectangleAnalysis.CenterPoint.X:F2}, {rectangleAnalysis.CenterPoint.Y:F2})");
                }
                catch (Exception ex)
                {
                    transaction.RollBack();
                    _logger.LogError(ex, "CreateColumn");
                    return ProcessingResult.CreateFailure($"Error creating column: {ex.Message}", ex);
                }
            }
        }

        private void SetAdditionalColumnParameters(FamilyInstance column, RectangleAnalysis rectangleAnalysis)
        {
            try
            {
                // Note: Structural usage parameter setting removed due to API compatibility issues

                // Set comments parameter with creation info
                var commentsParam = column.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                if (commentsParam != null && !commentsParam.IsReadOnly)
                {
                    string comment = $"Created by Enhanced Column By Line tool - {DateTime.Now:yyyy-MM-dd HH:mm}";
                    commentsParam.Set(comment);
                }

                _logger.LogInfo("Additional column parameters set successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not set additional column parameters: {ex.Message}");
            }
        }

        #endregion

        #region Helper Classes

        private class RectangleAnalysis
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public XYZ CenterPoint { get; set; }
            public List<XYZ> CornerPoints { get; set; }
        }

        private class DetailLineSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem) => elem is DetailLine;
            public bool AllowReference(Reference reference, XYZ position) => false;
        }

        #endregion
    }
}