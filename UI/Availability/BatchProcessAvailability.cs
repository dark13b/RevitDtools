using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitDtools.UI.Availability
{
    /// <summary>
    /// Availability class for Batch Processing command
    /// </summary>
    public class BatchProcessAvailability : BaseAvailability
    {
        protected override string[] AllowedViewTypes => new[] { "ViewPlan", "ViewSection", "ViewDrafting", "View3D" };

        protected override bool IsCommandAvailableCustom(UIApplication applicationData, CategorySet selectedCategories)
        {
            // Batch processing is available in any view as long as we have an active document
            return applicationData.ActiveUIDocument?.Document != null;
        }
    }
}