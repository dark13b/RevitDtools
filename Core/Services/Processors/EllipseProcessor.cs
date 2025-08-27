using Autodesk.Revit.DB;
using RevitDtools.Core.Interfaces;
using RevitDtools.Core.Models;
using RevitView = Autodesk.Revit.DB.View;

namespace RevitDtools.Core.Services
{
    /// <summary>
    /// Processor for Ellipse geometry objects
    /// </summary>
    public class EllipseProcessor : BaseGeometryProcessor
    {
        public EllipseProcessor(Document document, ILogger logger, RevitView activeView) 
            : base(document, logger, activeView)
        {
        }

        public override ProcessingResult Process(GeometryObject geometryObject, Transform transform)
        {
            return ExecuteGeometryProcessing(() =>
            {
                if (!ValidateGeometryType<Ellipse>(geometryObject, out Ellipse ellipse))
                {
                    return ProcessingResult.CreateFailure("Invalid ellipse geometry object");
                }

                string layerName = GetLayerName(geometryObject);
                Logger.LogInfo($"Processing ellipse from layer: {layerName}");

                // Transform the ellipse
                Ellipse transformedEllipse = ellipse.CreateTransformed(transform) as Ellipse;
                if (transformedEllipse == null)
                {
                    return ProcessingResult.CreateFailure("Failed to transform ellipse");
                }

                // Create detail curve from the ellipse
                return CreateDetailCurve(transformedEllipse, $"Ellipse from {layerName}");

            }, "EllipseProcessor.Process");
        }
    }
}