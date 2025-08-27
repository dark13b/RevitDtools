using Autodesk.Revit.DB;
using RevitDtools.Core.Models;
using System.Collections.Generic;

namespace RevitDtools.Core.Interfaces
{
    /// <summary>
    /// Interface for managing Revit families, particularly column families
    /// </summary>
    public interface IFamilyManager
    {
        /// <summary>
        /// Create a new column family with specified dimensions
        /// </summary>
        Family CreateColumnFamily(string familyName, double width, double height);

        /// <summary>
        /// Create a custom family symbol with specific parameters
        /// </summary>
        FamilySymbol CreateCustomSymbol(Family family, ColumnParameters parameters);

        /// <summary>
        /// Load standard column families into the project
        /// </summary>
        void LoadStandardColumnFamilies();

        /// <summary>
        /// Validate if a family is compatible for column creation
        /// </summary>
        bool ValidateFamilyCompatibility(Family family);

        /// <summary>
        /// Get all available column families in the project
        /// </summary>
        List<Family> GetAvailableColumnFamilies();

        /// <summary>
        /// Get all available column family symbols in the project
        /// </summary>
        List<FamilySymbol> GetAvailableColumnSymbols();

        /// <summary>
        /// Find or create a family symbol with the specified dimensions
        /// </summary>
        FamilySymbol FindOrCreateSymbol(double width, double height, string baseFamilyName = null);
    }
}