using Autodesk.Revit.DB;
using RevitDtools.Core.Interfaces;
using RevitDtools.Core.Models;
using System;

namespace RevitDtools.Core.Services
{
    /// <summary>
    /// Base service class providing common functionality for all services
    /// </summary>
    public abstract class BaseService
    {
        protected readonly Document Document;
        protected readonly ILogger Logger;

        protected BaseService(Document document, ILogger logger)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Execute an operation with comprehensive error handling
        /// </summary>
        protected ProcessingResult ExecuteWithErrorHandling(Func<ProcessingResult> operation, string context)
        {
            var startTime = DateTime.Now;
            
            try
            {
                Logger.LogInfo($"Starting operation: {context}");
                var result = operation();
                result.ProcessingTime = DateTime.Now - startTime;
                result.Context = context;

                if (result.Success)
                {
                    Logger.LogInfo($"Operation completed successfully: {context} - {result.Message}");
                }
                else
                {
                    Logger.LogWarning($"Operation completed with issues: {context} - {result.Message}");
                }

                return result;
            }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException ex)
            {
                Logger.LogError(ex, context);
                return ProcessingResult.CreateFailure($"Revit operation failed: {ex.Message}", ex);
            }
            catch (ArgumentException ex)
            {
                Logger.LogError(ex, context);
                return ProcessingResult.CreateFailure($"Invalid argument: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, context);
                return ProcessingResult.CreateFailure($"Unexpected error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Execute a transaction with error handling
        /// </summary>
        protected ProcessingResult ExecuteTransaction(Func<Transaction, ProcessingResult> operation, string transactionName)
        {
            using (var transaction = new Transaction(Document, transactionName))
            {
                try
                {
                    transaction.Start();
                    var result = operation(transaction);
                    
                    if (result.Success)
                    {
                        transaction.Commit();
                        Logger.LogInfo($"Transaction committed: {transactionName}");
                    }
                    else
                    {
                        transaction.RollBack();
                        Logger.LogWarning($"Transaction rolled back: {transactionName} - {result.Message}");
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    transaction.RollBack();
                    Logger.LogError(ex, $"Transaction failed: {transactionName}");
                    return ProcessingResult.CreateFailure($"Transaction failed: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Validate that the document is in a valid state for operations
        /// </summary>
        protected bool ValidateDocument(out string errorMessage)
        {
            errorMessage = null;

            if (Document == null)
            {
                errorMessage = "Document is null";
                return false;
            }

            if (Document.IsReadOnly)
            {
                errorMessage = "Document is read-only";
                return false;
            }

            if (Document.ActiveView == null)
            {
                errorMessage = "No active view";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get a safe element name for logging and error reporting
        /// </summary>
        protected string GetSafeElementName(Element element)
        {
            if (element == null) return "null";
            
            try
            {
                return $"{element.GetType().Name} (Id: {element.Id})";
            }
            catch
            {
                return $"Element (Id: {element.Id})";
            }
        }

        /// <summary>
        /// Check if an element is valid for processing
        /// </summary>
        protected bool IsElementValid(Element element)
        {
            return element != null && element.IsValidObject && !element.Id.Equals(ElementId.InvalidElementId);
        }
    }
}