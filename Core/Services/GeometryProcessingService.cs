using Autodesk.Revit.DB;
using RevitDtools.Core.Interfaces;
using RevitDtools.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RevitView = Autodesk.Revit.DB.View;

namespace RevitDtools.Core.Services
{
    /// <summary>
    /// Service for processing all types of DWG geometry and converting to Revit elements
    /// </summary>
    public class GeometryProcessingService : BaseService, IGeometryProcessor
    {
        private Dictionary<Type, IGeometryTypeProcessor> _processors;
        private readonly RevitView _activeView;

        public GeometryProcessingService(Document document, ILogger logger, RevitView activeView) 
            : base(document, logger)
        {
            _activeView = activeView ?? throw new ArgumentNullException(nameof(activeView));
            InitializeProcessors();
        }

        private void InitializeProcessors()
        {
            _processors = new Dictionary<Type, IGeometryTypeProcessor>
            {
                { typeof(Arc), new ArcProcessor(Document, Logger, _activeView) },
                { typeof(NurbSpline), new SplineProcessor(Document, Logger, _activeView) },
                { typeof(Ellipse), new EllipseProcessor(Document, Logger, _activeView) },
                { typeof(Line), new LineProcessor(Document, Logger, _activeView) },
                { typeof(PolyLine), new PolyLineProcessor(Document, Logger, _activeView) }
            };
        }

        public ProcessingResult ProcessArc(GeometryObject arc, Transform transform)
        {
            return ExecuteWithErrorHandling(() =>
            {
                if (arc is Arc arcGeometry)
                {
                    return _processors[typeof(Arc)].Process(arcGeometry, transform);
                }
                return ProcessingResult.CreateFailure("Invalid arc geometry object");
            }, "ProcessArc");
        }

        public ProcessingResult ProcessSpline(GeometryObject spline, Transform transform)
        {
            return ExecuteWithErrorHandling(() =>
            {
                if (spline is NurbSpline splineGeometry)
                {
                    return _processors[typeof(NurbSpline)].Process(splineGeometry, transform);
                }
                return ProcessingResult.CreateFailure("Invalid spline geometry object");
            }, "ProcessSpline");
        }

        public ProcessingResult ProcessEllipse(GeometryObject ellipse, Transform transform)
        {
            return ExecuteWithErrorHandling(() =>
            {
                if (ellipse is Ellipse ellipseGeometry)
                {
                    return _processors[typeof(Ellipse)].Process(ellipseGeometry, transform);
                }
                return ProcessingResult.CreateFailure("Invalid ellipse geometry object");
            }, "ProcessEllipse");
        }

        public ProcessingResult ProcessText(GeometryObject text, Transform transform)
        {
            return ExecuteWithErrorHandling(() =>
            {
                // Text processing will be handled by TextProcessor
                var textProcessor = new TextProcessor(Document, Logger, _activeView);
                return textProcessor.Process(text, transform);
            }, "ProcessText");
        }

        public ProcessingResult ProcessHatch(GeometryObject hatch, Transform transform)
        {
            return ExecuteWithErrorHandling(() =>
            {
                // Hatch processing will be handled by HatchProcessor
                var hatchProcessor = new HatchProcessor(Document, Logger, _activeView);
                return hatchProcessor.Process(hatch, transform);
            }, "ProcessHatch");
        }

        public ProcessingResult ProcessNestedBlock(GeometryInstance block, Transform transform)
        {
            return ExecuteWithErrorHandling(() =>
            {
                var results = new List<ProcessingResult>();
                var nestedGeometry = block.GetInstanceGeometry();
                var combinedTransform = transform.Multiply(block.Transform);

                foreach (GeometryObject geomObj in nestedGeometry)
                {
                    var result = ProcessSingleGeometryObject(geomObj, combinedTransform);
                    results.Add(result);
                }

                return ProcessingResult.Combine(results);
            }, "ProcessNestedBlock");
        }

