using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitDtools.UI.Windows;
using RevitDtools.Utilities;
using System;

namespace RevitDtools.Core.Commands
{
    /// <summary>
    /// Command to launch the batch processing window for column creation
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class BatchProcessCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var uiApp = commandData.Application;
                var uiDocument = uiApp.ActiveUIDocument;
                var document = uiDocument?.Document;

                if (document == null)
                {
                    message = "No active document found. Please open a Revit project.";
                    return Result.Failed;
                }

                // Create logger
                var logger = Logger.Instance;

                // Launch the batch processing window
                var batchWindow = new BatchProcessingWindow(document, uiDocument, logger);
                batchWindow.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.LogError("BatchProcessCommand", $"Error launching batch processing window: {ex.Message}");
                message = $"Error launching batch processing: {ex.Message}";
                return Result.Failed;
            }
        }
    }
}