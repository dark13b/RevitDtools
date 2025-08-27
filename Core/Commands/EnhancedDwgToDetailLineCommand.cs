using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RevitDtools.Core.Interfaces;
using RevitDtools.Core.Services;
using RevitDtools.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using RevitTaskDialog = Autodesk.Revit.UI.TaskDialog;

namespace RevitDtools.Core.Commands
{
    /// <summary>
    /// Enhanced DWG to Detail Line command using the comprehensive geometry processing service
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class EnhancedDwgToDetailLineCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            Autodesk.Revit.DB.View activeView = doc.ActiveView;

            // Initialize logging
            ILogger logger = Logger.Instance;
            
            try
            {
                // Validate view type
                if (!(activeView is ViewPlan || activeView is ViewSection || activeView is ViewDrafting))
                {
                    message = "This tool can only be used in Plan, Section, or Drafting views.";
                    return Result.Failed;
                }

                // Select DWG import
                Reference dwgRef;
                try
                {
                    dwgRef = uidoc.Selection.PickObject(ObjectType.Element, 
                        new DwgImportSelectionFilter(), 
                        "Select an imported or linked DWG file");
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return Result.Cancelled;
                }

                ImportInstance importInstance = doc.GetElement(dwgRef) as ImportInstance;
                if (importInstance == null)
                {
                    message = "The selected element is not a DWG import.";
                    return Result.Failed;
                }

                logger.LogInfo($"Processing DWG import: {importInstance.Name}");

                // Initialize geometry processing service
                var geometryService = new GeometryProcessingService(doc, logger, activeView);

                // Get geometry with proper options for comprehensive detection
                Options options = new Options
                {
                    IncludeNonVisibleObjects = true,
                    DetailLevel = ViewDetailLevel.Fine
                };

                GeometryElement geomElem = importInstance.get_Geometry(options);
                if (geomElem == null)
                {
                    message = "Could not access geometry from the selected DWG import.";
                    return Result.Failed;
                }

                // Extract all layers and geometry information
                var allCurves = new List<Tuple<Curve, string>>();
                var allLayers = new HashSet<string>();

                // Use the enhanced geometry processing to get comprehensive layer information
                var tempResult = geometryService.ProcessAllGeometry(geomElem, Transform.Identity, new List<string>());
                
                // Extract layers using the service's internal method (we'll need to expose this)
                ExtractLayersFromGeometry(geomElem, Transform.Identity, doc, allLayers, logger);

                logger.LogInfo($"Found {allLayers.Count} layers in DWG import");

                if (!allLayers.Any())
                {
                    RevitTaskDialog.Show("No Layers", "Could not find any layers in the selected DWG import.");
                    return Result.Failed;
                }

                var layerNames = allLayers.Where(n => !string.IsNullOrWhiteSpace(n))
                                         .OrderBy(name => name)
                                         .ToList();

                // Show layer selection dialog (reuse existing UI)
                var window = new RevitDtools.DwgLayersWindow(layerNames);
                if (window.ShowDialog() != true)
                {
                    return Result.Cancelled;
                }

                List<string> selectedLayers = window.GetSelectedLayers();
                if (!selectedLayers.Any())
                {
                    return Result.Succeeded;
                }

                logger.LogInfo($"User selected {selectedLayers.Count} layers for processing");

                // Process geometry using enhanced service
                using (Transaction tx = new Transaction(doc, "Enhanced DWG to Detail Lines"))
                {
                    tx.Start();
                    
                    try
                    {
                        var processingResult = geometryService.ProcessAllGeometry(geomElem, Transform.Identity, selectedLayers);
                        
                        tx.Commit();

                        // Show results
                        string resultMessage = $"Processing completed:\n";
                        resultMessage += $"- Elements processed: {processingResult.ElementsProcessed}\n";
                        resultMessage += $"- Elements skipped: {processingResult.ElementsSkipped}\n";
                        resultMessage += $"- Processing time: {processingResult.ProcessingTime.TotalSeconds:F2} seconds";

                        if (processingResult.Warnings.Any())
                        {
                            resultMessage += $"\n- Warnings: {processingResult.Warnings.Count}";
                        }

                        if (processingResult.Errors.Any())
                        {
                            resultMessage += $"\n- Errors: {processingResult.Errors.Count}";
                        }

                        RevitTaskDialog.Show("Enhanced Processing Complete", resultMessage);

                        logger.LogInfo("Enhanced DWG processing completed successfully");
                        return Result.Succeeded;
                    }
                    catch (Exception ex)
                    {
                        tx.RollBack();
                        logger.LogError(ex, "Enhanced DWG processing failed");
                        message = $"Error processing DWG: {ex.Message}";
                        return Result.Failed;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Enhanced DWG command execution failed");
                message = $"Command execution failed: {ex.Message}";
                return Result.Failed;
            }
        }

        /// <summary>
        /// Extract layer information from geometry element
        /// </summary>
        private void ExtractLayersFromGeometry(GeometryElement geomElem, Transform transform, 
            Document doc, HashSet<string> layerSet, ILogger logger)
        {
            foreach (GeometryObject geomObj in geomElem)
            {
                string layerName = GetLayerName(geomObj, doc);
                if (!string.IsNullOrWhiteSpace(layerName))
                {
                    layerSet.Add(layerName);
                }

                if (geomObj is GeometryInstance geomInst)
                {
                    // Recursively process nested geometry (blocks)
                    ExtractLayersFromGeometry(geomInst.GetInstanceGeometry(),
                                            transform.Multiply(geomInst.Transform),
                                            doc, layerSet, logger);
                }
            }
        }

        /// <summary>
        /// Get layer name from geometry object
        /// </summary>
        private string GetLayerName(GeometryObject geomObj, Document doc)
        {
            try
            {
                if (geomObj.GraphicsStyleId != ElementId.InvalidElementId)
                {
                    Element styleElement = doc.GetElement(geomObj.GraphicsStyleId);
                    if (styleElement is GraphicsStyle gs && gs.GraphicsStyleCategory != null)
                    {
                        return gs.GraphicsStyleCategory.Name;
                    }
                }
            }
            catch (Exception)
            {
                // Ignore errors in layer name extraction
            }
            return "Unknown Layer";
        }
    }

    /// <summary>
    /// Selection filter for DWG imports
    /// </summary>
    public class DwgImportSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem) => elem is ImportInstance;
        public bool AllowReference(Reference reference, XYZ position) => false;
    }
}