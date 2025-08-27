using Autodesk.Revit.DB;
using RevitDtools.Core.Models;
using System.Collections.Generic;

namespace RevitDtools.Core.Interfaces
{
    /// <summary>
    /// Interface for processing different types of geometry from DWG imports
    /// </summary>
    public interface IGeometryProcessor
    {
        /// <summary>
        /// Process arc geometry and convert to Revit elements
        /// </summary>
        ProcessingResult ProcessArc(GeometryObject arc, Transform transform);

        /// <summary>
        /// Process spline geometry and convert to Revit elements
        /// </summary>
        ProcessingResult ProcessSpline(GeometryObject spline, Transform transform);

        /// <summary>
        /// Process ellipse geometry and convert to Revit elements
        /// </summary>
        ProcessingResult ProcessEllipse(GeometryObject ellipse, Transform transform);

        /// <summary>
        /// Process text geometry and convert to Revit elements
        /// </summary>
        ProcessingResult ProcessText(GeometryObject text, Transform transform);

        /// <summary>
        /// Process hatch geometry and convert to Revit elements
        /// </summary>
        ProcessingResult ProcessHatch(GeometryObject hatch, Transform transform);

        /// <summary>
        /// Process nested block geometry recursively
        /// </summary>
        ProcessingResult ProcessNestedBlock(GeometryInstance block, Transform transform);

        /// <summary>
        /// Process all geometry types from a geometry element
        /// </summary>
        ProcessingResult ProcessAllGeometry(GeometryElement geometryElement, Transform transform, List<string> selectedLayers);

        /// <summary>
        /// Process a Revit element (typically an imported DWG element)
        /// </summary>
        ProcessingResult ProcessElement(Element element);
    }
}