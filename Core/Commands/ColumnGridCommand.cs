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
    /// Command for creating column grids with pattern definition
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class ColumnGridCommand : IExternalCommand
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
                _logger.LogInfo("Starting Column Grid Generation command");

                // Get grid parameters from user
                var gridParameters = GetGridParametersFromUser();
                if (gridParameters == null)
                {
                    message = "Operation cancelled - no grid parameters specified";
                    return Result.Cancelled;
                }

                // Get origin point from user
                var originPoint = GetOriginPointFromUser(uidoc);
                if (originPoint == null)
                {
                    message = "Operation cancelled - no origin point selected";
                    return Result.Cancelled;
                }

                _logger.LogInfo($"Creating column grid: {gridParameters.ColumnsX}x{gridParameters.ColumnsY} at ({originPoint.X:F2}, {originPoint.Y:F2})");

                // Get or create appropriate family symbol
                var familySymbol = GetOrCreateGridFamilySymbol(gridParameters);
                if (familySymbol == null)
                {
                    message = "Could not find or create appropriate column family symbol for grid";
                    return Result.Failed;
                }

                // Get the level for column placement
                var level = GetColumnLevel(doc, originPoint);
                if (level == null)
                {
                    message = "Could not determine appropriate level for column placement";
                    return Result.Failed;
                }

                // Create the column grid
                var gridResult = CreateColumnGrid(doc, familySymbol, level, originPoint, gridParameters);
                if (!gridResult.Success)
                {
                    message = gridResult.Message;
                    return Result.Failed;
                }

                // Show success message
                RevitTaskDialog.Show("Column Grid Created Successfully", 
                    $"Column grid created successfully!\n\n" +
                    $"Grid: {gridParameters.ColumnsX} × {gridParameters.ColumnsY} columns\n" +
                    $"Spacing: {gridParameters.SpacingX:F1}' × {gridParameters.SpacingY:F1}'\n" +
                    $"Origin: ({originPoint.X:F2}, {originPoint.Y:F2})\n" +
                    $"Total columns: {gridParameters.ColumnsX * gridParameters.ColumnsY}\n" +
                    $"Family: {familySymbol.Family.Name}\n" +
                    $"Symbol: {familySymbol.Name}\n" +
                    $"Level: {level.Name}");

                _logger.LogInfo("Column Grid Generation command completed successfully");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ColumnGridCommand");
                message = $"Unexpected error: {ex.Message}";
                return Result.Failed;
            }
        }

        #region Private Methods

        private ColumnGridParameters GetGridParametersFromUser()
        {
            try
            {
                // Show grid configuration dialog
                var dialog = new RevitTaskDialog("Column Grid Configuration")
                {
                    MainInstruction = "Configure Column Grid",
                    MainContent = "Select a predefined grid configuration:",
                    CommonButtons = TaskDialogCommonButtons.Cancel
                };

                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "3×3 Grid - 20' spacing", "9 columns in a 3×3 pattern with 20' spacing");
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "4×4 Grid - 25' spacing", "16 columns in a 4×4 pattern with 25' spacing");
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "5×3 Grid - 30' spacing", "15 columns in a 5×3 pattern with 30' spacing");
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink4, "6×4 Grid - 20' spacing", "24 columns in a 6×4 pattern with 20' spacing");
                // Remove the 5th option and use CommandLink4 for custom configuration
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink4, "Custom Configuration", "Specify custom grid dimensions and spacing");

                var result = dialog.Show();
                
                switch (result)
                {
                    case TaskDialogResult.CommandLink1:
                        return new ColumnGridParameters { ColumnsX = 3, ColumnsY = 3, SpacingX = 20.0, SpacingY = 20.0, ColumnWidth = 1.0, ColumnHeight = 1.0 };
                    case TaskDialogResult.CommandLink2:
                        return new ColumnGridParameters { ColumnsX = 4, ColumnsY = 4, SpacingX = 25.0, SpacingY = 25.0, ColumnWidth = 1.0, ColumnHeight = 1.0 };
                    case TaskDialogResult.CommandLink3:
                        return new ColumnGridParameters { ColumnsX = 5, ColumnsY = 3, SpacingX = 30.0, SpacingY = 30.0, ColumnWidth = 1.0, ColumnHeight = 1.0 };
                    case TaskDialogResult.CommandLink4:
                        return GetCustomGridParameters();
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetGridParametersFromUser");
                return null;
            }
        }

        private ColumnGridParameters GetCustomGridParameters()
        {
            try
            {
                // For simplicity, we'll provide a few custom options
                // In a real implementation, you might want to create a custom WPF dialog
                var dialog = new RevitTaskDialog("Custom Grid Configuration")
                {
                    MainInstruction = "Select Custom Grid Configuration",
                    MainContent = "Choose from these custom configurations:",
                    CommonButtons = TaskDialogCommonButtons.Cancel
                };

                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "2×8 Grid - 15' × 25' spacing", "Linear arrangement for long buildings");
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "8×2 Grid - 25' × 15' spacing", "Linear arrangement for wide buildings");
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "3×5 Grid - 18' × 22' spacing", "Rectangular grid with different spacings");
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink4, "7×7 Grid - 15' spacing", "Large square grid with tight spacing");

                var result = dialog.Show();
                
                switch (result)
                {
                    case TaskDialogResult.CommandLink1:
                        return new ColumnGridParameters { ColumnsX = 2, ColumnsY = 8, SpacingX = 15.0, SpacingY = 25.0, ColumnWidth = 1.0, ColumnHeight = 1.0 };
                    case TaskDialogResult.CommandLink2:
                        return new ColumnGridParameters { ColumnsX = 8, ColumnsY = 2, SpacingX = 25.0, SpacingY = 15.0, ColumnWidth = 1.0, ColumnHeight = 1.0 };
                    case TaskDialogResult.CommandLink3:
                        return new ColumnGridParameters { ColumnsX = 3, ColumnsY = 5, SpacingX = 18.0, SpacingY = 22.0, ColumnWidth = 1.0, ColumnHeight = 1.0 };
                    case TaskDialogResult.CommandLink4:
                        return new ColumnGridParameters { ColumnsX = 7, ColumnsY = 7, SpacingX = 15.0, SpacingY = 15.0, ColumnWidth = 1.0, ColumnHeight = 1.0 };
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCustomGridParameters");
                return null;
            }
        }

        private XYZ GetOriginPointFromUser(UIDocument uidoc)
        {
            try
            {
                RevitTaskDialog dialog = new RevitTaskDialog("Grid Origin Point")
                {
                    MainContent = "Click to specify the origin point for the column grid.\n\n" +
                                 "The origin point will be the location of the first column (bottom-left corner of the grid).",
                    CommonButtons = TaskDialogCommonButtons.Ok | TaskDialogCommonButtons.Cancel,
                    DefaultButton = TaskDialogResult.Ok
                };

                if (dialog.Show() != TaskDialogResult.Ok)
                {
                    return null;
                }

                // Let user pick a point
                var pickedPoint = uidoc.Selection.PickPoint("Click to specify the origin point for the column grid");
                _logger.LogInfo($"Origin point selected: ({pickedPoint.X:F2}, {pickedPoint.Y:F2}, {pickedPoint.Z:F2})");
                return pickedPoint;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOriginPointFromUser");
                return null;
            }
        }

        private FamilySymbol GetOrCreateGridFamilySymbol(ColumnGridParameters gridParameters)
        {
            try
            {
                _logger.LogInfo($"Finding or creating family symbol for grid columns: {gridParameters.ColumnWidth:F3}' x {gridParameters.ColumnHeight:F3}'");

                // Use the family manager to find or create the symbol
                var symbol = _familyManager.FindOrCreateSymbol(gridParameters.ColumnWidth, gridParameters.ColumnHeight);
                
                if (symbol != null)
                {
                    // Ensure the symbol is active
                    if (!symbol.IsActive)
                    {
                        using (var transaction = new Transaction(symbol.Document, "Activate Grid Column Symbol"))
                        {
                            transaction.Start();
                            symbol.Activate();
                            transaction.Commit();
                        }
                    }

                    _logger.LogInfo($"Using family symbol for grid: {symbol.Family.Name} - {symbol.Name}");
                    return symbol;
                }

                _logger.LogWarning("Could not find or create appropriate family symbol for grid");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOrCreateGridFamilySymbol");
                return null;
            }
        }

        private Level GetColumnLevel(Document doc, XYZ originPoint)
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

                // Find the level closest to the origin point elevation
                Level bestLevel = levels.First();
                double minDistance = Math.Abs(bestLevel.Elevation - originPoint.Z);

                foreach (var level in levels)
                {
                    double distance = Math.Abs(level.Elevation - originPoint.Z);
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

        private ProcessingResult CreateColumnGrid(Document doc, FamilySymbol familySymbol, Level level, XYZ originPoint, ColumnGridParameters gridParameters)
        {
            using (var transaction = new Transaction(doc, "Create Column Grid"))
            {
                try
                {
                    transaction.Start();

                    var createdColumns = new List<FamilyInstance>();
                    int totalColumns = gridParameters.ColumnsX * gridParameters.ColumnsY;
                    int createdCount = 0;

                    // Create columns in grid pattern
                    for (int x = 0; x < gridParameters.ColumnsX; x++)
                    {
                        for (int y = 0; y < gridParameters.ColumnsY; y++)
                        {
                            // Calculate column position
                            double columnX = originPoint.X + (x * gridParameters.SpacingX);
                            double columnY = originPoint.Y + (y * gridParameters.SpacingY);
                            XYZ columnPosition = new XYZ(columnX, columnY, level.Elevation);

                            // Create column
                            FamilyInstance column = doc.Create.NewFamilyInstance(
                                columnPosition,
                                familySymbol,
                                level,
                                StructuralType.Column);

                            if (column != null)
                            {
                                createdColumns.Add(column);
                                createdCount++;

                                // Set grid-specific parameters
                                SetGridColumnParameters(column, x, y, gridParameters);
                            }
                            else
                            {
                                _logger.LogWarning($"Failed to create column at position ({x}, {y})");
                            }
                        }
                    }

                    doc.Regenerate();

                    if (createdCount == 0)
                    {
                        transaction.RollBack();
                        return ProcessingResult.CreateFailure("Failed to create any columns in the grid");
                    }

                    transaction.Commit();

                    string message = createdCount == totalColumns 
                        ? $"Successfully created all {totalColumns} columns in {gridParameters.ColumnsX}×{gridParameters.ColumnsY} grid"
                        : $"Created {createdCount} of {totalColumns} columns in {gridParameters.ColumnsX}×{gridParameters.ColumnsY} grid";

                    _logger.LogInfo($"Column grid created: {createdCount} columns");
                    return ProcessingResult.CreateSuccess(createdCount, message);
                }
                catch (Exception ex)
                {
                    transaction.RollBack();
                    _logger.LogError(ex, "CreateColumnGrid");
                    return ProcessingResult.CreateFailure($"Error creating column grid: {ex.Message}", ex);
                }
            }
        }

        private void SetGridColumnParameters(FamilyInstance column, int gridX, int gridY, ColumnGridParameters gridParameters)
        {
            try
            {
                // Set comments parameter with grid info
                var commentsParam = column.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                if (commentsParam != null && !commentsParam.IsReadOnly)
                {
                    string comment = $"Grid column ({gridX + 1}, {gridY + 1}) - Created by Column Grid tool - {DateTime.Now:yyyy-MM-dd HH:mm}";
                    commentsParam.Set(comment);
                }

                // Set mark parameter with grid position
                var markParam = column.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                if (markParam != null && !markParam.IsReadOnly)
                {
                    string mark = $"C{gridX + 1}{(char)('A' + gridY)}"; // e.g., C1A, C1B, C2A, etc.
                    markParam.Set(mark);
                }

                _logger.LogInfo($"Set parameters for grid column at ({gridX}, {gridY})");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not set grid column parameters for position ({gridX}, {gridY}): {ex.Message}");
            }
        }

        #endregion

        #region Helper Classes

        private class ColumnGridParameters
        {
            public int ColumnsX { get; set; }
            public int ColumnsY { get; set; }
            public double SpacingX { get; set; }
            public double SpacingY { get; set; }
            public double ColumnWidth { get; set; }
            public double ColumnHeight { get; set; }
        }

        #endregion
    }
}