        public ProcessingResult ProcessAllGeometry(GeometryElement geometryElement, Transform transform, List<string> selectedLayers)
        {
            return ExecuteWithErrorHandling(() =>
            {
                var results = new List<ProcessingResult>();
                var allCurves = new List<Tuple<Curve, string>>();
                var allLayers = new HashSet<string>();
                var geometryStats = new Dictionary<string, int>();

                // Extract all curves and process all geometry types
                ExtractAndProcessAllGeometry(geometryElement, transform, allCurves, allLayers, geometryStats, selectedLayers, results);

                Logger.LogInfo($"Geometry processing summary:");
                Logger.LogInfo($"- Found {allCurves.Count} curves across {allLayers.Count} layers");
                foreach (var stat in geometryStats)
                {
                    Logger.LogInfo($"- {stat.Key}: {stat.Value} elements");
                }

                // Filter curves by selected layers
                var curvesToProcess = allCurves.Where(t => selectedLayers.Contains(t.Item2)).ToList();

                Logger.LogInfo($"Processing {curvesToProcess.Count} curves from selected layers");

                // Process each curve as detail line
                foreach (var curveData in curvesToProcess)
                {
                    var result = ProcessCurveAsDetailLine(curveData.Item1, curveData.Item2);
                    results.Add(result);
                }

                return ProcessingResult.Combine(results);
            }, "ProcessAllGeometry");
        }

