using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitDtools.UI.Availability
{
    /// <summary>
    /// Availability class for Advanced Column command
    /// </summary>
    public class AdvancedColumnAvailability : BaseAvailability
    {
        protected override string[] AllowedViewTypes => new[] { "ViewPlan", "View3D" };

        protected override bool IsCommandAvailableCustom(UIApplication applicationData, CategorySet selectedCategories)
        {
            var activeView = applicationData.ActiveUIDocument?.ActiveView;
            if (activeView == null)
                return false;

            return activeView is ViewPlan || activeView is View3D;
        }
    }
}