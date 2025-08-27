using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitDtools.UI.Availability
{
    /// <summary>
    /// Availability class for BatchColumnByLine command
    /// Controls when the command is available in the Revit UI
    /// </summary>
    public class BatchColumnByLineAvailability : BaseAvailability
    {
        /// <summary>
        /// Allowed view types for batch column creation
        /// </summary>
        protected override string[] AllowedViewTypes => new[] { "ViewPlan", "View3D" };

        /// <summary>
        /// Requires an active document
        /// </summary>
        protected override bool RequireActiveDocument => true;

        /// <summary>
        /// Does not require selection (command will prompt for selection)
        /// </summary>
        protected override bool RequireSelection => false;

        /// <summary>
        /// Custom availability logic for batch column creation
        /// </summary>
        protected override bool IsCommandAvailableCustom(UIApplication applicationData, CategorySet selectedCategories)
        {
            // Additional checks can be added here if needed
            // For example, checking if structural column families are available
            return true;
        }
    }
}