using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitDtools.Core.Services;
using RevitDtools.Utilities;
using System;
using System.IO;
using System.Linq;

namespace RevitDtools
{
    /// <summary>
    /// Command to load standard column families into the project
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class LoadStandardColumnFamilies : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                var logger = Logger.Instance;
                var familyService = new FamilyManagementService(doc, logger);

                // Check current state
                var currentFamilies = familyService.GetAvailableColumnFamilies();
                var currentSymbols = familyService.GetAvailableColumnSymbols();

                string initialMessage = $"Current state:\n";
                initialMessage += $"• Column families: {currentFamilies.Count}\n";
                initialMessage += $"• Column symbols: {currentSymbols.Count}\n\n";
                initialMessage += "This will attempt to load standard Revit column families.\n\n";
                initialMessage += "Continue?";

                var confirmResult = Autodesk.Revit.UI.TaskDialog.Show("Load Standard Column Families", initialMessage,
                    TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);

                if (confirmResult != TaskDialogResult.Yes)
                {
                    return Result.Cancelled;
                }

                // Try to load standard families
                using (var transaction = new Transaction(doc, "Load Standard Column Families"))
                {
                    transaction.Start();

                    int loadedCount = 0;
                    var loadedFamilies = new System.Collections.Generic.List<string>();

                    // Try common family paths
                    var familyPaths = GetStandardColumnFamilyPaths();
                    
                    foreach (var familyPath in familyPaths)
                    {
                        try
                        {
                            if (File.Exists(familyPath))
                            {
                                bool loaded = doc.LoadFamily(familyPath);
                                if (loaded)
                                {
                                    string familyName = Path.GetFileNameWithoutExtension(familyPath);
                                    loadedFamilies.Add(familyName);
                                    loadedCount++;
                                    Logger.LogInfo($"Loaded family: {familyName}", "LoadStandardColumnFamilies");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning($"Could not load family from {familyPath}: {ex.Message}", "LoadStandardColumnFamilies");
                        }
                    }

                    // If no families were loaded from standard paths, try to create a basic one
                    if (loadedCount == 0)
                    {
                        // This would require a family template, which might not be available
                        // For now, just show a message about manual loading
                    }

                    if (loadedCount > 0)
                    {
                        doc.Regenerate();
                        transaction.Commit();
                    }
                    else
                    {
                        transaction.RollBack();
                    }

                    // Check final state
                    var finalFamilies = familyService.GetAvailableColumnFamilies();
                    var finalSymbols = familyService.GetAvailableColumnSymbols();

                    string resultMessage = $"Loading Results:\n\n";
                    resultMessage += $"📦 Families loaded: {loadedCount}\n";
                    if (loadedFamilies.Any())
                    {
                        resultMessage += $"  • {string.Join("\n  • ", loadedFamilies)}\n";
                    }
                    resultMessage += $"\n📊 Final state:\n";
                    resultMessage += $"• Column families: {finalFamilies.Count} (was {currentFamilies.Count})\n";
                    resultMessage += $"• Column symbols: {finalSymbols.Count} (was {currentSymbols.Count})\n";

                    if (loadedCount == 0)
                    {
                        resultMessage += $"\n❌ No families were loaded automatically.\n\n";
                        resultMessage += $"💡 Manual loading instructions:\n";
                        resultMessage += $"1. Go to Insert > Load Family\n";
                        resultMessage += $"2. Navigate to your Revit installation folder\n";
                        resultMessage += $"3. Look for Libraries > US Imperial > Structural Columns\n";
                        resultMessage += $"4. Load 'Concrete-Rectangular-Column.rfa' or similar\n";
                        resultMessage += $"5. Try batch processing again\n";
                    }
                    else
                    {
                        resultMessage += $"\n✅ You can now try batch column creation again!";
                    }

                    Autodesk.Revit.UI.TaskDialog.Show("Load Standard Families Complete", resultMessage);

                    return loadedCount > 0 ? Result.Succeeded : Result.Failed;
                }
            }
            catch (Exception ex)
            {
                message = $"Failed to load standard families: {ex.Message}";
                return Result.Failed;
            }
        }

        private System.Collections.Generic.List<string> GetStandardColumnFamilyPaths()
        {
            var paths = new System.Collections.Generic.List<string>();

            try
            {
                // Get Revit installation path
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

                var basePaths = new[] { programFiles, programFilesX86 };
                var versions = new[] { "2026", "2025", "2024", "2023" };

                foreach (var basePath in basePaths)
                {
                    foreach (var version in versions)
                    {
                        var familyPath = Path.Combine(basePath, "Autodesk", $"Revit {version}", "Libraries", "US Imperial", "Structural Columns");
                        
                        if (Directory.Exists(familyPath))
                        {
                            // Look for common column family files
                            var commonFamilies = new[]
                            {
                                "Concrete-Rectangular-Column.rfa",
                                "Steel-Column.rfa",
                                "Rectangular Column.rfa",
                                "Column.rfa"
                            };

                            foreach (var familyFile in commonFamilies)
                            {
                                var fullPath = Path.Combine(familyPath, familyFile);
                                if (File.Exists(fullPath))
                                {
                                    paths.Add(fullPath);
                                }
                            }

                            // If we found some families, don't check older versions
                            if (paths.Any())
                            {
                                return paths;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not locate standard family paths: {ex.Message}", "GetStandardColumnFamilyPaths");
            }

            return paths;
        }
    }
}