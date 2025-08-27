using Autodesk.Revit.DB;
using RevitDtools.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RevitDtools.Utilities
{
    /// <summary>
    /// Helper class for validating inputs and system state
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// Validate that a document is ready for processing
        /// </summary>
        public static ValidationResult ValidateDocument(Document document)
        {
            if (document == null)
            {
                return ValidationResult.Failure("Document is null");
            }

            if (document.IsReadOnly)
            {
                return ValidationResult.Failure("Document is read-only and cannot be modified");
            }

            if (document.ActiveView == null)
            {
                return ValidationResult.Failure("No active view available");
            }

            if (!(document.ActiveView is ViewPlan || 
                  document.ActiveView is ViewSection || 
                  document.ActiveView is ViewDrafting))
            {
                return ValidationResult.Failure("Active view must be a Plan, Section, or Drafting view");
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validate geometry for processing
        /// </summary>
        public static ValidationResult ValidateGeometry(GeometryObject geometry)
        {
            if (geometry == null)
            {
                return ValidationResult.Failure("Geometry object is null");
            }

            if (geometry is Curve curve)
            {
                if (!curve.IsBound)
                {
                    return ValidationResult.Failure("Curve is not bound");
                }

                if (curve.Length < 1e-6)
                {
                    return ValidationResult.Failure("Curve is too short to process");
                }
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validate column parameters
        /// </summary>
        public static ValidationResult ValidateColumnParameters(ColumnParameters parameters)
        {
            if (parameters == null)
            {
                return ValidationResult.Failure("Column parameters are null");
            }

            if (parameters.Width <= 0)
            {
                return ValidationResult.Failure("Column width must be greater than zero");
            }

            if (parameters.Height <= 0)
            {
                return ValidationResult.Failure("Column height must be greater than zero");
            }

            if (parameters.Width > 10.0) // 10 feet
            {
                return ValidationResult.Failure("Column width exceeds maximum allowed size (10 feet)");
            }

            if (parameters.Height > 10.0) // 10 feet
            {
                return ValidationResult.Failure("Column height exceeds maximum allowed size (10 feet)");
            }

            if (parameters.Width < 0.01) // 0.01 feet (about 1/8 inch)
            {
                return ValidationResult.Failure("Column width is too small (minimum 0.01 feet)");
            }

            if (parameters.Height < 0.01)
            {
                return ValidationResult.Failure("Column height is too small (minimum 0.01 feet)");
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validate file path for batch processing
        /// </summary>
        public static ValidationResult ValidateFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return ValidationResult.Failure("File path is empty");
            }

            if (!File.Exists(filePath))
            {
                return ValidationResult.Failure($"File does not exist: {filePath}");
            }

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (extension != ".dwg")
            {
                return ValidationResult.Failure($"Unsupported file type: {extension}. Only .dwg files are supported.");
            }

            try
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length == 0)
                {
                    return ValidationResult.Failure("File is empty");
                }

                if (fileInfo.Length > 100 * 1024 * 1024) // 100 MB
                {
                    return ValidationResult.Warning($"File is very large ({fileInfo.Length / (1024 * 1024)} MB). Processing may take a long time.");
                }
            }
            catch (Exception ex)
            {
                return ValidationResult.Failure($"Cannot access file information: {ex.Message}");
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validate folder path for batch processing
        /// </summary>
        public static ValidationResult ValidateFolderPath(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return ValidationResult.Failure("Folder path is empty");
            }

            if (!Directory.Exists(folderPath))
            {
                return ValidationResult.Failure($"Folder does not exist: {folderPath}");
            }

            try
            {
                var dwgFiles = Directory.GetFiles(folderPath, "*.dwg", SearchOption.AllDirectories);
                if (dwgFiles.Length == 0)
                {
                    return ValidationResult.Warning("No DWG files found in the specified folder");
                }

                if (dwgFiles.Length > 100)
                {
                    return ValidationResult.Warning($"Large number of files found ({dwgFiles.Length}). Processing may take a long time.");
                }
            }
            catch (Exception ex)
            {
                return ValidationResult.Failure($"Cannot access folder: {ex.Message}");
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validate family symbol for column creation
        /// </summary>
        public static ValidationResult ValidateFamilySymbol(FamilySymbol familySymbol)
        {
            if (familySymbol == null)
            {
                return ValidationResult.Failure("Family symbol is null");
            }

            if (!familySymbol.IsValidObject)
            {
                return ValidationResult.Failure("Family symbol is not valid");
            }

            if (familySymbol.Category?.Id != new ElementId(BuiltInCategory.OST_StructuralColumns))
            {
                return ValidationResult.Failure("Family symbol is not a structural column family");
            }

            if (!familySymbol.IsActive)
            {
                return ValidationResult.Warning("Family symbol is not active and will be activated automatically");
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validate detail lines form a rectangle
        /// </summary>
        public static ValidationResult ValidateRectangleLines(List<DetailLine> detailLines)
        {
            if (detailLines == null || detailLines.Count != 4)
            {
                return ValidationResult.Failure($"Expected 4 detail lines, got {detailLines?.Count ?? 0}");
            }

            try
            {
                // Get all endpoints
                var allPoints = new List<XYZ>();
                foreach (var line in detailLines)
                {
                    if (line?.GeometryCurve == null)
                    {
                        return ValidationResult.Failure("One or more detail lines have invalid geometry");
                    }

                    var curve = line.GeometryCurve;
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
                    return ValidationResult.Failure($"Lines do not form a simple rectangle. Found {distinctPoints.Count} unique corner points (expected 4)");
                }

                // Check if points form a rectangle
                var minX = distinctPoints.Min(p => p.X);
                var maxX = distinctPoints.Max(p => p.X);
                var minY = distinctPoints.Min(p => p.Y);
                var maxY = distinctPoints.Max(p => p.Y);

                var width = maxX - minX;
                var height = maxY - minY;

                if (width < tolerance || height < tolerance)
                {
                    return ValidationResult.Failure($"Rectangle is too small: {width:F6}' × {height:F6}'");
                }

                // Verify all points lie on the rectangle boundary
                foreach (var point in distinctPoints)
                {
                    bool onBoundary = (Math.Abs(point.X - minX) < tolerance || Math.Abs(point.X - maxX) < tolerance) &&
                                     (Math.Abs(point.Y - minY) < tolerance || Math.Abs(point.Y - maxY) < tolerance);
                    
                    if (!onBoundary)
                    {
                        return ValidationResult.Failure("Lines do not form a proper rectangle");
                    }
                }

                return ValidationResult.Success($"Valid rectangle: {width:F3}' × {height:F3}'");
            }
            catch (Exception ex)
            {
                return ValidationResult.Failure($"Error validating rectangle: {ex.Message}");
            }
        }

        /// <summary>
        /// Validate user settings
        /// </summary>
        public static ValidationResult ValidateUserSettings(UserSettings settings)
        {
            if (settings == null)
            {
                return ValidationResult.Failure("Settings object is null");
            }

            var issues = new List<string>();

            // Validate column settings
            if (settings.ColumnSettings != null)
            {
                if (settings.ColumnSettings.MinimumColumnSize <= 0)
                {
                    issues.Add("Minimum column size must be greater than zero");
                }

                if (settings.ColumnSettings.MaximumColumnSize <= settings.ColumnSettings.MinimumColumnSize)
                {
                    issues.Add("Maximum column size must be greater than minimum column size");
                }
            }

            // Validate logging settings
            if (settings.LoggingSettings != null)
            {
                if (settings.LoggingSettings.MaxLogEntries <= 0)
                {
                    issues.Add("Maximum log entries must be greater than zero");
                }

                if (settings.LoggingSettings.LogRetentionDays <= 0)
                {
                    issues.Add("Log retention days must be greater than zero");
                }
            }

            // Validate batch settings
            if (settings.BatchSettings != null)
            {
                if (settings.BatchSettings.MaxConcurrentFiles <= 0)
                {
                    issues.Add("Maximum concurrent files must be greater than zero");
                }
            }

            if (issues.Any())
            {
                return ValidationResult.Failure($"Settings validation failed: {string.Join("; ", issues)}");
            }

            return ValidationResult.Success();
        }
    }

    /// <summary>
    /// Result of a validation operation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public bool IsWarning { get; set; }
        public string Message { get; set; }

        public static ValidationResult Success(string message = null)
        {
            return new ValidationResult { IsValid = true, Message = message };
        }

        public static ValidationResult Warning(string message)
        {
            return new ValidationResult { IsValid = true, IsWarning = true, Message = message };
        }

        public static ValidationResult Failure(string message)
        {
            return new ValidationResult { IsValid = false, Message = message };
        }
    }
}