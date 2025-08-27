using Autodesk.Revit.DB;
using RevitDtools.Core.Interfaces;
using RevitDtools.Core.Models;
using System;
using System.Collections.Generic;
using RevitView = Autodesk.Revit.DB.View;

namespace RevitDtools.Core.Services
{
    /// <summary>
    /// Processor for PolyLine geometry objects
    /// </summary>
    public class PolyLineProcessor : BaseGeometryProcessor
    {
        public PolyLineProcessor(Document document, ILogger logger, RevitView activeView) 
            : base(document, logger, activeView)
        {
        }

        public override ProcessingResult Process(GeometryObject geometryObject, Transform transform)
        {
            return ExecuteGeometryProcessing(() =>
            {
                if (!ValidateGeometryType<PolyLine>(geometryObject, out PolyLine polyLine))
                {
                    return ProcessingResult.CreateFailure("Invalid polyline geometry object");
                }

                string layerName = GetLayerName(geometryObject);
                Logger.LogInfo($"Processing polyline from layer: {layerName}");

                var results = new List<ProcessingResult>();
                IList<XYZ> points = polyLine.GetCoordinates();

                // Convert polyline to individual line segments
                for (int i = 0; i < points.Count - 1; i++)
                {
                    try
                    {
                        XYZ start = transform.OfPoint(points[i]);
                        XYZ end = transform.OfPoint(points[i + 1]);

                        // Skip zero-length segments
                        if (start.IsAlmostEqualTo(end, 1e-6))
                        {
                            Logger.LogWarning($"Skipped zero-length polyline segment in {layerName}");
                            continue;
                        }

                        Line line = Line.CreateBound(start, end);
                        var result = CreateDetailCurve(line, $"Polyline segment {i + 1} from {layerName}");
                        results.Add(result);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, $"Failed to process polyline segment {i + 1} from {layerName}");
                        results.Add(ProcessingResult.CreateFailure($"Failed to process segment {i + 1}: {ex.Message}", ex));
                    }
                }

                return ProcessingResult.Combine(results);

            }, "PolyLineProcessor.Process");
        }
    }
}