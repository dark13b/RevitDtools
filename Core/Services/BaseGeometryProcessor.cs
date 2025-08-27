using Autodesk.Revit.DB;
using RevitDtools.Core.Interfaces;
using RevitDtools.Core.Models;
using System;
using RevitView = Autodesk.Revit.DB.View;

namespace RevitDtools.Core.Services
{
    /// <summary>
    /// Base class for geometry type processors
    /// </summary>
    public abstract class BaseGeometryProcessor : IGeometryTypeProcessor
    {
        protected readonly Document Document;
        protected readonly ILogger Logger;
        protected readonly RevitView ActiveView;

        protected BaseGeometryProcessor(Document document, ILogger logger, RevitView activeView)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ActiveView = activeView ?? throw new ArgumentNullException(nameof(activeView));
        }

        public abstract ProcessingResult Process(GeometryObject geometryObject, Transform transform);

        /// <summary>
        /// Get the layer name from a geometry object
        /// </summary>
        protected string GetLayerName(GeometryObject geomObj)
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

        /// <summary>
        /// Create a detail curve from a curve object
        /// </summary>
        protected ProcessingResult CreateDetailCurve(Curve curve, string context)
        {
            try
            {
                if (curve != null && curve.IsBound)
                {
                    Document.Create.NewDetailCurve(ActiveView, curve);
                    return ProcessingResult.CreateSuccess(1, $"Created detail curve: {context}");
                }
                else
                {
                    Logger.LogWarning($"Invalid or unbounded curve: {context}");
                    return ProcessingResult.CreateFailure("Invalid or unbounded curve");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"CreateDetailCurve - {context}");
                return ProcessingResult.CreateFailure($"Failed to create detail curve: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validate that a geometry object is of the expected type
        /// </summary>
        protected bool ValidateGeometryType<T>(GeometryObject geometryObject, out T typedGeometry) where T : class
        {
            typedGeometry = geometryObject as T;
            if (typedGeometry == null)
            {
                Logger.LogWarning($"Expected {typeof(T).Name} but got {geometryObject?.GetType().Name ?? "null"}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Execute geometry processing with error handling
        /// </summary>
        protected ProcessingResult ExecuteGeometryProcessing(Func<ProcessingResult> operation, string context)
        {
            var startTime = DateTime.Now;
            
            try
            {
                var result = operation();
                result.ProcessingTime = DateTime.Now - startTime;
                result.Context = context;
                return result;
            }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException ex)
            {
                Logger.LogError(ex, context);
                return ProcessingResult.CreateFailure($"Revit operation failed: {ex.Message}", ex);
            }
            catch (ArgumentException ex)
            {
                Logger.LogError(ex, context);
                return ProcessingResult.CreateFailure($"Invalid argument: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, context);
                return ProcessingResult.CreateFailure($"Unexpected error: {ex.Message}", ex);
            }
        }
    }
}