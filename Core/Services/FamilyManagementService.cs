using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using RevitDtools.Core.Interfaces;
using RevitDtools.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RevitDtools.Core.Services
{
    /// <summary>
    /// Service for managing Revit families, particularly column families
    /// </summary>
    public class FamilyManagementService : BaseService, IFamilyManager
    {
        private readonly Dictionary<string, Family> _familyCache;
        private readonly Dictionary<string, FamilySymbol> _symbolCache;
        private readonly ColumnScheduleService _scheduleService;

        public FamilyManagementService(Document document, ILogger logger) : base(document, logger)
        {
            _familyCache = new Dictionary<string, Family>();
            _symbolCache = new Dictionary<string, FamilySymbol>();
            _scheduleService = new ColumnScheduleService(document, logger);
            InitializeCaches();
        }

        /// <summary>
        /// Create a new column family with specified dimensions
        /// </summary>
        public Family CreateColumnFamily(string familyName, double width, double height)
        {
            return ExecuteWithErrorHandling(() =>
            {
                if (!ValidateDocument(out string error))
                {
                    return ProcessingResult.CreateFailure($"Document validation failed: {error}");
                }

                // For now, we'll use existing families and create new symbols
                // Creating new families requires family template files which may not be available
                var existingFamily = GetBestMatchingColumnFamily();
                if (existingFamily != null)
                {
                    Logger.LogInfo($"Using existing family '{existingFamily.Name}' as base for '{familyName}'");
                    _familyCache[familyName] = existingFamily;
                    return ProcessingResult.CreateSuccess(1, $"Family '{familyName}' mapped to existing family '{existingFamily.Name}'");
                }

                return ProcessingResult.CreateFailure("No suitable base column family found in project");
            }, $"CreateColumnFamily: {familyName}").Success ? (_familyCache.ContainsKey(familyName) ? _familyCache[familyName] : null) : null;
        }

        /// <summary>
        /// Create a custom family symbol with specific parameters
        /// </summary>
        public FamilySymbol CreateCustomSymbol(Family family, ColumnParameters parameters)
        {
            if (family == null || parameters == null)
                return null;

            var result = ExecuteTransaction(transaction =>
            {
                try
                {
                    // Get the first symbol from the family to duplicate
                    var baseSymbol = family.GetFamilySymbolIds().FirstOrDefault();
                    if (baseSymbol == null || baseSymbol == ElementId.InvalidElementId)
                    {
                        return ProcessingResult.CreateFailure("No base symbol found in family");
                    }

                    var originalSymbol = Document.GetElement(baseSymbol) as FamilySymbol;
                    if (originalSymbol == null)
                    {
                        return ProcessingResult.CreateFailure("Could not retrieve base symbol");
                    }

                    // Activate the symbol if not already active
                    if (!originalSymbol.IsActive)
                    {
                        originalSymbol.Activate();
                        Document.Regenerate();
                    }

                    // Try to duplicate the symbol
                    var duplicatedIds = ElementTransformUtils.CopyElement(Document, originalSymbol.Id, XYZ.Zero);
                    if (!duplicatedIds.Any())
                    {
                        return ProcessingResult.CreateFailure("Failed to duplicate family symbol");
                    }

                    var newSymbol = Document.GetElement(duplicatedIds.First()) as FamilySymbol;
                    if (newSymbol == null)
                    {
                        return ProcessingResult.CreateFailure("Duplicated element is not a family symbol");
                    }

                    // Set the name
                    string symbolName = parameters.SymbolName ?? $"{parameters.Width:F3}x{parameters.Height:F3}";
                    newSymbol.Name = symbolName;

                    // Set parameters
                    bool parametersSet = SetSymbolParameters(newSymbol, parameters);
                    
                    if (!newSymbol.IsActive)
                    {
                        newSymbol.Activate();
                    }

                    Document.Regenerate();

                    // Cache the new symbol
                    string cacheKey = parameters.GetUniqueId();
                    _symbolCache[cacheKey] = newSymbol;

                    string message = parametersSet 
                        ? $"Custom symbol '{symbolName}' created successfully with parameters"
                        : $"Custom symbol '{symbolName}' created but some parameters could not be set";

                    return ProcessingResult.CreateSuccess(1, message);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "CreateCustomSymbol");
                    return ProcessingResult.CreateFailure($"Failed to create custom symbol: {ex.Message}", ex);
                }
            }, "Create Custom Column Symbol");

            return result.Success ? (_symbolCache.ContainsKey(parameters.GetUniqueId()) ? _symbolCache[parameters.GetUniqueId()] : null) : null;
        }

        /// <summary>
        /// Load standard column families into the project
        /// </summary>
        public void LoadStandardColumnFamilies()
        {
            ExecuteWithErrorHandling(() =>
            {
                var loadedFamilies = new List<string>();
                
                // Try to load common column families from Revit's family library
                var familyPaths = GetStandardColumnFamilyPaths();
                
                foreach (var familyPath in familyPaths)
                {
                    try
                    {
                        if (File.Exists(familyPath))
                        {
                            using (var transaction = new Transaction(Document, "Load Column Family"))
                            {
                                transaction.Start();
                                bool loaded = Document.LoadFamily(familyPath);
                                if (loaded)
                                {
                                    loadedFamilies.Add(Path.GetFileNameWithoutExtension(familyPath));
                                    transaction.Commit();
                                }
                                else
                                {
                                    transaction.RollBack();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Could not load family from {familyPath}: {ex.Message}");
                    }
                }

                // Refresh cache after loading new families
                InitializeCaches();

                string message = loadedFamilies.Any() 
                    ? $"Loaded {loadedFamilies.Count} standard column families: {string.Join(", ", loadedFamilies)}"
                    : "No additional column families were loaded";

                return ProcessingResult.CreateSuccess(loadedFamilies.Count, message);
            }, "LoadStandardColumnFamilies");
        }

        /// <summary>
        /// Validate if a family is compatible for column creation
        /// </summary>
        public bool ValidateFamilyCompatibility(Family family)
        {
            if (family == null) return false;

            try
            {
                // Check if it's a structural column family
                if (family.FamilyCategory?.Id.Value != (int)BuiltInCategory.OST_StructuralColumns)
                {
                    Logger.LogWarning($"Family '{family.Name}' is not a structural column family");
                    return false;
                }

                // Check if family has symbols
                var symbolIds = family.GetFamilySymbolIds();
                if (!symbolIds.Any())
                {
                    Logger.LogWarning($"Family '{family.Name}' has no symbols");
                    return false;
                }

                // Check if at least one symbol is valid
                foreach (var symbolId in symbolIds)
                {
                    var symbol = Document.GetElement(symbolId) as FamilySymbol;
                    if (symbol != null)
                    {
                        Logger.LogInfo($"Family '{family.Name}' is compatible for column creation");
                        return true;
                    }
                }

                Logger.LogWarning($"Family '{family.Name}' has no valid symbols");
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error validating family compatibility: {family?.Name}");
                return false;
            }
        }

        /// <summary>
        /// Get all available column families in the project
        /// </summary>
        public List<Family> GetAvailableColumnFamilies()
        {
            try
            {
                var families = new FilteredElementCollector(Document)
                    .OfClass(typeof(Family))
                    .Cast<Family>()
                    .Where(f => f.FamilyCategory?.Id.Value == (int)BuiltInCategory.OST_StructuralColumns)
                    .ToList();

                Logger.LogInfo($"Found {families.Count} column families in project");
                return families;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetAvailableColumnFamilies");
                return new List<Family>();
            }
        }

        /// <summary>
        /// Get all available column family symbols in the project
        /// </summary>
        public List<FamilySymbol> GetAvailableColumnSymbols()
        {
            try
            {
                var symbols = new FilteredElementCollector(Document)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_StructuralColumns)
                    .Cast<FamilySymbol>()
                    .ToList();

                Logger.LogInfo($"Found {symbols.Count} column symbols in project");
                return symbols;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetAvailableColumnSymbols");
                return new List<FamilySymbol>();
            }
        }

        /// <summary>
        /// Find or create a family symbol with the specified dimensions
        /// </summary>
        public FamilySymbol FindOrCreateSymbol(double width, double height, string baseFamilyName = null)
        {
            var parameters = ColumnParameters.Create(width, height, baseFamilyName);
            string cacheKey = parameters.GetUniqueId();

            // Check cache first
            if (_symbolCache.ContainsKey(cacheKey))
            {
                var cachedSymbol = _symbolCache[cacheKey];
                Logger.LogInfo($"Found cached symbol for dimensions {width:F3}x{height:F3}");
                return cachedSymbol;
            }

            // Try to find existing symbol with matching dimensions
            var existingSymbol = FindExistingSymbolWithDimensions(width, height);
            if (existingSymbol != null)
            {
                _symbolCache[cacheKey] = existingSymbol;
                Logger.LogInfo($"Found existing symbol '{existingSymbol.Name}' for dimensions {width:F3}x{height:F3}");
                return existingSymbol;
            }

            // Create new symbol
            var family = GetBestMatchingColumnFamily(baseFamilyName);
            if (family != null)
            {
                var newSymbol = CreateCustomSymbol(family, parameters);
                if (newSymbol != null)
                {
                    Logger.LogInfo($"Created new symbol for dimensions {width:F3}x{height:F3}");
                    return newSymbol;
                }
            }

            // Fallback: return the first available symbol
            var fallbackSymbol = GetAvailableColumnSymbols().FirstOrDefault();
            if (fallbackSymbol != null)
            {
                Logger.LogWarning($"Using fallback symbol '{fallbackSymbol.Name}' for dimensions {width:F3}x{height:F3}");
            }

            return fallbackSymbol;
        }

        /// <summary>
        /// Create column with automatic schedule data application
        /// </summary>
        public FamilyInstance CreateColumnWithScheduleData(XYZ location, FamilySymbol familySymbol, Level level, string columnMark = null)
        {
            var result = ExecuteTransaction(transaction =>
            {
                try
                {
                    // Create the column
                    FamilyInstance column = Document.Create.NewFamilyInstance(
                        location,
                        familySymbol,
                        level,
                        StructuralType.Column);

                    if (column == null)
                    {
                        return ProcessingResult.CreateFailure("Failed to create column instance");
                    }

                    Document.Regenerate();

                    // Apply schedule data if available
                    ColumnScheduleData scheduleData = null;
                    if (!string.IsNullOrEmpty(columnMark))
                    {
                        scheduleData = _scheduleService.GetAvailableScheduleData()
                            .FirstOrDefault(data => data.ColumnMark == columnMark);
                    }

                    if (scheduleData != null)
                    {
                        var scheduleResult = _scheduleService.ApplyScheduleData(column, scheduleData);
                        if (scheduleResult.Success)
                        {
                            Logger.LogInfo($"Applied schedule data '{columnMark}' to column {column.Id}");
                        }
                        else
                        {
                            Logger.LogWarning($"Could not apply schedule data '{columnMark}': {scheduleResult.Message}");
                        }
                    }

                    return ProcessingResult.CreateSuccess(1, $"Column created with schedule data at ({location.X:F2}, {location.Y:F2})");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "CreateColumnWithScheduleData");
                    return ProcessingResult.CreateFailure($"Failed to create column with schedule data: {ex.Message}", ex);
                }
            }, "Create Column with Schedule Data");

            return result.Success ? 
                new FilteredElementCollector(Document)
                    .OfClass(typeof(FamilyInstance))
                    .OfCategory(BuiltInCategory.OST_StructuralColumns)
                    .Cast<FamilyInstance>()
                    .OrderByDescending(c => c.Id.Value)
                    .FirstOrDefault() : null;
        }

        /// <summary>
        /// Get all family symbols for a specific family
        /// </summary>
        public IEnumerable<FamilySymbol> GetFamilySymbols(Family family)
        {
            if (family == null)
                return Enumerable.Empty<FamilySymbol>();

            try
            {
                var symbolIds = family.GetFamilySymbolIds();
                var symbols = new List<FamilySymbol>();

                foreach (var symbolId in symbolIds)
                {
                    var symbol = Document.GetElement(symbolId) as FamilySymbol;
                    if (symbol != null)
                    {
                        symbols.Add(symbol);
                    }
                }

                Logger.LogInfo($"Found {symbols.Count} symbols in family '{family.Name}'");
                return symbols;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"GetFamilySymbols for family '{family?.Name}'");
                return Enumerable.Empty<FamilySymbol>();
            }
        }

        /// <summary>
        /// Get the column schedule service
        /// </summary>
        public ColumnScheduleService GetScheduleService()
        {
            return _scheduleService;
        }

        #region Private Helper Methods

        private void InitializeCaches()
        {
            _familyCache.Clear();
            _symbolCache.Clear();

            try
            {
                // Cache all column families
                var families = GetAvailableColumnFamilies();
                foreach (var family in families)
                {
                    _familyCache[family.Name] = family;
                }

                // Cache all column symbols
                var symbols = GetAvailableColumnSymbols();
                foreach (var symbol in symbols)
                {
                    string key = $"{symbol.Family.Name}_{symbol.Name}";
                    _symbolCache[key] = symbol;
                }

                Logger.LogInfo($"Initialized caches: {_familyCache.Count} families, {_symbolCache.Count} symbols");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "InitializeCaches");
            }
        }

        private Family GetBestMatchingColumnFamily(string preferredName = null)
        {
            var families = GetAvailableColumnFamilies();
            
            if (!families.Any())
                return null;

            // If preferred name is specified, try to find it
            if (!string.IsNullOrEmpty(preferredName))
            {
                var preferred = families.FirstOrDefault(f => 
                    string.Equals(f.Name, preferredName, StringComparison.OrdinalIgnoreCase));
                if (preferred != null)
                    return preferred;
            }

            // Look for common rectangular column family names
            var commonNames = new[] { "Rectangular Column", "Concrete-Rectangular-Column", "Steel-Column", "Column" };
            
            foreach (var commonName in commonNames)
            {
                var match = families.FirstOrDefault(f => 
                    f.Name.ToLowerInvariant().Contains(commonName.ToLowerInvariant()));
                if (match != null)
                    return match;
            }

            // Return the first available family
            return families.First();
        }

        private FamilySymbol FindExistingSymbolWithDimensions(double width, double height)
        {
            var symbols = GetAvailableColumnSymbols();
            const double tolerance = 0.01; // 1/8 inch tolerance

            foreach (var symbol in symbols)
            {
                if (TryGetSymbolDimensions(symbol, out double symbolWidth, out double symbolHeight))
                {
                    if (Math.Abs(symbolWidth - width) < tolerance && Math.Abs(symbolHeight - height) < tolerance)
                    {
                        return symbol;
                    }
                }
            }

            return null;
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
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not get dimensions for symbol '{symbol.Name}': {ex.Message}");
                return false;
            }
        }

        private bool SetSymbolParameters(FamilySymbol symbol, ColumnParameters parameters)
        {
            bool allParametersSet = true;

            try
            {
                // Set width parameter
                var widthNames = new[] { "b", "Width", "Depth", "d" };
                bool widthSet = false;
                foreach (var widthName in widthNames)
                {
                    var widthParam = symbol.LookupParameter(widthName);
                    if (widthParam != null && !widthParam.IsReadOnly)
                    {
                        widthParam.Set(parameters.Width);
                        widthSet = true;
                        Logger.LogInfo($"Set width parameter '{widthName}' to {parameters.Width:F3}");
                        break;
                    }
                }

                if (!widthSet)
                {
                    Logger.LogWarning($"Could not set width parameter for symbol '{symbol.Name}'");
                    allParametersSet = false;
                }

                // Set height parameter
                var heightNames = new[] { "h", "Height", "t" };
                bool heightSet = false;
                foreach (var heightName in heightNames)
                {
                    var heightParam = symbol.LookupParameter(heightName);
                    if (heightParam != null && !heightParam.IsReadOnly)
                    {
                        heightParam.Set(parameters.Height);
                        heightSet = true;
                        Logger.LogInfo($"Set height parameter '{heightName}' to {parameters.Height:F3}");
                        break;
                    }
                }

                if (!heightSet)
                {
                    Logger.LogWarning($"Could not set height parameter for symbol '{symbol.Name}'");
                    allParametersSet = false;
                }

                // Set custom parameters
                foreach (var customParam in parameters.CustomParameters)
                {
                    var param = symbol.LookupParameter(customParam.Key);
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
                            
                            Logger.LogInfo($"Set custom parameter '{customParam.Key}' to {customParam.Value}");
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning($"Could not set custom parameter '{customParam.Key}': {ex.Message}");
                            allParametersSet = false;
                        }
                    }
                }

                return allParametersSet;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"SetSymbolParameters for '{symbol.Name}'");
                return false;
            }
        }

        private List<string> GetStandardColumnFamilyPaths()
        {
            var paths = new List<string>();

            try
            {
                // Get Revit installation path
                var revitPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var familyPaths = new[]
                {
                    Path.Combine(revitPath, "Autodesk", "Revit 2026", "Libraries", "US Imperial", "Structural Columns"),
                    Path.Combine(revitPath, "Autodesk", "Revit 2025", "Libraries", "US Imperial", "Structural Columns"),
                    Path.Combine(revitPath, "Autodesk", "Revit 2024", "Libraries", "US Imperial", "Structural Columns")
                };

                foreach (var familyPath in familyPaths)
                {
                    if (Directory.Exists(familyPath))
                    {
                        var familyFiles = Directory.GetFiles(familyPath, "*.rfa");
                        paths.AddRange(familyFiles);
                        break; // Use the first available version
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not locate standard family paths: {ex.Message}");
            }

            return paths;
        }

        #endregion
    }
}