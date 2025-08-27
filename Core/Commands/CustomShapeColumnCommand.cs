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
    /// Command for creating structural columns from user-defined profile curves
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class CustomShapeColumnCommand : IExternalCommand
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
                _logger.LogInfo("Starting Custom Shape Column Creation command");

                // Get profile curves from user
                var profileCurves = GetProfileCurvesFromUser(uidoc, doc);
                if (profileCurves == null || !profileCurves.Any())
                {
                    message = "Operation cancelled or no profile curves selected";
                    return Result.Cancelled;
                }

                // Analyze the profile
                var profileAnalysis = AnalyzeProfile(profileCurves);
                if (!profileAnalysis.IsValid)
                {
                    message = profileAnalysis.ErrorMessage;
                    return Result.Failed;
                }

                _logger.LogInfo($"Profile analyzed: {profileAnalysis.CurveCount} curves, center at ({profileAnalysis.CenterPoint.X:F2}, {profileAnalysis.CenterPoint.Y:F2})");

                // Get placement point from user
                var placementPoint = GetPlacementPointFromUser(uidoc, profileAnalysis.CenterPoint);
                if (placementPoint == null)
                {
                    message = "Operation cancelled - no placement point selected";
                    return Result.Cancelled;
                }

                // Get or create appropriate family symbol for custom shape
                var familySymbol = GetOrCreateCustomShapeFamilySymbol(doc, profileAnalysis);
                if (familySymbol == null)
                {
                    message = "Could not find or create appropriate custom shape column family symbol";
                    return Result.Failed;
                }

                // Get the level for column placement
                var level = GetColumnLevel(doc, placementPoint);
                if (level == null)
                {
                    message = "Could not determine appropriate level for column placement";
                    return Result.Failed;
                }

                // Create the custom shape column
                var columnResult = CreateCustomShapeColumn(doc, familySymbol, level, placementPoint, profileAnalysis);
                if (!columnResult.Success)
                {
                    message = columnResult.Message;
                    return Result.Failed;
                }

                // Show success message
                RevitTaskDialog.Show("Custom Shape Column Created Successfully", 
                    $"Custom shape column created at ({placementPoint.X:F2}, {placementPoint.Y:F2})\n" +
                    $"Profile: {profileAnalysis.CurveCount} curves\n" +
                    $"Bounds: {profileAnalysis.BoundingWidth:F3}' × {profileAnalysis.BoundingHeight:F3}'\n" +
                    $"Family: {familySymbol.Family.Name}\n" +
                    $"Symbol: {familySymbol.Name}\n" +
                    $"Level: {level.Name}");

                _logger.LogInfo("Custom Shape Column Creation command completed successfully");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CustomShapeColumnCommand");
                message = $"Unexpected error: {ex.Message}";
                return Result.Failed;
            }
        }

        #region Private Methods

        private List<Curve> GetProfileCurvesFromUser(UIDocument uidoc, Document doc)
        {
            try
            {
                RevitTaskDialog dialog = new RevitTaskDialog("Custom Shape Column Creation")
                {
                    MainContent = "Select the curves that define the column profile.\n\n" +
                                 "Requirements:\n" +
                                 "• Curves must form a closed profile\n" +
                                 "• Profile should be in plan view\n" +
                                 "• Curves can be lines, arcs, or splines\n\n" +
                                 "Select the profile curves now.",
                    CommonButtons = TaskDialogCommonButtons.Ok | TaskDialogCommonButtons.Cancel,
                    DefaultButton = TaskDialogResult.Ok
                };

                if (dialog.Show() != TaskDialogResult.Ok)
                {
                    return null;
                }

                // Let user select the profile curves
                var selectedRefs = uidoc.Selection.PickObjects(ObjectType.Element,
                    new CurveSelectionFilter(),
                    "Select curves that form the column profile (lines, arcs, detail lines, model lines):");

                if (!selectedRefs.Any())
                {
                    RevitTaskDialog.Show("No Selection", "No curves were selected.");
                    return null;
                }

                var curves = new List<Curve>();
                foreach (var reference in selectedRefs)
                {
                    var element = doc.GetElement(reference);
                    Curve curve = null;

                    if (element is DetailLine detailLine)
                    {
                        curve = detailLine.GeometryCurve;
                    }
                    else if (element is ModelLine modelLine)
                    {
                        curve = modelLine.GeometryCurve;
                    }
                    else if (element is CurveElement curveElement)
                    {
                        curve = curveElement.GeometryCurve;
                    }

                    if (curve != null)
                    {
                        curves.Add(curve);
                    }
                }

                _logger.LogInfo($"Selected {curves.Count} profile curves");
                return curves;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetProfileCurvesFromUser");
                return null;
            }
        }

        private ProfileAnalysis AnalyzeProfile(List<Curve> curves)
        {
            try
            {
                if (!curves.Any())
                {
                    return new ProfileAnalysis
                    {
                        IsValid = false,
                        ErrorMessage = "No curves provided for profile analysis"
                    };
                }

                // Get all endpoints
                var allPoints = new List<XYZ>();
                foreach (var curve in curves)
                {
                    allPoints.Add(curve.GetEndPoint(0));
                    allPoints.Add(curve.GetEndPoint(1));
                }

                // Calculate bounding box
                double minX = allPoints.Min(p => p.X);
                double minY = allPoints.Min(p => p.Y);
                double maxX = allPoints.Max(p => p.X);
                double maxY = allPoints.Max(p => p.Y);

                double boundingWidth = maxX - minX;
                double boundingHeight = maxY - minY;
                XYZ centerPoint = new XYZ((minX + maxX) / 2, (minY + maxY) / 2, allPoints[0].Z);

                // Basic validation
                if (boundingWidth < 0.01 || boundingHeight < 0.01)
                {
                    return new ProfileAnalysis
                    {
                        IsValid = false,
                        ErrorMessage = $"Profile is too small: {boundingWidth:F3}' × {boundingHeight:F3}'"
                    };
                }

                if (boundingWidth > 20.0 || boundingHeight > 20.0)
                {
                    return new ProfileAnalysis
                    {
                        IsValid = false,
                        ErrorMessage = $"Profile is too large: {boundingWidth:F3}' × {boundingHeight:F3}' (maximum 20' in each dimension)"
                    };
                }

                // Check for curve connectivity (simplified check)
                bool isConnected = CheckCurveConnectivity(curves);
                if (!isConnected)
                {
                    _logger.LogWarning("Profile curves may not form a closed loop - proceeding anyway");
                }

                return new ProfileAnalysis
                {
                    IsValid = true,
                    CurveCount = curves.Count,
                    BoundingWidth = boundingWidth,
                    BoundingHeight = boundingHeight,
                    CenterPoint = centerPoint,
                    ProfileCurves = curves,
                    IsClosedProfile = isConnected
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AnalyzeProfile");
                return new ProfileAnalysis
                {
                    IsValid = false,
                    ErrorMessage = $"Error analyzing profile: {ex.Message}"
                };
            }
        }

        private bool CheckCurveConnectivity(List<Curve> curves)
        {
            try
            {
                const double tolerance = 1e-6;
                
                // For each curve, check if its endpoints connect to other curves
                foreach (var curve in curves)
                {
                    var startPoint = curve.GetEndPoint(0);
                    var endPoint = curve.GetEndPoint(1);
                    
                    bool startConnected = false;
                    bool endConnected = false;
                    
                    foreach (var otherCurve in curves)
                    {
                        if (curve.Equals(otherCurve)) continue;
                        
                        var otherStart = otherCurve.GetEndPoint(0);
                        var otherEnd = otherCurve.GetEndPoint(1);
                        
                        if (startPoint.IsAlmostEqualTo(otherStart, tolerance) || startPoint.IsAlmostEqualTo(otherEnd, tolerance))
                            startConnected = true;
                        
                        if (endPoint.IsAlmostEqualTo(otherStart, tolerance) || endPoint.IsAlmostEqualTo(otherEnd, tolerance))
                            endConnected = true;
                    }
                    
                    if (!startConnected || !endConnected)
                        return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error checking curve connectivity: {ex.Message}");
                return false;
            }
        }

        private XYZ GetPlacementPointFromUser(UIDocument uidoc, XYZ suggestedPoint)
        {
            try
            {
                RevitTaskDialog dialog = new RevitTaskDialog("Column Placement")
                {
                    MainContent = $"Click to specify the placement point for the custom shape column.\n\n" +
                                 $"Suggested location (profile center): ({suggestedPoint.X:F2}, {suggestedPoint.Y:F2})",
                    CommonButtons = TaskDialogCommonButtons.Ok | TaskDialogCommonButtons.Cancel,
                    DefaultButton = TaskDialogResult.Ok
                };

                if (dialog.Show() != TaskDialogResult.Ok)
                {
                    return null;
                }

                // Let user pick a point
                var pickedPoint = uidoc.Selection.PickPoint("Click to specify the placement point for the custom shape column");
                _logger.LogInfo($"Placement point selected: ({pickedPoint.X:F2}, {pickedPoint.Y:F2}, {pickedPoint.Z:F2})");
                return pickedPoint;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPlacementPointFromUser");
                return null;
            }
        }

        private FamilySymbol GetOrCreateCustomShapeFamilySymbol(Document doc, ProfileAnalysis profileAnalysis)
        {
            try
            {
                _logger.LogInfo($"Finding or creating custom shape column family symbol");

                // Look for existing custom shape or generic column families
                var customFamilies = GetCustomShapeColumnFamilies();
                
                if (customFamilies.Any())
                {
                    // Use the first available custom shape family
                    var baseFamily = customFamilies.First();
                    var parameters = new ColumnParameters
                    {
                        Width = profileAnalysis.BoundingWidth,
                        Height = profileAnalysis.BoundingHeight,
                        FamilyName = baseFamily.Name,
                        SymbolName = $"Custom_{profileAnalysis.BoundingWidth:F1}x{profileAnalysis.BoundingHeight:F1}"
                    };
                    parameters.CustomParameters["Profile_Type"] = "Custom";
                    parameters.CustomParameters["Curve_Count"] = profileAnalysis.CurveCount;

                    var newSymbol = _familyManager.CreateCustomSymbol(baseFamily, parameters);
                    if (newSymbol != null)
                    {
                        _logger.LogInfo($"Created new custom shape symbol");
                        return newSymbol;
                    }
                }

                // Fallback: use any available column family
                var allFamilies = _familyManager.GetAvailableColumnFamilies();
                if (allFamilies.Any())
                {
                    var fallbackFamily = allFamilies.First();
                    var fallbackSymbol = fallbackFamily.GetFamilySymbolIds()
                        .Select(id => doc.GetElement(id) as FamilySymbol)
                        .FirstOrDefault(s => s != null);

                    if (fallbackSymbol != null)
                    {
                        if (!fallbackSymbol.IsActive)
                        {
                            using (var transaction = new Transaction(fallbackSymbol.Document, "Activate Fallback Custom Shape Column Symbol"))
                            {
                                transaction.Start();
                                fallbackSymbol.Activate();
                                transaction.Commit();
                            }
                        }
                        _logger.LogWarning($"Using fallback symbol for custom shape: {fallbackSymbol.Family.Name} - {fallbackSymbol.Name}");
                        return fallbackSymbol;
                    }
                }

                _logger.LogWarning("No suitable column families found for custom shape");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOrCreateCustomShapeFamilySymbol");
                return null;
            }
        }

        private List<Family> GetCustomShapeColumnFamilies()
        {
            try
            {
                var allColumnFamilies = _familyManager.GetAvailableColumnFamilies();
                var customFamilies = allColumnFamilies.Where(f => 
                    f.Name.ToLowerInvariant().Contains("custom") ||
                    f.Name.ToLowerInvariant().Contains("generic") ||
                    f.Name.ToLowerInvariant().Contains("profile") ||
                    f.Name.ToLowerInvariant().Contains("shape")).ToList();

                // If no specific custom families found, return all families
                if (!customFamilies.Any())
                {
                    customFamilies = allColumnFamilies;
                }

                _logger.LogInfo($"Found {customFamilies.Count} potential custom shape column families");
                return customFamilies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCustomShapeColumnFamilies");
                return new List<Family>();
            }
        }

        private Level GetColumnLevel(Document doc, XYZ placementPoint)
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

                // Find the level closest to the placement point elevation
                Level bestLevel = levels.First();
                double minDistance = Math.Abs(bestLevel.Elevation - placementPoint.Z);

                foreach (var level in levels)
                {
                    double distance = Math.Abs(level.Elevation - placementPoint.Z);
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

        private ProcessingResult CreateCustomShapeColumn(Document doc, FamilySymbol familySymbol, Level level, XYZ placementPoint, ProfileAnalysis profileAnalysis)
        {
            using (var transaction = new Transaction(doc, "Create Custom Shape Column"))
            {
                try
                {
                    transaction.Start();

                    // Create column at the specified placement point
                    FamilyInstance column = doc.Create.NewFamilyInstance(
                        new XYZ(placementPoint.X, placementPoint.Y, level.Elevation),
                        familySymbol,
                        level,
                        StructuralType.Column);

                    if (column == null)
                    {
                        transaction.RollBack();
                        return ProcessingResult.CreateFailure("Failed to create custom shape column instance");
                    }

                    doc.Regenerate();

                    // Set additional parameters
                    SetCustomShapeColumnParameters(column, profileAnalysis);

                    transaction.Commit();

                    _logger.LogInfo($"Custom shape column created successfully: Id {column.Id}");
                    return ProcessingResult.CreateSuccess(1, $"Custom shape column created at ({placementPoint.X:F2}, {placementPoint.Y:F2})");
                }
                catch (Exception ex)
                {
                    transaction.RollBack();
                    _logger.LogError(ex, "CreateCustomShapeColumn");
                    return ProcessingResult.CreateFailure($"Error creating custom shape column: {ex.Message}", ex);
                }
            }
        }

        private void SetCustomShapeColumnParameters(FamilyInstance column, ProfileAnalysis profileAnalysis)
        {
            try
            {
                // Set width and height parameters if available
                var widthNames = new[] { "b", "Width", "Depth", "d" };
                foreach (var widthName in widthNames)
                {
                    var param = column.LookupParameter(widthName);
                    if (param != null && !param.IsReadOnly)
                    {
                        param.Set(profileAnalysis.BoundingWidth);
                        _logger.LogInfo($"Set width parameter '{widthName}' to {profileAnalysis.BoundingWidth:F3}'");
                        break;
                    }
                }

                var heightNames = new[] { "h", "Height", "t" };
                foreach (var heightName in heightNames)
                {
                    var param = column.LookupParameter(heightName);
                    if (param != null && !param.IsReadOnly)
                    {
                        param.Set(profileAnalysis.BoundingHeight);
                        _logger.LogInfo($"Set height parameter '{heightName}' to {profileAnalysis.BoundingHeight:F3}'");
                        break;
                    }
                }

                // Set comments parameter with creation info
                var commentsParam = column.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                if (commentsParam != null && !commentsParam.IsReadOnly)
                {
                    string comment = $"Custom shape column created by Advanced Column tool - {DateTime.Now:yyyy-MM-dd HH:mm} - " +
                                   $"Profile: {profileAnalysis.CurveCount} curves, Bounds: {profileAnalysis.BoundingWidth:F3}' × {profileAnalysis.BoundingHeight:F3}'";
                    commentsParam.Set(comment);
                }

                _logger.LogInfo("Custom shape column parameters set successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not set custom shape column parameters: {ex.Message}");
            }
        }

        #endregion

        #region Helper Classes

        private class ProfileAnalysis
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; }
            public int CurveCount { get; set; }
            public double BoundingWidth { get; set; }
            public double BoundingHeight { get; set; }
            public XYZ CenterPoint { get; set; }
            public List<Curve> ProfileCurves { get; set; }
            public bool IsClosedProfile { get; set; }
        }

        private class CurveSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                return elem is DetailLine || elem is ModelLine || elem is CurveElement;
            }

            public bool AllowReference(Reference reference, XYZ position) => false;
        }

        #endregion
    }
}