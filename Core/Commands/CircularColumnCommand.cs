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
    /// Command for creating circular structural columns with center point and diameter specification
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class CircularColumnCommand : IExternalCommand
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
                _logger.LogInfo("Starting Circular Column Creation command");

                // Get center point from user
                var centerPoint = GetCenterPointFromUser(uidoc);
                if (centerPoint == null)
                {
                    message = "Operation cancelled - no center point selected";
                    return Result.Cancelled;
                }

                // Get diameter from user
                var diameter = GetDiameterFromUser();
                if (diameter <= 0)
                {
                    message = "Operation cancelled - invalid diameter";
                    return Result.Cancelled;
                }

                _logger.LogInfo($"Creating circular column at ({centerPoint.X:F2}, {centerPoint.Y:F2}) with diameter {diameter:F3}'");

                // Get or create appropriate circular column family symbol
                var familySymbol = GetOrCreateCircularFamilySymbol(doc, diameter);
                if (familySymbol == null)
                {
                    message = "Could not find or create appropriate circular column family symbol";
                    return Result.Failed;
                }

                // Get the level for column placement
                var level = GetColumnLevel(doc, centerPoint);
                if (level == null)
                {
                    message = "Could not determine appropriate level for column placement";
                    return Result.Failed;
                }

                // Create the circular column
                var columnResult = CreateCircularColumn(doc, familySymbol, level, centerPoint, diameter);
                if (!columnResult.Success)
                {
                    message = columnResult.Message;
                    return Result.Failed;
                }

                // Show success message
                RevitTaskDialog.Show("Circular Column Created Successfully", 
                    $"Circular column created at ({centerPoint.X:F2}, {centerPoint.Y:F2})\n" +
                    $"Diameter: {diameter:F3}'\n" +
                    $"Family: {familySymbol.Family.Name}\n" +
                    $"Symbol: {familySymbol.Name}\n" +
                    $"Level: {level.Name}");

                _logger.LogInfo("Circular Column Creation command completed successfully");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CircularColumnCommand");
                message = $"Unexpected error: {ex.Message}";
                return Result.Failed;
            }
        }

        #region Private Methods

        private XYZ GetCenterPointFromUser(UIDocument uidoc)
        {
            try
            {
                RevitTaskDialog dialog = new RevitTaskDialog("Circular Column Creation")
                {
                    MainContent = "Click to specify the center point for the circular column.",
                    CommonButtons = TaskDialogCommonButtons.Ok | TaskDialogCommonButtons.Cancel,
                    DefaultButton = TaskDialogResult.Ok
                };

                if (dialog.Show() != TaskDialogResult.Ok)
                {
                    return null;
                }

                // Let user pick a point
                var pickedPoint = uidoc.Selection.PickPoint("Click to specify the center point for the circular column");
                _logger.LogInfo($"Center point selected: ({pickedPoint.X:F2}, {pickedPoint.Y:F2}, {pickedPoint.Z:F2})");
                return pickedPoint;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCenterPointFromUser");
                return null;
            }
        }

        private double GetDiameterFromUser()
        {
            try
            {
                var dialog = new RevitTaskDialog("Circular Column Diameter")
                {
                    MainContent = "Enter the diameter for the circular column (in feet):",
                    MainInstruction = "Column Diameter",
                    CommonButtons = TaskDialogCommonButtons.Ok | TaskDialogCommonButtons.Cancel,
                    DefaultButton = TaskDialogResult.Ok,
                    ExpandedContent = "Common diameters:\n" +
                                   "• 1.0' (12 inches)\n" +
                                   "• 1.5' (18 inches)\n" +
                                   "• 2.0' (24 inches)\n" +
                                   "• 2.5' (30 inches)\n" +
                                   "• 3.0' (36 inches)"
                };

                // For simplicity, we'll use a predefined set of common diameters
                // In a real implementation, you might want to create a custom dialog
                var result = dialog.Show();
                if (result != TaskDialogResult.Ok)
                {
                    return -1;
                }

                // Show options dialog for diameter selection
                var diameterDialog = new RevitTaskDialog("Select Diameter")
                {
                    MainInstruction = "Select column diameter:",
                    CommonButtons = TaskDialogCommonButtons.Cancel
                };

                diameterDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "1.0 feet (12 inches)");
                diameterDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "1.5 feet (18 inches)");
                diameterDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "2.0 feet (24 inches)");
                diameterDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink4, "2.5 feet (30 inches)");
                // Remove the 5th option, use CommandLink4 for largest diameter
                diameterDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink4, "3.0 feet (36 inches)");

                var diameterResult = diameterDialog.Show();
                
                switch (diameterResult)
                {
                    case TaskDialogResult.CommandLink1: return 1.0;
                    case TaskDialogResult.CommandLink2: return 1.5;
                    case TaskDialogResult.CommandLink3: return 2.0;
                    case TaskDialogResult.CommandLink4: return 3.0;
                    default: return -1;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetDiameterFromUser");
                return -1;
            }
        }

        private FamilySymbol GetOrCreateCircularFamilySymbol(Document doc, double diameter)
        {
            try
            {
                _logger.LogInfo($"Finding or creating circular column family symbol for diameter {diameter:F3}'");

                // Look for existing circular column families
                var circularFamilies = GetCircularColumnFamilies();
                
                if (circularFamilies.Any())
                {
                    // Try to find existing symbol with matching diameter
                    foreach (var family in circularFamilies)
                    {
                        var symbols = family.GetFamilySymbolIds()
                            .Select(id => doc.GetElement(id) as FamilySymbol)
                            .Where(s => s != null);

                        foreach (var symbol in symbols)
                        {
                            if (TryGetCircularSymbolDiameter(symbol, out double symbolDiameter))
                            {
                                const double tolerance = 0.01; // 1/8 inch tolerance
                                if (Math.Abs(symbolDiameter - diameter) < tolerance)
                                {
                                    if (!symbol.IsActive)
                                    {
                                        using (var transaction = new Transaction(symbol.Document, "Activate Circular Column Symbol"))
                                        {
                                            transaction.Start();
                                            symbol.Activate();
                                            transaction.Commit();
                                        }
                                    }
                                    _logger.LogInfo($"Found existing circular symbol: {symbol.Family.Name} - {symbol.Name}");
                                    return symbol;
                                }
                            }
                        }
                    }

                    // Create new symbol with specified diameter
                    var baseFamily = circularFamilies.First();
                    var parameters = new ColumnParameters
                    {
                        Width = diameter,
                        Height = diameter,
                        FamilyName = baseFamily.Name,
                        SymbolName = $"D{diameter:F1}'"
                    };
                    parameters.CustomParameters["Diameter"] = diameter;
                    parameters.CustomParameters["d"] = diameter;

                    var newSymbol = _familyManager.CreateCustomSymbol(baseFamily, parameters);
                    if (newSymbol != null)
                    {
                        _logger.LogInfo($"Created new circular symbol for diameter {diameter:F3}'");
                        return newSymbol;
                    }
                }

                // Fallback: try to load standard circular column families
                _familyManager.LoadStandardColumnFamilies();
                
                // Try again after loading
                circularFamilies = GetCircularColumnFamilies();
                if (circularFamilies.Any())
                {
                    var fallbackFamily = circularFamilies.First();
                    var fallbackSymbol = fallbackFamily.GetFamilySymbolIds()
                        .Select(id => doc.GetElement(id) as FamilySymbol)
                        .FirstOrDefault(s => s != null);

                    if (fallbackSymbol != null)
                    {
                        if (!fallbackSymbol.IsActive)
                        {
                            using (var transaction = new Transaction(fallbackSymbol.Document, "Activate Fallback Circular Column Symbol"))
                            {
                                transaction.Start();
                                fallbackSymbol.Activate();
                                transaction.Commit();
                            }
                        }
                        _logger.LogWarning($"Using fallback circular symbol: {fallbackSymbol.Family.Name} - {fallbackSymbol.Name}");
                        return fallbackSymbol;
                    }
                }

                _logger.LogWarning("No circular column families found");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOrCreateCircularFamilySymbol");
                return null;
            }
        }

        private List<Family> GetCircularColumnFamilies()
        {
            try
            {
                var allColumnFamilies = _familyManager.GetAvailableColumnFamilies();
                var circularFamilies = allColumnFamilies.Where(f => 
                    f.Name.ToLowerInvariant().Contains("circular") ||
                    f.Name.ToLowerInvariant().Contains("round") ||
                    f.Name.ToLowerInvariant().Contains("pipe") ||
                    f.Name.ToLowerInvariant().Contains("tube")).ToList();

                _logger.LogInfo($"Found {circularFamilies.Count} circular column families");
                return circularFamilies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCircularColumnFamilies");
                return new List<Family>();
            }
        }

        private bool TryGetCircularSymbolDiameter(FamilySymbol symbol, out double diameter)
        {
            diameter = 0;

            try
            {
                // Common parameter names for circular column diameter
                var diameterNames = new[] { "Diameter", "d", "D", "Radius", "r" };

                foreach (var diameterName in diameterNames)
                {
                    var param = symbol.LookupParameter(diameterName);
                    if (param != null && param.HasValue)
                    {
                        diameter = param.AsDouble();
                        
                        // If it's radius, convert to diameter
                        if (diameterName.ToLowerInvariant().Contains("radius") || diameterName.ToLowerInvariant() == "r")
                        {
                            diameter *= 2;
                        }
                        
                        return diameter > 0;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not get diameter for circular symbol '{symbol.Name}': {ex.Message}");
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

        private ProcessingResult CreateCircularColumn(Document doc, FamilySymbol familySymbol, Level level, XYZ centerPoint, double diameter)
        {
            using (var transaction = new Transaction(doc, "Create Circular Column"))
            {
                try
                {
                    transaction.Start();

                    // Create column at the specified center point
                    FamilyInstance column = doc.Create.NewFamilyInstance(
                        new XYZ(centerPoint.X, centerPoint.Y, level.Elevation),
                        familySymbol,
                        level,
                        StructuralType.Column);

                    if (column == null)
                    {
                        transaction.RollBack();
                        return ProcessingResult.CreateFailure("Failed to create circular column instance");
                    }

                    doc.Regenerate();

                    // Set additional parameters
                    SetCircularColumnParameters(column, diameter);

                    transaction.Commit();

                    _logger.LogInfo($"Circular column created successfully: Id {column.Id}");
                    return ProcessingResult.CreateSuccess(1, $"Circular column created at ({centerPoint.X:F2}, {centerPoint.Y:F2}) with diameter {diameter:F3}'");
                }
                catch (Exception ex)
                {
                    transaction.RollBack();
                    _logger.LogError(ex, "CreateCircularColumn");
                    return ProcessingResult.CreateFailure($"Error creating circular column: {ex.Message}", ex);
                }
            }
        }

        private void SetCircularColumnParameters(FamilyInstance column, double diameter)
        {
            try
            {
                // Set diameter parameter if available
                var diameterNames = new[] { "Diameter", "d", "D" };
                foreach (var diameterName in diameterNames)
                {
                    var param = column.LookupParameter(diameterName);
                    if (param != null && !param.IsReadOnly)
                    {
                        param.Set(diameter);
                        _logger.LogInfo($"Set diameter parameter '{diameterName}' to {diameter:F3}'");
                        break;
                    }
                }

                // Set comments parameter with creation info
                var commentsParam = column.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                if (commentsParam != null && !commentsParam.IsReadOnly)
                {
                    string comment = $"Circular column created by Advanced Column tool - {DateTime.Now:yyyy-MM-dd HH:mm} - Diameter: {diameter:F3}'";
                    commentsParam.Set(comment);
                }

                _logger.LogInfo("Circular column parameters set successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not set circular column parameters: {ex.Message}");
            }
        }

        #endregion
    }
}