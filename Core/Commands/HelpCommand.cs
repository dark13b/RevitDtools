using System;
using System.Diagnostics;
using System.Reflection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitDtools.Utilities;
using RevitTaskDialog = Autodesk.Revit.UI.TaskDialog;

namespace RevitDtools.Core.Commands
{
    /// <summary>
    /// Command to display help and about information
    /// </summary>
    [Transaction(TransactionMode.ReadOnly)]
    [Regeneration(RegenerationOption.Manual)]
    public class HelpCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Logger.LogInfo("Opening help dialog", "HelpCommand");

                // Get version information
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                var assemblyInfo = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                var versionString = assemblyInfo?.InformationalVersion ?? version?.ToString() ?? "Unknown";

                // Create help dialog
                var dialog = new RevitTaskDialog("RevitDtools Help & About")
                {
                    MainInstruction = "RevitDtools - Professional DWG Processing Tools",
                    MainContent = BuildHelpContent(versionString),
                    CommonButtons = TaskDialogCommonButtons.Close,
                    DefaultButton = TaskDialogResult.Close,
                    AllowCancellation = true,
                    TitleAutoPrefix = false
                };

                // Add command links for different help topics
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "View User Guide", 
                    "Open the comprehensive user guide with tutorials and examples");
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Report an Issue", 
                    "Report bugs or request new features");
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "Check for Updates", 
                    "Check if a newer version is available");

                var result = dialog.Show();

                // Handle command link clicks
                switch (result)
                {
                    case TaskDialogResult.CommandLink1:
                        OpenUserGuide();
                        break;
                    case TaskDialogResult.CommandLink2:
                        OpenIssueReporter();
                        break;
                    case TaskDialogResult.CommandLink3:
                        CheckForUpdates();
                        break;
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "HelpCommand");
                message = $"Error opening help: {ex.Message}";
                return Result.Failed;
            }
        }

        private string BuildHelpContent(string version)
        {
            return $@"Version: {version}

RevitDtools is a comprehensive add-in for Autodesk Revit that provides professional-grade tools for:

• DWG Processing: Convert imported DWG geometry to native Revit elements
• Column Creation: Create structural columns from detail lines with automatic family management
• Batch Processing: Process multiple DWG files simultaneously with progress tracking
• Advanced Features: Support for all geometry types including arcs, splines, ellipses, text, and hatches

Key Features:
✓ Enhanced geometry processing with comprehensive DWG support
✓ Dynamic family management and creation
✓ Batch processing with cancellation and reporting
✓ User settings and preferences persistence
✓ Professional error logging and diagnostics
✓ Context-sensitive ribbon interface

For detailed instructions and tutorials, click 'View User Guide' below.";
        }

        private void OpenUserGuide()
        {
            try
            {
                // In a real implementation, this would open documentation
                // For now, show a placeholder message
                RevitTaskDialog.Show("User Guide", 
                    "User guide would be available at:\n\n" +
                    "• Online documentation portal\n" +
                    "• PDF user manual\n" +
                    "• Video tutorials\n\n" +
                    "This feature will be implemented in the final release.",
                    TaskDialogCommonButtons.Ok);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OpenUserGuide");
            }
        }

        private void OpenIssueReporter()
        {
            try
            {
                // In a real implementation, this would open a bug reporting system
                RevitTaskDialog.Show("Report Issue", 
                    "Issue reporting would be available through:\n\n" +
                    "• Online support portal\n" +
                    "• Email support\n" +
                    "• GitHub issues (if open source)\n\n" +
                    "This feature will be implemented in the final release.",
                    TaskDialogCommonButtons.Ok);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OpenIssueReporter");
            }
        }

        private void CheckForUpdates()
        {
            try
            {
                // In a real implementation, this would check for updates
                RevitTaskDialog.Show("Check for Updates", 
                    "Update checking would:\n\n" +
                    "• Connect to update server\n" +
                    "• Compare current version with latest\n" +
                    "• Provide download links for updates\n" +
                    "• Show release notes\n\n" +
                    "This feature will be implemented in the final release.",
                    TaskDialogCommonButtons.Ok);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "CheckForUpdates");
            }
        }
    }
}