        /// <summary>
        /// Comprehensive geometry extraction and processing method
        /// </summary>
        private void ExtractAndProcessAllGeometry(GeometryElement geomElem, Transform transform, 
            List<Tuple<Curve, string>> curveList, HashSet<string> layerSet, 
            Dictionary<string, int> geometryStats, List<string> selectedLayers, List<ProcessingResult> results)
        {
            foreach (GeometryObject geomObj in geomElem)
            {
                string layerName = GetLayerName(geomObj);
                if (!string.IsNullOrWhiteSpace(layerName))
                {
                    layerSet.Add(layerName);
                }

                string geometryType = geomObj.GetType().Name;
                geometryStats[geometryType] = geometryStats.ContainsKey(geometryType) ? geometryStats[geometryType] + 1 : 1;

                // Only process non-curve geometry if the layer is selected
                bool shouldProcessNonCurves = selectedLayers.Contains(layerName);

                if (geomObj is Curve curve)
                {
                    curveList.Add(new Tuple<Curve, string>(curve.CreateTransformed(transform), layerName));
                }
                else if (geomObj is Arc arc)
                {
                    curveList.Add(new Tuple<Curve, string>(arc.CreateTransformed(transform), layerName));
                }
                else if (geomObj is Ellipse ellipse)
                {
                    curveList.Add(new Tuple<Curve, string>(ellipse.CreateTransformed(transform), layerName));
                }
                else if (geomObj is NurbSpline spline)
                {
                    curveList.Add(new Tuple<Curve, string>(spline.CreateTransformed(transform), layerName));
                }
                else if (geomObj is Line line)
                {
                    curveList.Add(new Tuple<Curve, string>(line.CreateTransformed(transform), layerName));
                }
                else if (geomObj is GeometryInstance geomInst)
                {
                    // Recursively process nested geometry (blocks)
                    ExtractAndProcessAllGeometry(geomInst.GetInstanceGeometry(),
                                              transform.Multiply(geomInst.Transform),
                                              curveList, layerSet, geometryStats, selectedLayers, results);
                }
                else if (geomObj is PolyLine polyLine)
                {
                    // Handle polylines by converting to individual line segments
                    IList<XYZ> points = polyLine.GetCoordinates();
                    for (int i = 0; i < points.Count - 1; i++)
                    {
                        try
                        {
                            XYZ start = transform.OfPoint(points[i]);
                            XYZ end = transform.OfPoint(points[i + 1]);

                            if (!start.IsAlmostEqualTo(end, 1e-6))
                            {
                                Line lineSegment = Line.CreateBound(start, end);
                                curveList.Add(new Tuple<Curve, string>(lineSegment, layerName));
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning($"Skipped polyline segment: {ex.Message}");
                        }
                    }
                }
                else if (geomObj is Solid solid && shouldProcessNonCurves)
                {
                    // Process solid geometry (often from hatches)
                    var hatchResult = ProcessHatch(solid, transform);
                    results.Add(hatchResult);
                    
                    // Also extract curves from solid edges
                    foreach (Edge edge in solid.Edges)
                    {
                        try
                        {
                            Curve edgeCurve = edge.AsCurve();
                            if (edgeCurve != null)
                            {
                                curveList.Add(new Tuple<Curve, string>(edgeCurve.CreateTransformed(transform), layerName));
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning($"Skipped solid edge curve: {ex.Message}");
                        }
                    }
                }
                else if (shouldProcessNonCurves)
                {
                    // Handle text and other non-curve geometry types
                    if (geometryType.Contains("Text") || HasTextCharacteristics(geomObj))
                    {
                        var textResult = ProcessText(geomObj, transform);
                        results.Add(textResult);
                    }
                    else
                    {
                        Logger.LogInfo($"Found unhandled geometry type: {geometryType} on layer: {layerName}");
                    }
                }
            }
        }

        /// <summary>
        /// Heuristic to determine if geometry object might represent text
        /// </summary>
        private bool HasTextCharacteristics(GeometryObject geomObj)
        {
            try
            {
                // Check if the geometry has characteristics typical of text
                BoundingBoxXYZ bbox = null;
                
                // Try to get bounding box based on geometry type
                if (geomObj is Curve curve && curve.IsBound)
                {
                    var startPoint = curve.GetEndPoint(0);
                    var endPoint = curve.GetEndPoint(1);
                    bbox = new BoundingBoxXYZ();
                    bbox.Min = new XYZ(Math.Min(startPoint.X, endPoint.X), Math.Min(startPoint.Y, endPoint.Y), Math.Min(startPoint.Z, endPoint.Z));
                    bbox.Max = new XYZ(Math.Max(startPoint.X, endPoint.X), Math.Max(startPoint.Y, endPoint.Y), Math.Max(startPoint.Z, endPoint.Z));
                }
                
                if (bbox != null)
                {
                    double width = bbox.Max.X - bbox.Min.X;
                    double height = bbox.Max.Y - bbox.Min.Y;
                    
                    // Text typically has a certain aspect ratio and size range
                    if (height > 0 && width > 0)
                    {
                        double aspectRatio = width / height;
                        // Text usually has width > height and reasonable dimensions
                        return aspectRatio > 0.5 && aspectRatio < 20 && height < 10; // Assuming feet units
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error checking text characteristics: {ex.Message}");
            }
            return false;
        }

        private ProcessingResult ProcessSingleGeometryObject(GeometryObject geomObj, Transform transform)
        {
            var geometryType = geomObj.GetType();

            if (_processors.ContainsKey(geometryType))
            {
                return _processors[geometryType].Process(geomObj, transform);
            }

            // Handle specific geometry types
            if (geomObj is Curve curve)
            {
                return ProcessCurveAsDetailLine(curve.CreateTransformed(transform), GetLayerName(geomObj));
            }

            if (geomObj is GeometryInstance nestedInstance)
            {
                return ProcessNestedBlock(nestedInstance, transform);
            }

            if (geomObj is PolyLine polyLine)
            {
                return _processors[typeof(PolyLine)].Process(polyLine, transform);
            }

            // Log unsupported geometry type
            Logger.LogWarning($"Unsupported geometry type: {geometryType.Name}");
            return ProcessingResult.CreateSuccess(0, $"Skipped unsupported geometry type: {geometryType.Name}");
        }

        private void ExtractAllCurvesFromGeometry(GeometryElement geomElem, Transform transform, 
            List<Tuple<Curve, string>> curveList, HashSet<string> layerSet)
        {
            foreach (GeometryObject geomObj in geomElem)
            {
                string layerName = GetLayerName(geomObj);
                if (!string.IsNullOrWhiteSpace(layerName))
                {
                    layerSet.Add(layerName);
                }

                if (geomObj is Curve curve)
                {
                    curveList.Add(new Tuple<Curve, string>(curve.CreateTransformed(transform), layerName));
                }
                else if (geomObj is Arc arc)
                {
                    // Handle arcs specifically
                    curveList.Add(new Tuple<Curve, string>(arc.CreateTransformed(transform), layerName));
                }
                else if (geomObj is Ellipse ellipse)
                {
                    // Handle ellipses specifically
                    curveList.Add(new Tuple<Curve, string>(ellipse.CreateTransformed(transform), layerName));
                }
                else if (geomObj is NurbSpline spline)
                {
                    // Handle splines specifically
                    curveList.Add(new Tuple<Curve, string>(spline.CreateTransformed(transform), layerName));
                }
                else if (geomObj is Line line)
                {
                    // Handle lines specifically
                    curveList.Add(new Tuple<Curve, string>(line.CreateTransformed(transform), layerName));
                }
                else if (geomObj is GeometryInstance geomInst)
                {
                    // Recursively process nested geometry (blocks)
                    ExtractAllCurvesFromGeometry(geomInst.GetInstanceGeometry(),
                                              transform.Multiply(geomInst.Transform),
                                              curveList, layerSet);
                }
                else if (geomObj is PolyLine polyLine)
                {
                    // Handle polylines by converting to individual line segments
                    IList<XYZ> points = polyLine.GetCoordinates();
                    for (int i = 0; i < points.Count - 1; i++)
                    {
                        try
                        {
                            XYZ start = transform.OfPoint(points[i]);
                            XYZ end = transform.OfPoint(points[i + 1]);

                            if (!start.IsAlmostEqualTo(end, 1e-6))
                            {
                                Line lineSegment = Line.CreateBound(start, end);
                                curveList.Add(new Tuple<Curve, string>(lineSegment, layerName));
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning($"Skipped polyline segment: {ex.Message}");
                        }
                    }
                }
                else if (geomObj is Solid solid)
                {
                    // Extract curves from solid edges (for hatches and complex geometry)
                    foreach (Edge edge in solid.Edges)
                    {
                        try
                        {
                            Curve edgeCurve = edge.AsCurve();
                            if (edgeCurve != null)
                            {
                                curveList.Add(new Tuple<Curve, string>(edgeCurve.CreateTransformed(transform), layerName));
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning($"Skipped solid edge curve: {ex.Message}");
                        }
                    }
                }
                else
                {
                    // Handle other geometry types (text, hatches, etc.)
                    Logger.LogInfo($"Found non-curve geometry type: {geomObj.GetType().Name} on layer: {layerName}");
                    
                    // Process using specific processors but don't add to curve list
                    var result = ProcessSingleGeometryObject(geomObj, transform);
                    if (!result.Success)
                    {
                        Logger.LogWarning($"Failed to process {geomObj.GetType().Name}: {result.Message}");
                    }
                }
            }
        }

        private ProcessingResult ProcessCurveAsDetailLine(Curve curve, string layerName)
        {
            try
            {
                if (curve != null && curve.IsBound)
                {
                    Document.Create.NewDetailCurve(_activeView, curve);
                    return ProcessingResult.CreateSuccess(1, $"Created detail line from {layerName}");
                }
                else
                {
                    return ProcessingResult.CreateFailure("Invalid or unbounded curve");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"ProcessCurveAsDetailLine - Layer: {layerName}");
                return ProcessingResult.CreateFailure($"Failed to create detail line: {ex.Message}", ex);
            }
        }

        public ProcessingResult ProcessElement(Element element)
        {
            return ExecuteWithErrorHandling(() =>
            {
                if (element == null)
                {
                    return ProcessingResult.CreateFailure("Element is null");
                }

                Logger.LogInfo($"Processing element: {element.Name ?? element.GetType().Name} (ID: {element.Id})");

                // Get the element's geometry
                var geometryElement = element.get_Geometry(new Options());
                if (geometryElement == null)
                {
                    return ProcessingResult.CreateFailure("Element has no geometry");
                }

                // Process all geometry with all layers selected (for batch processing)
                var allLayers = new List<string>();
                var tempCurves = new List<Tuple<Curve, string>>();
                var tempLayerSet = new HashSet<string>();
                var tempStats = new Dictionary<string, int>();
                var tempResults = new List<ProcessingResult>();

                // Extract all layers first
                ExtractAndProcessAllGeometry(geometryElement, Transform.Identity, tempCurves, tempLayerSet, tempStats, new List<string>(), tempResults);
                allLayers.AddRange(tempLayerSet);

                // Now process with all layers selected
                return ProcessAllGeometry(geometryElement, Transform.Identity, allLayers);
            }, "ProcessElement");
        }

        private string GetLayerName(GeometryObject geomObj)
        {
            try
            {
                if (geomObj.GraphicsStyleId != ElementId.InvalidElementId)
                {
                    Element styleElement = Document.GetElement(geomObj.GraphicsStyleId);
                    if (styleElement is GraphicsStyle gs && gs.GraphicsStyleCategory != null)
                    {
                        return gs.GraphicsStyleCategory.Name;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error getting layer name: {ex.Message}");
            }
            return "Unknown Layer";
        }
    }
}