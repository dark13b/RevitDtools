using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitDtools.UI.Availability
{
    /// <summary>
    /// Availability class for Column by Line commands
    /// </summary>
    public class ColumnByLineAvailability : BaseAvailability
    {
        protected override string[] AllowedViewTypes => new[] { "ViewPlan", "View3D" };

        protected override bool IsCommandAvailableCustom(UIApplication applicationData, CategorySet selectedCategories)
        {
            var activeView = applicationData.ActiveUIDocument?.ActiveView;
            if (activeView == null)
                return false;

            // Columns are best created in plan views or 3D views
            return activeView is ViewPlan || activeView is View3D;
        }
    }
}