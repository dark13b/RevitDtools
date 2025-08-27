using Autodesk.Revit.DB;
using RevitDtools.Core.Interfaces;
using RevitDtools.Core.Models;
using System;
using RevitView = Autodesk.Revit.DB.View;

namespace RevitDtools.Core.Services
{
    /// <summary>
    /// Processor for Arc geometry objects
    /// </summary>
    public class ArcProcessor : BaseGeometryProcessor
    {
        public ArcProcessor(Document document, ILogger logger, RevitView activeView) 
            : base(document, logger, activeView)
        {
        }

        public override ProcessingResult Process(GeometryObject geometryObject, Transform transform)
        {
            return ExecuteGeometryProcessing(() =>
            {
                if (!(geometryObject is Arc arc))
                {
                    return ProcessingResult.CreateFailure("Invalid arc geometry object");
                }

                Logger.LogInfo($"Processing arc with center {arc.Center} and length {arc.Length}");

                // Transform the arc
                var transformedArc = arc.CreateTransformed(transform);

                // Validate the arc
                if (!transformedArc.IsBound)
                {
                    return ProcessingResult.CreateFailure("Arc is not bounded");
                }

                if (transformedArc.Length < Document.Application.ShortCurveTolerance)
                {
                    return ProcessingResult.CreateSkipped("Arc too short, skipping");
                }

                // Validate arc (Arc-specific validation would require casting)
                if (transformedArc is Arc arcCast && arcCast.Radius < Document.Application.ShortCurveTolerance)
                {
                    return ProcessingResult.CreateFailure("Arc radius too small");
                }

                // Create detail curve
                var detailCurve = Document.Create.NewDetailCurve(ActiveView, transformedArc);
                
                if (detailCurve != null)
                {
                    Logger.LogInfo($"Successfully created detail arc (ID: {detailCurve.Id})");
                    return ProcessingResult.CreateSuccess(1, "Arc converted to detail curve");
                }
                else
                {
                    return ProcessingResult.CreateFailure("Failed to create detail curve from arc");
                }
            }, "ArcProcessor.Process");
        }
    }
}