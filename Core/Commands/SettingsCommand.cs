using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
// Temporarily commented out due to XAML compilation issues
// using RevitDtools.UI.Windows;
using RevitDtools.Utilities;
using RevitTaskDialog = Autodesk.Revit.UI.TaskDialog;

namespace RevitDtools.Core.Commands
{
    /// <summary>
    /// Command to open the RevitDtools settings window
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class SettingsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Logger.LogInfo("Settings command executed (UI temporarily disabled)", "SettingsCommand");

                // Temporarily show message instead of opening settings window
                RevitTaskDialog.Show("Settings", "Settings window is temporarily disabled due to XAML compilation issues.\nCore functionality is available.", TaskDialogCommonButtons.Ok);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SettingsCommand");
                message = $"Error opening settings: {ex.Message}";
                
                RevitTaskDialog.Show("Error", $"Failed to open settings window:\n\n{ex.Message}", 
                    TaskDialogCommonButtons.Ok);
                
                return Result.Failed;
            }
        }
    }
}