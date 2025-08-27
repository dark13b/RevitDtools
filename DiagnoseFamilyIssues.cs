using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitDtools.Core.Services;
using RevitDtools.Utilities;
using System;
using System.Linq;
using System.Text;

namespace RevitDtools
{
    /// <summary>
    /// Diagnostic tool to help understand why batch column creation is failing
    /// </summary>
    [Transaction(TransactionMode.ReadOnly)]
    public class DiagnoseFamilyIssues : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                var logger = Logger.Instance;
                var familyService = new FamilyManagementService(doc, logger);

                var report = new StringBuilder();
                report.AppendLine("=== FAMILY DIAGNOSTIC REPORT ===\n");

                // 1. Check available column families
                var families = familyService.GetAvailableColumnFamilies();
                report.AppendLine($"📁 AVAILABLE COLUMN FAMILIES ({families.Count}):");
                if (families.Any())
                {
                    foreach (var family in families)
                    {
                        report.AppendLine($"  • {family.Name} (ID: {family.Id})");
                    }
                }
                else
                {
                    report.AppendLine("  ❌ NO COLUMN FAMILIES FOUND!");
                    report.AppendLine("  This is likely why batch processing is failing.");
                }
                report.AppendLine();

                // 2. Check available column symbols
                var symbols = familyService.GetAvailableColumnSymbols();
                report.AppendLine($"🔧 AVAILABLE COLUMN SYMBOLS ({symbols.Count}):");
                if (symbols.Any())
                {
                    foreach (var symbol in symbols.Take(10)) // Show first 10
                    {
                        report.AppendLine($"  • {symbol.Family.Name} - {symbol.Name} (Active: {symbol.IsActive})");
                        
                        // Try to get dimensions
                        if (TryGetSymbolDimensions(symbol, out double width, out double height))
                        {
                            report.AppendLine($"    Dimensions: {width:F3}' × {height:F3}'");
                        }
                        else
                        {
                            report.AppendLine($"    Dimensions: Could not determine");
                        }
                    }
                    if (symbols.Count > 10)
                    {
                        report.AppendLine($"  ... and {symbols.Count - 10} more symbols");
                    }
                }
                else
                {
                    report.AppendLine("  ❌ NO COLUMN SYMBOLS FOUND!");
                }
                report.AppendLine();

                // 3. Test specific dimensions that failed
                var failedDimensions = new[]
                {
                    new { Width = 2.625, Height = 1.640 },
                    new { Width = 1.969, Height = 1.640 }
                };

                report.AppendLine("🔍 TESTING FAILED DIMENSIONS:");
                foreach (var dim in failedDimensions)
                {
                    report.AppendLine($"  Testing {dim.Width:F3}' × {dim.Height:F3}':");
                    
                    var symbol = familyService.FindOrCreateSymbol(dim.Width, dim.Height);
                    if (symbol != null)
                    {
                        report.AppendLine($"    ✅ Found/Created: {symbol.Family.Name} - {symbol.Name}");
                    }
                    else
                    {
                        report.AppendLine($"    ❌ FAILED to find or create symbol");
                        
                        // Try to understand why
                        if (!families.Any())
                        {
                            report.AppendLine($"    Reason: No column families available");
                        }
                        else
                        {
                            report.AppendLine($"    Reason: Symbol creation failed (check parameters)");
                        }
                    }
                }
                report.AppendLine();

                // 4. Check if we can load standard families
                report.AppendLine("📦 ATTEMPTING TO LOAD STANDARD FAMILIES:");
                try
                {
                    familyService.LoadStandardColumnFamilies();
                    var newFamilies = familyService.GetAvailableColumnFamilies();
                    report.AppendLine($"  After loading: {newFamilies.Count} families available");
                }
                catch (Exception ex)
                {
                    report.AppendLine($"  ❌ Failed to load standard families: {ex.Message}");
                }
                report.AppendLine();

                // 5. Recommendations
                report.AppendLine("💡 RECOMMENDATIONS:");
                if (!families.Any())
                {
                    report.AppendLine("  1. Load column families into your project:");
                    report.AppendLine("     - Insert > Load Family > Browse to Revit family library");
                    report.AppendLine("     - Look for Structural Columns folder");
                    report.AppendLine("     - Load 'Concrete-Rectangular-Column.rfa' or similar");
                    report.AppendLine();
                    report.AppendLine("  2. Or create a simple rectangular column family");
                    report.AppendLine("     - File > New > Family > Structural Column template");
                    report.AppendLine("     - Add width (b) and height (h) parameters");
                    report.AppendLine("     - Load into project");
                }
                else if (!symbols.Any())
                {
                    report.AppendLine("  1. Check that loaded families have valid symbols");
                    report.AppendLine("  2. Activate symbols if they're not active");
                }
                else
                {
                    report.AppendLine("  1. The issue might be with parameter names in your families");
                    report.AppendLine("  2. Check that your column families have 'b', 'Width', 'h', or 'Height' parameters");
                    report.AppendLine("  3. Make sure these parameters are not read-only");
                }

                // Show the report
                Autodesk.Revit.UI.TaskDialog.Show("Family Diagnostic Report", report.ToString());

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Diagnostic failed: {ex.Message}";
                return Result.Failed;
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
    }
}