using Autodesk.Revit.DB;
using RevitDtools.Core.Interfaces;
using RevitDtools.Core.Models;
using System;
using System.Linq;
using RevitView = Autodesk.Revit.DB.View;

namespace RevitDtools.Core.Services
{
    /// <summary>
    /// Processor for Text geometry objects
    /// </summary>
    public class TextProcessor : BaseGeometryProcessor
    {
        public TextProcessor(Document document, ILogger logger, RevitView activeView) 
            : base(document, logger, activeView)
        {
        }

        public override ProcessingResult Process(GeometryObject geometryObject, Transform transform)
        {
            return ExecuteGeometryProcessing(() =>
            {
                string layerName = GetLayerName(geometryObject);
                Logger.LogInfo($"Processing text from layer: {layerName}");

                try
                {
                    // For DWG imports, text is often represented as geometry curves that form text shapes
                    // We'll attempt to create text notes at the geometry location
                    
                    // Get the bounding box of the text geometry to determine placement
                    BoundingBoxXYZ bbox = null;
                    
                    // Try to get bounding box based on geometry type
                    if (geometryObject is Curve curve)
                    {
                        var startPoint = curve.GetEndPoint(0);
                        var endPoint = curve.GetEndPoint(1);
                        bbox = new BoundingBoxXYZ();
                        bbox.Min = new XYZ(Math.Min(startPoint.X, endPoint.X), Math.Min(startPoint.Y, endPoint.Y), Math.Min(startPoint.Z, endPoint.Z));
                        bbox.Max = new XYZ(Math.Max(startPoint.X, endPoint.X), Math.Max(startPoint.Y, endPoint.Y), Math.Max(startPoint.Z, endPoint.Z));
                    }
                    
                    if (bbox == null)
                    {
                        Logger.LogWarning($"Could not determine text bounds for layer: {layerName}");
                        return ProcessingResult.CreateSuccess(0, $"Text element found in {layerName} but bounds unavailable");
                    }

                    // Transform the center point
                    XYZ center = transform.OfPoint((bbox.Min + bbox.Max) * 0.5);
                    
                    // Create a simple text note at the location
                    // Note: In a real implementation, we would extract actual text content from DWG
                    string textContent = $"[Text from {layerName}]";
                    
                    // Get default text note type
                    var textNoteTypes = new FilteredElementCollector(Document)
                        .OfClass(typeof(TextNoteType))
                        .Cast<TextNoteType>()
                        .FirstOrDefault();

                    if (textNoteTypes == null)
                    {
                        Logger.LogWarning("No text note types available in document");
                        return ProcessingResult.CreateSuccess(0, $"Text found in {layerName} but no text note types available");
                    }

                    // Create text note
                    TextNote textNote = TextNote.Create(Document, ActiveView.Id, center, textContent, textNoteTypes.Id);
                    
                    if (textNote != null)
                    {
                        Logger.LogInfo($"Created text note from {layerName} at ({center.X:F2}, {center.Y:F2})");
                        return ProcessingResult.CreateSuccess(1, $"Created text note from {layerName}");
                    }
                    else
                    {
                        return ProcessingResult.CreateFailure("Failed to create text note");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Error processing text from {layerName}");
                    return ProcessingResult.CreateFailure($"Error processing text: {ex.Message}", ex);
                }

            }, "TextProcessor.Process");
        }
    }
}