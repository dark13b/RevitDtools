using Autodesk.Revit.DB;
using RevitDtools.Core.Interfaces;
using RevitDtools.Core.Models;
using RevitDtools.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RevitDtools.Core.Services
{
    /// <summary>
    /// Service for batch processing multiple DWG files
    /// </summary>
    public class BatchProcessingService : BaseService, IBatchProcessor
    {
        private readonly IGeometryProcessor _geometryProcessor;
        private CancellationTokenSource _cancellationTokenSource;
        private BatchProcessingStatus _currentStatus;
        private readonly object _statusLock = new object();

        public BatchProcessingService(Document document, ILogger logger, IGeometryProcessor geometryProcessor) 
            : base(document, logger)
        {
            _geometryProcessor = geometryProcessor ?? throw new ArgumentNullException(nameof(geometryProcessor));
            _currentStatus = BatchProcessingStatus.NotStarted;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Process multiple DWG files with progress reporting
        /// </summary>
        public async Task<BatchResult> ProcessMultipleFiles(List<string> filePaths, IProgress<BatchProgress> progress, CancellationToken cancellationToken = default)
        {
            if (filePaths == null || !filePaths.Any())
            {
                throw new ArgumentException("File paths cannot be null or empty", nameof(filePaths));
            }

            SetStatus(BatchProcessingStatus.Running);
            var stopwatch = Stopwatch.StartNew();
            var result = new BatchResult
            {
                ProcessingStartTime = DateTime.Now
            };

            try
            {
                Logger.LogInfo($"Starting batch processing of {filePaths.Count} files");

                // Validate all files exist before processing
                var validFiles = ValidateFiles(filePaths);
                if (validFiles.Count != filePaths.Count)
                {
                    Logger.LogWarning($"Only {validFiles.Count} of {filePaths.Count} files are valid and will be processed");
                }

                var totalFiles = validFiles.Count;
                var processedFiles = 0;

                foreach (var filePath in validFiles)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        result.WasCancelled = true;
                        break;
                    }

                    var fileName = Path.GetFileName(filePath);
                    Logger.LogInfo($"Processing file {processedFiles + 1}/{totalFiles}: {fileName}");

                    // Report progress
                    var progressInfo = new BatchProgress
                    {
                        CurrentFile = processedFiles + 1,
                        TotalFiles = totalFiles,
                        CurrentFileName = fileName,
                        CurrentOperation = "Processing DWG file",
                        ElapsedTime = stopwatch.Elapsed
                    };

                    if (processedFiles > 0)
                    {
                        var avgTimePerFile = stopwatch.Elapsed.TotalSeconds / processedFiles;
                        var remainingFiles = totalFiles - processedFiles;
                        progressInfo.EstimatedTimeRemaining = TimeSpan.FromSeconds(avgTimePerFile * remainingFiles);
                    }

                    progress?.Report(progressInfo);

                    // Process individual file
                    var fileResult = await ProcessSingleFile(filePath, cancellationToken);
                    result.FileResults.Add(fileResult);
                    processedFiles++;
                }

                result.ProcessingEndTime = DateTime.Now;
                result.CalculateSummary();

                SetStatus(result.WasCancelled ? BatchProcessingStatus.Cancelled : BatchProcessingStatus.Completed);
                Logger.LogInfo($"Batch processing completed: {result.Summary}");

                return result;
            }
            catch (Exception ex)
            {
                SetStatus(BatchProcessingStatus.Failed);
                Logger.LogError(ex, "Batch processing failed");
                result.ProcessingEndTime = DateTime.Now;
                result.CalculateSummary();
                throw;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// Process all DWG files in a folder with optional subfolder inclusion
        /// </summary>
        public async Task<BatchResult> ProcessFolder(string folderPath, bool includeSubfolders, IProgress<BatchProgress> progress, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                throw new ArgumentException("Folder path cannot be null or empty", nameof(folderPath));
            }

            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException($"Folder not found: {folderPath}");
            }

            Logger.LogInfo($"Scanning folder for DWG files: {folderPath} (Include subfolders: {includeSubfolders})");

            // Find all DWG files in the folder
            var searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var dwgFiles = Directory.GetFiles(folderPath, "*.dwg", searchOption).ToList();

            Logger.LogInfo($"Found {dwgFiles.Count} DWG files in folder");

            if (!dwgFiles.Any())
            {
                var result = new BatchResult
                {
                    ProcessingStartTime = DateTime.Now,
                    ProcessingEndTime = DateTime.Now,
                    Summary = "No DWG files found in the specified folder"
                };
                result.CalculateSummary();
                return result;
            }

            return await ProcessMultipleFiles(dwgFiles, progress, cancellationToken);
        }

        /// <summary>
        /// Cancel the current batch processing operation
        /// </summary>
        public void CancelProcessing()
        {
            _cancellationTokenSource?.Cancel();
            SetStatus(BatchProcessingStatus.Cancelled);
            Logger.LogInfo("Batch processing cancellation requested");
        }

        /// <summary>
        /// Get the current processing status
        /// </summary>
        public BatchProcessingStatus GetProcessingStatus()
        {
            lock (_statusLock)
            {
                return _currentStatus;
            }
        }

        /// <summary>
        /// Process a single DWG file
        /// </summary>
        private async Task<FileProcessingResult> ProcessSingleFile(string filePath, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new FileProcessingResult
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                ProcessedAt = DateTime.Now
            };

            try
            {
                // Validate file
                if (!File.Exists(filePath))
                {
                    result.Success = false;
                    result.Message = "File not found";
                    result.Errors.Add($"File not found: {filePath}");
                    return result;
                }

                // Check file size (warn for very large files)
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length > 100 * 1024 * 1024) // 100MB
                {
                    result.Warnings.Add($"Large file detected ({fileInfo.Length / (1024 * 1024)}MB) - processing may take longer");
                }

                // Import DWG file into Revit
                using (var transaction = new Transaction(Document, $"Import DWG: {result.FileName}"))
                {
                    transaction.Start();

                    try
                    {
                        // Create DWG import options
                        var importOptions = new DWGImportOptions
                        {
                            ColorMode = ImportColorMode.BlackAndWhite,
                            OrientToView = true,
                            ThisViewOnly = false,
                            VisibleLayersOnly = false
                        };

                        // Import the DWG file
                        var importResult = Document.Import(filePath, importOptions, Document.ActiveView, out var elementId);

                        if (!importResult)
                        {
                            result.Success = false;
                            result.Message = "Failed to import DWG file";
                            result.Errors.Add("DWG import operation failed");
                            transaction.RollBack();
                            return result;
                        }

                        // Get the imported element
                        var importedElement = Document.GetElement(elementId);
                        if (importedElement == null)
                        {
                            result.Success = false;
                            result.Message = "Imported element not found";
                            result.Errors.Add("Could not retrieve imported DWG element");
                            transaction.RollBack();
                            return result;
                        }

                        // Process the imported geometry
                        var processingResult = await ProcessImportedGeometry(importedElement, cancellationToken);
                        
                        result.ElementsProcessed = processingResult.ElementsProcessed;
                        result.ElementsSkipped = processingResult.ElementsSkipped;
                        result.Warnings.AddRange(processingResult.Warnings);
                        result.Errors.AddRange(processingResult.Errors);

                        if (processingResult.Success)
                        {
                            transaction.Commit();
                            result.Success = true;
                            result.Message = $"Successfully processed {result.ElementsProcessed} elements";
                        }
                        else
                        {
                            transaction.RollBack();
                            result.Success = false;
                            result.Message = "Geometry processing failed";
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.RollBack();
                        result.Success = false;
                        result.Message = $"Error during processing: {ex.Message}";
                        result.Errors.Add(ex.Message);
                        Logger.LogError(ex, $"Error processing file: {filePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Unexpected error: {ex.Message}";
                result.Errors.Add(ex.Message);
                Logger.LogError(ex, $"Unexpected error processing file: {filePath}");
            }
            finally
            {
                stopwatch.Stop();
                result.ProcessingTime = stopwatch.Elapsed;
            }

            return result;
        }

        /// <summary>
        /// Process the geometry from an imported DWG element
        /// </summary>
        private async Task<ProcessingResult> ProcessImportedGeometry(Element importedElement, CancellationToken cancellationToken)
        {
            try
            {
                // Use the geometry processor to handle the imported element
                return await Task.Run(() => _geometryProcessor.ProcessElement(importedElement), cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing imported geometry");
                return new ProcessingResult
                {
                    Success = false,
                    Message = $"Geometry processing failed: {ex.Message}",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        /// <summary>
        /// Validate that files exist and are accessible
        /// </summary>
        private List<string> ValidateFiles(List<string> filePaths)
        {
            var validFiles = new List<string>();

            foreach (var filePath in filePaths)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        // Check if file is accessible
                        using (var stream = File.OpenRead(filePath))
                        {
                            validFiles.Add(filePath);
                        }
                    }
                    else
                    {
                        Logger.LogWarning($"File not found: {filePath}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Cannot access file {filePath}: {ex.Message}");
                }
            }

            return validFiles;
        }

        /// <summary>
        /// Set the current processing status thread-safely
        /// </summary>
        private void SetStatus(BatchProcessingStatus status)
        {
            lock (_statusLock)
            {
                _currentStatus = status;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}