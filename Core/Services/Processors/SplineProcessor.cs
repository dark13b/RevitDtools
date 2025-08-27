using Autodesk.Revit.DB;
using RevitDtools.Core.Interfaces;
using RevitDtools.Core.Models;
using System;
using System.Linq;
using RevitView = Autodesk.Revit.DB.View;

namespace RevitDtools.Core.Services
{
    /// <summary>
    /// Processor for NurbSpline geometry objects
    /// </summary>
    public class SplineProcessor : BaseGeometryProcessor
    {
        public SplineProcessor(Document document, ILogger logger, RevitView activeView) 
            : base(document, logger, activeView)
        {
        }

        public override ProcessingResult Process(GeometryObject geometryObject, Transform transform)
        {
            return ExecuteGeometryProcessing(() =>
            {
                if (!(geometryObject is NurbSpline spline))
                {
                    return ProcessingResult.CreateFailure("Invalid spline geometry object");
                }

                Logger.LogInfo($"Processing NURBS spline with degree {spline.Degree}");

                // Transform the spline
                var transformedSpline = spline.CreateTransformed(transform);

                // Validate the spline
                if (!transformedSpline.IsBound)
                {
                    return ProcessingResult.CreateFailure("Spline is not bounded");
                }

                if (transformedSpline.Length < Document.Application.ShortCurveTolerance)
                {
                    return ProcessingResult.CreateSkipped("Spline too short, skipping");
                }

                // Validate spline properties
                if (transformedSpline is NurbSpline nurbSpline)
                {
                    if (nurbSpline.CtrlPoints.Count < 2)
                    {
                        return ProcessingResult.CreateFailure("Spline must have at least 2 control points");
                    }

                    // Check for degenerate spline (all control points are the same)
                    var firstPoint = nurbSpline.CtrlPoints[0];
                    if (nurbSpline.CtrlPoints.All(p => p.IsAlmostEqualTo(firstPoint, Document.Application.ShortCurveTolerance)))
                    {
                        return ProcessingResult.CreateSkipped("Degenerate spline (all control points identical), skipping");
                    }
                }

                // Create detail curve
                var detailCurve = Document.Create.NewDetailCurve(ActiveView, transformedSpline);
                
                if (detailCurve != null)
                {
                    Logger.LogInfo($"Successfully created detail spline (ID: {detailCurve.Id})");
                    return ProcessingResult.CreateSuccess(1, "Spline converted to detail curve");
                }
                else
                {
                    return ProcessingResult.CreateFailure("Failed to create detail curve from spline");
                }
            }, "SplineProcessor.Process");
        }
    }
}