using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using RevitDtools.Core.Interfaces;
using RevitDtools.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitDtools.Core.Services
{
    /// <summary>
    /// Service for managing column schedule data integration and automatic parameter assignment
    /// </summary>
    public class ColumnScheduleService : BaseService
    {
        private readonly Dictionary<string, ColumnScheduleData> _scheduleCache;

        public ColumnScheduleService(Document document, ILogger logger) : base(document, logger)
        {
            _scheduleCache = new Dictionary<string, ColumnScheduleData>();
            InitializeScheduleData();
        }

        /// <summary>
        /// Apply schedule data to a column based on its properties
        /// </summary>
        public ProcessingResult ApplyScheduleData(FamilyInstance column, ColumnScheduleData scheduleData = null)
        {
            return ExecuteWithErrorHandling(() =>
            {
                if (column == null)
                {
                    return ProcessingResult.CreateFailure("Column instance is null");
                }

                // If no specific schedule data provided, try to find matching data
                if (scheduleData == null)
                {
                    scheduleData = FindMatchingScheduleData(column);
                }

                if (scheduleData == null)
                {
                    return ProcessingResult.CreateWarning("No matching schedule data found for column");
                }

                return ExecuteTransaction(transaction =>
                {
                    try
                    {
                        int parametersSet = 0;

                        // Set structural properties
                        if (SetStructuralProperties(column, scheduleData))
                            parametersSet++;

                        // Set material properties
                        if (SetMaterialProperties(column, scheduleData))
                            parametersSet++;

                        // Set dimensional properties
                        if (SetDimensionalProperties(column, scheduleData))
                            parametersSet++;

                        // Set identification properties
                        if (SetIdentificationProperties(column, scheduleData))
                            parametersSet++;

                        // Set custom properties
                        if (SetCustomProperties(column, scheduleData))
                            parametersSet++;

                        Document.Regenerate();

                        return ProcessingResult.CreateSuccess(parametersSet, 
                            $"Applied schedule data to column {column.Id}: {parametersSet} parameter groups updated");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "ApplyScheduleData transaction");
                        return ProcessingResult.CreateFailure($"Failed to apply schedule data: {ex.Message}", ex);
                    }
                }, "Apply Column Schedule Data");
            }, $"ApplyScheduleData for column {column?.Id}");
        }

        /// <summary>
        /// Get available schedule data entries
        /// </summary>
        public List<ColumnScheduleData> GetAvailableScheduleData()
        {
            try
            {
                return _scheduleCache.Values.ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetAvailableScheduleData");
                return new List<ColumnScheduleData>();
            }
        }

        /// <summary>
        /// Add or update schedule data entry
        /// </summary>
        public ProcessingResult AddOrUpdateScheduleData(ColumnScheduleData scheduleData)
        {
            return ExecuteWithErrorHandling(() =>
            {
                if (scheduleData == null || string.IsNullOrEmpty(scheduleData.ColumnMark))
                {
                    return ProcessingResult.CreateFailure("Invalid schedule data or missing column mark");
                }

                _scheduleCache[scheduleData.ColumnMark] = scheduleData;
                Logger.LogInfo($"Added/updated schedule data for column mark: {scheduleData.ColumnMark}");

                return ProcessingResult.CreateSuccess(1, $"Schedule data updated for column mark: {scheduleData.ColumnMark}");
            }, $"AddOrUpdateScheduleData for {scheduleData?.ColumnMark}");
        }

        /// <summary>
        /// Load schedule data from project schedules
        /// </summary>
        public ProcessingResult LoadScheduleDataFromProject()
        {
            return ExecuteWithErrorHandling(() =>
            {
                var schedules = new FilteredElementCollector(Document)
                    .OfClass(typeof(ViewSchedule))
                    .Cast<ViewSchedule>()
                    .Where(s => s.Name.ToLowerInvariant().Contains("column"))
                    .ToList();

                int loadedCount = 0;
                foreach (var schedule in schedules)
                {
                    try
                    {
                        var scheduleData = ExtractScheduleData(schedule);
                        loadedCount += scheduleData.Count;
                        
                        foreach (var data in scheduleData)
                        {
                            _scheduleCache[data.ColumnMark] = data;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Could not load data from schedule '{schedule.Name}': {ex.Message}");
                    }
                }

                return ProcessingResult.CreateSuccess(loadedCount, 
                    $"Loaded {loadedCount} schedule data entries from {schedules.Count} column schedules");
            }, "LoadScheduleDataFromProject");
        }

        #region Private Methods

        private void InitializeScheduleData()
        {
            try
            {
                // Initialize with common column schedule data
                var commonScheduleData = new List<ColumnScheduleData>
                {
                    new ColumnScheduleData
                    {
                        ColumnMark = "C1",
                        Width = 1.0,
                        Height = 1.0,
                        Material = "Concrete",
                        StructuralUsage = "Column",
                        LoadBearing = true,
                        FireRating = "2 Hour",
                        Comments = "Standard rectangular column"
                    },
                    new ColumnScheduleData
                    {
                        ColumnMark = "C2",
                        Width = 1.5,
                        Height = 1.5,
                        Material = "Concrete",
                        StructuralUsage = "Column",
                        LoadBearing = true,
                        FireRating = "2 Hour",
                        Comments = "Large rectangular column"
                    },
                    new ColumnScheduleData
                    {
                        ColumnMark = "C3",
                        Width = 2.0,
                        Height = 1.0,
                        Material = "Steel",
                        StructuralUsage = "Column",
                        LoadBearing = true,
                        FireRating = "1 Hour",
                        Comments = "Steel wide flange column"
                    }
                };

                foreach (var data in commonScheduleData)
                {
                    _scheduleCache[data.ColumnMark] = data;
                }

                Logger.LogInfo($"Initialized {commonScheduleData.Count} default schedule data entries");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "InitializeScheduleData");
            }
        }

        private ColumnScheduleData FindMatchingScheduleData(FamilyInstance column)
        {
            try
            {
                // Try to match by mark parameter
                var markParam = column.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                if (markParam != null && markParam.HasValue)
                {
                    string mark = markParam.AsString();
                    if (!string.IsNullOrEmpty(mark) && _scheduleCache.ContainsKey(mark))
                    {
                        Logger.LogInfo($"Found schedule data for column mark: {mark}");
                        return _scheduleCache[mark];
                    }
                }

                // Try to match by dimensions
                if (TryGetColumnDimensions(column, out double width, out double height))
                {
                    const double tolerance = 0.1; // 1.2 inch tolerance
                    
                    var matchingData = _scheduleCache.Values.FirstOrDefault(data =>
                        Math.Abs(data.Width - width) < tolerance &&
                        Math.Abs(data.Height - height) < tolerance);

                    if (matchingData != null)
                    {
                        Logger.LogInfo($"Found schedule data by dimensions: {width:F1}' x {height:F1}' -> {matchingData.ColumnMark}");
                        return matchingData;
                    }
                }

                // Fallback to first available data
                if (_scheduleCache.Any())
                {
                    var fallbackData = _scheduleCache.Values.First();
                    Logger.LogWarning($"Using fallback schedule data: {fallbackData.ColumnMark}");
                    return fallbackData;
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "FindMatchingScheduleData");
                return null;
            }
        }

        private bool TryGetColumnDimensions(FamilyInstance column, out double width, out double height)
        {
            width = 0;
            height = 0;

            try
            {
                // Try common parameter names for width and height
                var widthNames = new[] { "b", "Width", "Depth", "d" };
                var heightNames = new[] { "h", "Height", "t" };

                foreach (var widthName in widthNames)
                {
                    var widthParam = column.LookupParameter(widthName);
                    if (widthParam != null && widthParam.HasValue)
                    {
                        width = widthParam.AsDouble();
                        break;
                    }
                }

                foreach (var heightName in heightNames)
                {
                    var heightParam = column.LookupParameter(heightName);
                    if (heightParam != null && heightParam.HasValue)
                    {
                        height = heightParam.AsDouble();
                        break;
                    }
                }

                return width > 0 && height > 0;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not get column dimensions: {ex.Message}");
                return false;
            }
        }

        private bool SetStructuralProperties(FamilyInstance column, ColumnScheduleData scheduleData)
        {
            try
            {
                bool anySet = false;

                // Set structural usage (if parameter exists)
                var structuralUsageParam = column.LookupParameter("Structural Usage");
                if (structuralUsageParam != null && !structuralUsageParam.IsReadOnly && !string.IsNullOrEmpty(scheduleData.StructuralUsage))
                {
                    structuralUsageParam.Set(scheduleData.StructuralUsage);
                    anySet = true;
                }

                // Set load bearing property (if parameter exists)
                var loadBearingParam = column.LookupParameter("Load Bearing");
                if (loadBearingParam != null && !loadBearingParam.IsReadOnly)
                {
                    loadBearingParam.Set(scheduleData.LoadBearing ? 1 : 0);
                    anySet = true;
                }

                return anySet;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not set structural properties: {ex.Message}");
                return false;
            }
        }

        private bool SetMaterialProperties(FamilyInstance column, ColumnScheduleData scheduleData)
        {
            try
            {
                if (string.IsNullOrEmpty(scheduleData.Material))
                    return false;

                // Try to find and set material
                var materials = new FilteredElementCollector(Document)
                    .OfClass(typeof(Material))
                    .Cast<Material>()
                    .Where(m => m.Name.ToLowerInvariant().Contains(scheduleData.Material.ToLowerInvariant()))
                    .ToList();

                if (materials.Any())
                {
                    var material = materials.First();
                    var materialParam = column.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM);
                    if (materialParam != null && !materialParam.IsReadOnly)
                    {
                        materialParam.Set(material.Id);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not set material properties: {ex.Message}");
                return false;
            }
        }

        private bool SetDimensionalProperties(FamilyInstance column, ColumnScheduleData scheduleData)
        {
            try
            {
                bool anySet = false;

                // Set width
                var widthNames = new[] { "b", "Width", "Depth", "d" };
                foreach (var widthName in widthNames)
                {
                    var param = column.LookupParameter(widthName);
                    if (param != null && !param.IsReadOnly)
                    {
                        param.Set(scheduleData.Width);
                        anySet = true;
                        break;
                    }
                }

                // Set height
                var heightNames = new[] { "h", "Height", "t" };
                foreach (var heightName in heightNames)
                {
                    var param = column.LookupParameter(heightName);
                    if (param != null && !param.IsReadOnly)
                    {
                        param.Set(scheduleData.Height);
                        anySet = true;
                        break;
                    }
                }

                return anySet;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not set dimensional properties: {ex.Message}");
                return false;
            }
        }

        private bool SetIdentificationProperties(FamilyInstance column, ColumnScheduleData scheduleData)
        {
            try
            {
                bool anySet = false;

                // Set mark
                if (!string.IsNullOrEmpty(scheduleData.ColumnMark))
                {
                    var markParam = column.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                    if (markParam != null && !markParam.IsReadOnly)
                    {
                        markParam.Set(scheduleData.ColumnMark);
                        anySet = true;
                    }
                }

                // Set comments
                if (!string.IsNullOrEmpty(scheduleData.Comments))
                {
                    var commentsParam = column.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                    if (commentsParam != null && !commentsParam.IsReadOnly)
                    {
                        commentsParam.Set(scheduleData.Comments);
                        anySet = true;
                    }
                }

                return anySet;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not set identification properties: {ex.Message}");
                return false;
            }
        }

        private bool SetCustomProperties(FamilyInstance column, ColumnScheduleData scheduleData)
        {
            try
            {
                bool anySet = false;

                // Set fire rating
                if (!string.IsNullOrEmpty(scheduleData.FireRating))
                {
                    var fireRatingParam = column.LookupParameter("Fire Rating");
                    if (fireRatingParam != null && !fireRatingParam.IsReadOnly)
                    {
                        fireRatingParam.Set(scheduleData.FireRating);
                        anySet = true;
                    }
                }

                // Set any custom parameters from the schedule data
                foreach (var customParam in scheduleData.CustomParameters)
                {
                    var param = column.LookupParameter(customParam.Key);
                    if (param != null && !param.IsReadOnly)
                    {
                        try
                        {
                            if (customParam.Value is double doubleValue)
                                param.Set(doubleValue);
                            else if (customParam.Value is int intValue)
                                param.Set(intValue);
                            else if (customParam.Value is string stringValue)
                                param.Set(stringValue);
                            
                            anySet = true;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning($"Could not set custom parameter '{customParam.Key}': {ex.Message}");
                        }
                    }
                }

                return anySet;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not set custom properties: {ex.Message}");
                return false;
            }
        }

        private List<ColumnScheduleData> ExtractScheduleData(ViewSchedule schedule)
        {
            var scheduleData = new List<ColumnScheduleData>();

            try
            {
                // This is a simplified implementation
                // In a real scenario, you would need to parse the schedule table data
                // For now, we'll just log that we found a schedule
                Logger.LogInfo($"Found column schedule: {schedule.Name}");
                
                // TODO: Implement actual schedule data extraction
                // This would involve reading the schedule table data and parsing it
                
                return scheduleData;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"ExtractScheduleData from {schedule.Name}");
                return scheduleData;
            }
        }

        #endregion
    }

    /// <summary>
    /// Data structure for column schedule information
    /// </summary>
    public class ColumnScheduleData
    {
        public string ColumnMark { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string Material { get; set; }
        public string StructuralUsage { get; set; }
        public bool LoadBearing { get; set; }
        public string FireRating { get; set; }
        public string Comments { get; set; }
        public Dictionary<string, object> CustomParameters { get; set; } = new Dictionary<string, object>();
    }
}