using Autodesk.Revit.DB;
using RevitDtools.Core.Interfaces;
using RevitDtools.Core.Models;
using System;
using RevitView = Autodesk.Revit.DB.View;

namespace RevitDtools.Core.Services
{
    /// <summary>
    /// Processor for Line geometry objects
    /// </summary>
    public class LineProcessor : BaseGeometryProcessor
    {
        public LineProcessor(Document document, ILogger logger, RevitView activeView) 
            : base(document, logger, activeView)
        {
        }

        public override ProcessingResult Process(GeometryObject geometryObject, Transform transform)
        {
            return ExecuteGeometryProcessing(() =>
            {
                if (!(geometryObject is Line line))
                {
                    return ProcessingResult.CreateFailure("Invalid line geometry object");
                }

                Logger.LogInfo($"Processing line from {line.GetEndPoint(0)} to {line.GetEndPoint(1)}");

                // Transform the line
                var transformedLine = line.CreateTransformed(transform);

                // Validate the line
                if (!transformedLine.IsBound)
                {
                    return ProcessingResult.CreateFailure("Line is not bounded");
                }

                if (transformedLine.Length < Document.Application.ShortCurveTolerance)
                {
                    return ProcessingResult.CreateSkipped("Line too short, skipping");
                }

                // Create detail curve
                var detailCurve = Document.Create.NewDetailCurve(ActiveView, transformedLine);
                
                if (detailCurve != null)
                {
                    Logger.LogInfo($"Successfully created detail line (ID: {detailCurve.Id})");
                    return ProcessingResult.CreateSuccess(1, "Line converted to detail curve");
                }
                else
                {
                    return ProcessingResult.CreateFailure("Failed to create detail curve from line");
                }
            }, "LineProcessor.Process");
        }
    }
}