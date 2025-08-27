using Autodesk.Revit.DB;
using RevitDtools.Core.Models;

namespace RevitDtools.Core.Interfaces
{
    /// <summary>
    /// Interface for processing specific geometry types
    /// </summary>
    public interface IGeometryTypeProcessor
    {
        /// <summary>
        /// Process a specific geometry object and convert it to Revit elements
        /// </summary>
        /// <param name="geometryObject">The geometry object to process</param>
        /// <param name="transform">Transform to apply to the geometry</param>
        /// <returns>Processing result</returns>
        ProcessingResult Process(GeometryObject geometryObject, Transform transform);
    }
}