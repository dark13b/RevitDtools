using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitDtools.UI.Availability
{
    /// <summary>
    /// Availability class for Enhanced DWG to Detail Line command
    /// </summary>
    public class EnhancedDwgToDetailLineAvailability : BaseAvailability
    {
        protected override string[] AllowedViewTypes => new[] { "ViewPlan", "ViewSection", "ViewDrafting" };

        protected override bool IsCommandAvailableCustom(UIApplication applicationData, CategorySet selectedCategories)
        {
            var activeView = applicationData.ActiveUIDocument?.ActiveView;
            if (activeView == null)
                return false;

            return activeView is ViewPlan || activeView is ViewSection || activeView is ViewDrafting;
        }
    }
}