using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using System.Collections.Generic;

namespace RevitDtools.Core.Models
{
    /// <summary>
    /// Parameters for column creation
    /// </summary>
    public class ColumnParameters
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public string MaterialName { get; set; }
        public StructuralType ColumnType { get; set; } = StructuralType.Column;
        public Dictionary<string, object> CustomParameters { get; set; } = new Dictionary<string, object>();
        public string FamilyName { get; set; }
        public string SymbolName { get; set; }

        /// <summary>
        /// Create column parameters with basic dimensions
        /// </summary>
        public static ColumnParameters Create(double width, double height, string familyName = null)
        {
            return new ColumnParameters
            {
                Width = width,
                Height = height,
                FamilyName = familyName ?? "Rectangular Column"
            };
        }

        /// <summary>
        /// Validate column parameters
        /// </summary>
        public bool IsValid(out string validationMessage)
        {
            validationMessage = null;

            if (Width <= 0)
            {
                validationMessage = "Width must be greater than zero";
                return false;
            }

            if (Height <= 0)
            {
                validationMessage = "Height must be greater than zero";
                return false;
            }

            if (Width > 10.0) // 10 feet maximum
            {
                validationMessage = "Width exceeds maximum allowed size (10 feet)";
                return false;
            }

            if (Height > 10.0) // 10 feet maximum
            {
                validationMessage = "Height exceeds maximum allowed size (10 feet)";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get a unique identifier for this parameter set
        /// </summary>
        public string GetUniqueId()
        {
            return $"{FamilyName}_{Width:F3}x{Height:F3}";
        }
    }
}