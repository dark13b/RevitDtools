using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitDtools.Core.Interfaces;
using RevitDtools.Core.Services;
using RevitDtools.Utilities;
using System;
using System.Linq;
using RevitTaskDialog = Autodesk.Revit.UI.TaskDialog;

namespace RevitDtools.Core.Commands
{
    /// <summary>
    /// Main command for advanced column creation features
    /// Provides access to circular columns, custom shapes, column grids, and schedule integration
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class AdvancedColumnCommand : IExternalCommand
    {
        private ILogger _logger;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Initialize services
            _logger = Logger.Instance;

            try
            {
                _logger.LogInfo("Starting Advanced Column Creation command");

                // Show main menu for advanced column features
                var selectedFeature = ShowAdvancedColumnMenu();
                if (selectedFeature == AdvancedColumnFeature.None)
                {
                    message = "Operation cancelled";
                    return Result.Cancelled;
                }

                // Execute the selected feature
                var result = ExecuteSelectedFeature(selectedFeature, commandData, ref message, elements);
                
                _logger.LogInfo($"Advanced Column Creation command completed with result: {result}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AdvancedColumnCommand");
                message = $"Unexpected error: {ex.Message}";
                return Result.Failed;
            }
        }

        #region Private Methods

        private AdvancedColumnFeature ShowAdvancedColumnMenu()
        {
            try
            {
                var dialog = new RevitTaskDialog("Advanced Column Creation")
                {
                    MainInstruction = "Select Advanced Column Feature",
                    MainContent = "Choose the type of advanced column creation you want to perform:",
                    CommonButtons = TaskDialogCommonButtons.Cancel,
                    DefaultButton = TaskDialogResult.Cancel
                };

                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, 
                    "Circular Column Creation", 
                    "Create circular columns by specifying center point and diameter");

                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, 
                    "Custom Shape Column Creation", 
                    "Create columns from user-defined profile curves");

                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, 
                    "Column Grid Generation", 
                    "Create multiple columns in a grid pattern with specified spacing");

                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink4, 
                    "Enhanced Rectangular Column", 
                    "Create rectangular columns with automatic schedule data integration");

                // Remove the 5th option, use CommandLink4 for schedule management
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink4, 
                    "Column Schedule Management", 
                    "Manage column schedule data and apply to existing columns");

                var result = dialog.Show();
                
                switch (result)
                {
                    case TaskDialogResult.CommandLink1:
                        return AdvancedColumnFeature.CircularColumn;
                    case TaskDialogResult.CommandLink2:
                        return AdvancedColumnFeature.CustomShapeColumn;
                    case TaskDialogResult.CommandLink3:
                        return AdvancedColumnFeature.ColumnGrid;
                    case TaskDialogResult.CommandLink4:
                        return AdvancedColumnFeature.ScheduleManagement;
                    default:
                        return AdvancedColumnFeature.None;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ShowAdvancedColumnMenu");
                return AdvancedColumnFeature.None;
            }
        }

        private Result ExecuteSelectedFeature(AdvancedColumnFeature feature, ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                switch (feature)
                {
                    case AdvancedColumnFeature.CircularColumn:
                        var circularCommand = new CircularColumnCommand();
                        return circularCommand.Execute(commandData, ref message, elements);

                    case AdvancedColumnFeature.CustomShapeColumn:
                        var customShapeCommand = new CustomShapeColumnCommand();
                        return customShapeCommand.Execute(commandData, ref message, elements);

                    case AdvancedColumnFeature.ColumnGrid:
                        var gridCommand = new ColumnGridCommand();
                        return gridCommand.Execute(commandData, ref message, elements);

                    case AdvancedColumnFeature.EnhancedRectangular:
                        var enhancedCommand = new EnhancedColumnByLineCommand();
                        return enhancedCommand.Execute(commandData, ref message, elements);

                    case AdvancedColumnFeature.ScheduleManagement:
                        return ExecuteScheduleManagement(commandData, ref message);

                    default:
                        message = "Invalid feature selection";
                        return Result.Failed;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"ExecuteSelectedFeature: {feature}");
                message = $"Error executing {feature}: {ex.Message}";
                return Result.Failed;
            }
        }

        private Result ExecuteScheduleManagement(ExternalCommandData commandData, ref string message)
        {
            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;

                var scheduleService = new ColumnScheduleService(doc, _logger);

                // Show schedule management options
                var dialog = new RevitTaskDialog("Column Schedule Management")
                {
                    MainInstruction = "Column Schedule Management",
                    MainContent = "Select a schedule management action:",
                    CommonButtons = TaskDialogCommonButtons.Cancel
                };

                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, 
                    "Load Schedule Data from Project", 
                    "Load column schedule data from existing project schedules");

                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, 
                    "Apply Schedule Data to Selected Columns", 
                    "Apply schedule data to currently selected columns");

                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, 
                    "View Available Schedule Data", 
                    "Display all available column schedule data entries");

                var result = dialog.Show();
                
                switch (result)
                {
                    case TaskDialogResult.CommandLink1:
                        var loadResult = scheduleService.LoadScheduleDataFromProject();
                        message = loadResult.Message;
                        return loadResult.Success ? Result.Succeeded : Result.Failed;

                    case TaskDialogResult.CommandLink2:
                        return ApplyScheduleDataToSelectedColumns(uidoc, scheduleService, ref message);

                    case TaskDialogResult.CommandLink3:
                        return ShowAvailableScheduleData(scheduleService, ref message);

                    default:
                        message = "Operation cancelled";
                        return Result.Cancelled;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExecuteScheduleManagement");
                message = $"Error in schedule management: {ex.Message}";
                return Result.Failed;
            }
        }

        private Result ApplyScheduleDataToSelectedColumns(UIDocument uidoc, ColumnScheduleService scheduleService, ref string message)
        {
            try
            {
                var selectedIds = uidoc.Selection.GetElementIds();
                if (!selectedIds.Any())
                {
                    message = "No elements selected. Please select columns to apply schedule data.";
                    return Result.Failed;
                }

                var columns = selectedIds
                    .Select(id => uidoc.Document.GetElement(id))
                    .OfType<FamilyInstance>()
                    .Where(fi => fi.Category?.Id.Value == (int)BuiltInCategory.OST_StructuralColumns)
                    .ToList();

                if (!columns.Any())
                {
                    message = "No structural columns found in selection.";
                    return Result.Failed;
                }

                int successCount = 0;
                int failureCount = 0;

                foreach (var column in columns)
                {
                    var result = scheduleService.ApplyScheduleData(column);
                    if (result.Success)
                        successCount++;
                    else
                        failureCount++;
                }

                message = $"Applied schedule data to {successCount} columns. {failureCount} failed.";
                return successCount > 0 ? Result.Succeeded : Result.Failed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ApplyScheduleDataToSelectedColumns");
                message = $"Error applying schedule data: {ex.Message}";
                return Result.Failed;
            }
        }

        private Result ShowAvailableScheduleData(ColumnScheduleService scheduleService, ref string message)
        {
            try
            {
                var scheduleData = scheduleService.GetAvailableScheduleData();
                
                if (!scheduleData.Any())
                {
                    message = "No schedule data available.";
                    RevitTaskDialog.Show("Schedule Data", "No column schedule data is currently available.");
                    return Result.Succeeded;
                }

                var scheduleInfo = string.Join("\n", scheduleData.Select(data => 
                    $"• {data.ColumnMark}: {data.Width:F1}' × {data.Height:F1}' - {data.Material}"));

                RevitTaskDialog.Show("Available Schedule Data", 
                    $"Available column schedule data entries:\n\n{scheduleInfo}");

                message = $"Displayed {scheduleData.Count} schedule data entries.";
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ShowAvailableScheduleData");
                message = $"Error displaying schedule data: {ex.Message}";
                return Result.Failed;
            }
        }

        #endregion

        #region Helper Enums

        private enum AdvancedColumnFeature
        {
            None,
            CircularColumn,
            CustomShapeColumn,
            ColumnGrid,
            EnhancedRectangular,
            ScheduleManagement
        }

        #endregion
    }
}