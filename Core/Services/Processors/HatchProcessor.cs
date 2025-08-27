using Autodesk.Revit.DB;
using RevitDtools.Core.Interfaces;
using RevitDtools.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using RevitView = Autodesk.Revit.DB.View;

namespace RevitDtools.Core.Services
{
    /// <summary>
    /// Processor for Hatch geometry objects
    /// </summary>
    public class HatchProcessor : BaseGeometryProcessor
    {
        public HatchProcessor(Document document, ILogger logger, RevitView activeView) 
            : base(document, logger, activeView)
        {
        }

        public override ProcessingResult Process(GeometryObject geometryObject, Transform transform)
        {
            return ExecuteGeometryProcessing(() =>
            {
                string layerName = GetLayerName(geometryObject);
                Logger.LogInfo($"Processing hatch from layer: {layerName}");

                try
                {
                    // For DWG hatches, we'll attempt to create filled regions
                    // First, try to extract boundary curves from the hatch geometry
                    
                    var boundaryCurves = new List<Curve>();
                    
                    // If the geometry object has tessellated geometry, extract curves from it
                    if (geometryObject is GeometryInstance geomInstance)
                    {
                        var instanceGeometry = geomInstance.GetInstanceGeometry();
                        ExtractCurvesFromGeometry(instanceGeometry, transform, boundaryCurves);
                    }
                    else if (geometryObject is Solid solid)
                    {
                        // Extract curves from solid edges
                        foreach (Edge edge in solid.Edges)
                        {
                            Curve edgeCurve = edge.AsCurve();
                            if (edgeCurve != null)
                            {
                                boundaryCurves.Add(edgeCurve.CreateTransformed(transform));
                            }
                        }
                    }

                    if (!boundaryCurves.Any())
                    {
                        Logger.LogWarning($"No boundary curves found for hatch in layer: {layerName}");
                        return ProcessingResult.CreateSuccess(0, $"Hatch found in {layerName} but no boundary curves extracted");
                    }

                    // Try to create a filled region from the boundary curves
                    // Note: This is a simplified approach - real hatch processing would be more complex
                    
                    // Get available fill pattern elements
                    var fillPatterns = new FilteredElementCollector(Document)
                        .OfClass(typeof(FillPatternElement))
                        .Cast<FillPatternElement>()
                        .Where(fp => fp.GetFillPattern().IsSolidFill)
                        .FirstOrDefault();

                    if (fillPatterns == null)
                    {
                        Logger.LogWarning("No solid fill patterns available for filled region");
                        // Create detail curves instead
                        int curvesCreated = 0;
                        foreach (var curve in boundaryCurves)
                        {
                            var result = CreateDetailCurve(curve, $"Hatch boundary from {layerName}");
                            if (result.Success) curvesCreated++;
                        }
                        return ProcessingResult.CreateSuccess(curvesCreated, $"Created {curvesCreated} boundary curves from hatch in {layerName}");
                    }

                    // Create curve loop for filled region
                    try
                    {
                        var curveLoop = CurveLoop.Create(boundaryCurves);
                        var curveLoops = new List<CurveLoop> { curveLoop };
                        
                        FilledRegion filledRegion = FilledRegion.Create(Document, fillPatterns.Id, ActiveView.Id, curveLoops);
                        
                        if (filledRegion != null)
                        {
                            Logger.LogInfo($"Created filled region from hatch in {layerName}");
                            return ProcessingResult.CreateSuccess(1, $"Created filled region from hatch in {layerName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Could not create filled region from hatch in {layerName}: {ex.Message}");
                        
                        // Fallback: create detail curves for the boundary
                        int curvesCreated = 0;
                        foreach (var curve in boundaryCurves)
                        {
                            var result = CreateDetailCurve(curve, $"Hatch boundary from {layerName}");
                            if (result.Success) curvesCreated++;
                        }
                        return ProcessingResult.CreateSuccess(curvesCreated, $"Created {curvesCreated} boundary curves from hatch in {layerName}");
                    }

                    return ProcessingResult.CreateFailure("Failed to process hatch geometry");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Error processing hatch from {layerName}");
                    return ProcessingResult.CreateFailure($"Error processing hatch: {ex.Message}", ex);
                }

            }, "HatchProcessor.Process");
        }

        private void ExtractCurvesFromGeometry(GeometryElement geomElement, Transform transform, List<Curve> curves)
        {
            foreach (GeometryObject geomObj in geomElement)
            {
                if (geomObj is Curve curve)
                {
                    curves.Add(curve.CreateTransformed(transform));
                }
                else if (geomObj is GeometryInstance nestedInstance)
                {
                    ExtractCurvesFromGeometry(nestedInstance.GetInstanceGeometry(), 
                        transform.Multiply(nestedInstance.Transform), curves);
                }
            }
        }
    }
}