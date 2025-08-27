using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitDtools.UI.Availability
{
    /// <summary>
    /// Availability class for DWG to Detail Line commands
    /// </summary>
    public class DwgToDetailLineAvailability : BaseAvailability
    {
        protected override string[] AllowedViewTypes => new[] { "ViewPlan", "ViewSection", "ViewDrafting" };

        protected override bool IsCommandAvailableCustom(UIApplication applicationData, CategorySet selectedCategories)
        {
            // Additional check: ensure we're in a view that supports detail lines
            var activeView = applicationData.ActiveUIDocument?.ActiveView;
            if (activeView == null)
                return false;

            // Detail lines can only be created in plan, section, or drafting views
            return activeView is ViewPlan || activeView is ViewSection || activeView is ViewDrafting;
        }
    }
}