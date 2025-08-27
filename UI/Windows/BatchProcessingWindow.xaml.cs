using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Microsoft.Win32;
using RevitDtools.Core.Interfaces;
using RevitDtools.Core.Models;
using RevitDtools.Core.Services;
using RevitDtools.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RevitDtools.UI.Windows
{
    /// <summary>
    /// Interaction logic for BatchProcessingWindow.xaml
    /// </summary>
    public partial class BatchProcessingWindow : Window, INotifyPropertyChanged
    {
        private readonly Document _document;
        private readonly UIDocument _uiDocument;
        private readonly ILogger _logger;
        private readonly FamilyManagementService _familyService;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isProcessing;
        private BatchResult _lastResult;

        public ObservableCollection<LineItem> SelectedLines { get; set; }
        public ObservableCollection<FamilyItem> AvailableFamilies { get; set; }

        public BatchProcessingWindow(Document document, UIDocument uiDocument, ILogger logger)
        {
            InitializeComponent();
            
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _uiDocument = uiDocument ?? throw new ArgumentNullException(nameof(uiDocument));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _familyService = new FamilyManagementService(_document, _logger);
            
            SelectedLines = new ObservableCollection<LineItem>();
            AvailableFamilies = new ObservableCollection<FamilyItem>();
            
            LinesListView.ItemsSource = SelectedLines;
            PrimaryFamilyComboBox.ItemsSource = AvailableFamilies;
            
            DataContext = this;
            
            LoadAvailableFamilies();
            UpdateUI();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SelectLinesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Hide(); // Hide the window during selection
                
                var selection = _uiDocument.Selection;
                var selectionFilter = new LineSelectionFilter();
                var selectedRefs = selection.PickObjects(ObjectType.Element, selectionFilter, "Select lines for column creation");
                
                foreach (var selectedRef in selectedRefs)
                {
                    var element = _document.GetElement(selectedRef);
                    if (element is CurveElement curveElement && curveElement.GeometryCurve is Line line)
                    {
                        AddLineToList(curveElement, line);
                    }
                }
                
                Show(); // Show the window again
                UpdateUI();
            }
            catch (Exception ex) when (ex.Message.Contains("cancelled"))
            {
                Show(); // Show the window if selection was cancelled
            }
            catch (Exception ex)
            {
                Show();
                _logger.LogError(ex, "Error selecting lines");
                System.Windows.MessageBox.Show($"Error selecting lines: {ex.Message}", "Selection Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void SelectAllLinesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SelectedLines.Clear();
                
                // Find all lines in the current view
                var collector = new FilteredElementCollector(_document, _document.ActiveView.Id)
                    .OfClass(typeof(CurveElement))
                    .WhereElementIsNotElementType();

                foreach (CurveElement curveElement in collector)
                {
                    if (curveElement.GeometryCurve is Line line)
                    {
                        AddLineToList(curveElement, line);
                    }
                }
                
                UpdateUI();
                
                if (!SelectedLines.Any())
                {
                    System.Windows.MessageBox.Show("No lines found in the current view.", "No Lines Found", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error selecting all lines");
                System.Windows.MessageBox.Show($"Error selecting all lines: {ex.Message}", "Selection Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ClearSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedLines.Clear();
            UpdateUI();
        }

        private void RefreshFamiliesButton_Click(object sender, RoutedEventArgs e)
        {
            LoadAvailableFamilies();
        }

        private async void StartProcessingButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SelectedLines.Any())
            {
                System.Windows.MessageBox.Show("Please select lines to process.", 
                    "No Lines Selected", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var selectedFamily = PrimaryFamilyComboBox.SelectedItem as FamilyItem;
            if (selectedFamily == null)
            {
                System.Windows.MessageBox.Show("Please select a column family.", 
                    "No Family Selected", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                _isProcessing = true;
                _cancellationTokenSource = new CancellationTokenSource();
                UpdateUI();

                // Clear previous results
                ResultsTextBox.Clear();
                ResetProgress();

                // Create progress reporter
                var progress = new Progress<BatchProgress>(UpdateProgress);

                // Start processing
                AppendToResults("Starting batch column creation...\n");
                _logger.LogInfo($"Starting batch processing of {SelectedLines.Count} lines");

                _lastResult = await ProcessSelectedLines(selectedFamily, progress, _cancellationTokenSource.Token);

                // Display final results
                DisplayFinalResults(_lastResult);
                
                SaveReportButton.IsEnabled = true;
            }
            catch (OperationCanceledException)
            {
                AppendToResults("\nBatch processing was cancelled by user.\n");
                _logger.LogInfo("Batch processing cancelled by user");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during batch processing");
                AppendToResults($"\nError during batch processing: {ex.Message}\n");
                System.Windows.MessageBox.Show($"Error during batch processing: {ex.Message}", 
                    "Processing Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                _isProcessing = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                UpdateUI();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing)
            {
                _cancellationTokenSource?.Cancel();
                AppendToResults("Cancelling batch processing...\n");
            }
            else
            {
                Close();
            }
        }

        private void SaveReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_lastResult == null)
            {
                System.Windows.MessageBox.Show("No processing results to save.", 
                    "No Results", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save Processing Report",
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                DefaultExt = "txt",
                FileName = $"BatchProcessingReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var report = GenerateDetailedReport(_lastResult);
                    File.WriteAllText(saveFileDialog.FileName, report);
                    
                    System.Windows.MessageBox.Show("Report saved successfully.", 
                        "Report Saved", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving report");
                    System.Windows.MessageBox.Show($"Error saving report: {ex.Message}", 
                        "Save Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing)
            {
                var result = System.Windows.MessageBox.Show(
                    "Processing is currently running. Do you want to cancel and close?", 
                    "Processing in Progress", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _cancellationTokenSource?.Cancel();
                    Close();
                }
            }
            else
            {
                Close();
            }
        }

        private void AddLineToList(CurveElement curveElement, Line line)
        {
            if (SelectedLines.Any(l => l.LineId == (int)curveElement.Id.Value))
            {
                return; // Line already in list
            }

            try
            {
                var startPoint = line.GetEndPoint(0);
                var endPoint = line.GetEndPoint(1);
                var length = line.Length;

                var lineItem = new LineItem
                {
                    LineId = (int)curveElement.Id.Value,
                    ElementId = curveElement.Id,
                    Length = $"{length:F2} ft",
                    StartPoint = $"({startPoint.X:F1}, {startPoint.Y:F1}, {startPoint.Z:F1})",
                    EndPoint = $"({endPoint.X:F1}, {endPoint.Y:F1}, {endPoint.Z:F1})",
                    Status = "Ready",
                    LengthValue = length
                };

                // Auto-match family if enabled
                if (AutoMatchDimensionsCheckBox.IsChecked == true)
                {
                    var matchedFamily = FindBestMatchingFamily(length);
                    lineItem.MatchedFamily = matchedFamily?.Name ?? "No match found";
                    lineItem.MatchedFamilySymbol = matchedFamily;
                }

                SelectedLines.Add(lineItem);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not add line to list: {curveElement.Id}. Error: {ex.Message}");
            }
        }

        private void UpdateUI()
        {
            var hasLines = SelectedLines.Any();
            var hasFamily = PrimaryFamilyComboBox.SelectedItem != null;
            var canProcess = hasLines && hasFamily && !_isProcessing;

            StartProcessingButton.IsEnabled = canProcess;
            SelectLinesButton.IsEnabled = !_isProcessing;
            SelectAllLinesButton.IsEnabled = !_isProcessing;
            ClearSelectionButton.IsEnabled = hasLines && !_isProcessing;
            AutoSelectAllCheckBox.IsEnabled = !_isProcessing;
            PrimaryFamilyComboBox.IsEnabled = !_isProcessing;
            RefreshFamiliesButton.IsEnabled = !_isProcessing;

            CancelButton.Content = _isProcessing ? "Cancel Processing" : "Cancel";

            SelectedLinesText.Text = hasLines 
                ? $"{SelectedLines.Count} line(s) selected" 
                : "No lines selected";

            if (_isProcessing)
            {
                ProcessingStatusText.Text = "Processing...";
            }
            else
            {
                ProcessingStatusText.Text = "Ready";
                ResetProgress();
            }
        }

        private void UpdateProgress(BatchProgress progress)
        {
            Dispatcher.Invoke(() =>
            {
                OverallProgressBar.Value = progress.PercentComplete;
                ProgressPercentText.Text = $"{progress.PercentComplete:F1}%";
                CurrentFileText.Text = $"Processing: {progress.CurrentFileName} ({progress.CurrentFile}/{progress.TotalFiles})";
                ElapsedTimeText.Text = $"Elapsed: {progress.ElapsedTime:hh\\:mm\\:ss}";
                
                if (progress.EstimatedTimeRemaining.TotalSeconds > 0)
                {
                    EstimatedTimeText.Text = $"Remaining: {progress.EstimatedTimeRemaining:hh\\:mm\\:ss}";
                }

                AppendToResults($"Processing {progress.CurrentFileName}...\n");
            });
        }

        private void ResetProgress()
        {
            OverallProgressBar.Value = 0;
            ProgressPercentText.Text = "0%";
            CurrentFileText.Text = "";
            ElapsedTimeText.Text = "";
            EstimatedTimeText.Text = "";
            SummaryText.Text = "";
        }

        private void DisplayFinalResults(BatchResult result)
        {
            AppendToResults($"\n=== BATCH PROCESSING COMPLETED ===\n");
            AppendToResults($"Total files processed: {result.TotalFilesProcessed}\n");
            AppendToResults($"Successful: {result.SuccessfulFiles}\n");
            AppendToResults($"Failed: {result.FailedFiles}\n");
            AppendToResults($"Total processing time: {result.TotalProcessingTime:hh\\:mm\\:ss}\n");
            
            if (result.WasCancelled)
            {
                AppendToResults("*** Processing was cancelled ***\n");
            }

            AppendToResults($"\n{result.Summary}\n");

            // Show detailed results for each file
            AppendToResults("\n=== DETAILED RESULTS ===\n");
            foreach (var fileResult in result.FileResults)
            {
                AppendToResults($"\nFile: {fileResult.FileName}\n");
                AppendToResults($"  Status: {(fileResult.Success ? "SUCCESS" : "FAILED")}\n");
                AppendToResults($"  Elements processed: {fileResult.ElementsProcessed}\n");
                AppendToResults($"  Elements skipped: {fileResult.ElementsSkipped}\n");
                AppendToResults($"  Processing time: {fileResult.ProcessingTime:mm\\:ss}\n");
                
                if (fileResult.Warnings.Any())
                {
                    AppendToResults($"  Warnings: {string.Join(", ", fileResult.Warnings)}\n");
                }
                
                if (fileResult.Errors.Any())
                {
                    AppendToResults($"  Errors: {string.Join(", ", fileResult.Errors)}\n");
                }
            }

            SummaryText.Text = result.Summary;
        }

        private void AppendToResults(string text)
        {
            Dispatcher.Invoke(() =>
            {
                ResultsTextBox.AppendText(text);
                ResultsTextBox.ScrollToEnd();
            });
        }

        private string GenerateDetailedReport(BatchResult result)
        {
            var report = new System.Text.StringBuilder();
            
            report.AppendLine("REVITDTOOLS BATCH PROCESSING REPORT");
            report.AppendLine("=====================================");
            report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Processing started: {result.ProcessingStartTime:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Processing completed: {result.ProcessingEndTime:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Total processing time: {result.TotalProcessingTime:hh\\:mm\\:ss}");
            report.AppendLine();
            
            report.AppendLine("SUMMARY");
            report.AppendLine("-------");
            report.AppendLine($"Total files processed: {result.TotalFilesProcessed}");
            report.AppendLine($"Successful: {result.SuccessfulFiles}");
            report.AppendLine($"Failed: {result.FailedFiles}");
            report.AppendLine($"Success rate: {(result.TotalFilesProcessed > 0 ? (double)result.SuccessfulFiles / result.TotalFilesProcessed * 100 : 0):F1}%");
            
            if (result.WasCancelled)
            {
                report.AppendLine("*** Processing was cancelled by user ***");
            }
            
            report.AppendLine();
            report.AppendLine("DETAILED RESULTS");
            report.AppendLine("----------------");
            
            foreach (var fileResult in result.FileResults)
            {
                report.AppendLine($"\nFile: {fileResult.FileName}");
                report.AppendLine($"Path: {fileResult.FilePath}");
                report.AppendLine($"Status: {(fileResult.Success ? "SUCCESS" : "FAILED")}");
                report.AppendLine($"Processed at: {fileResult.ProcessedAt:yyyy-MM-dd HH:mm:ss}");
                report.AppendLine($"Processing time: {fileResult.ProcessingTime:mm\\:ss\\.fff}");
                report.AppendLine($"Elements processed: {fileResult.ElementsProcessed}");
                report.AppendLine($"Elements skipped: {fileResult.ElementsSkipped}");
                
                if (fileResult.Warnings.Any())
                {
                    report.AppendLine("Warnings:");
                    foreach (var warning in fileResult.Warnings)
                    {
                        report.AppendLine($"  - {warning}");
                    }
                }
                
                if (fileResult.Errors.Any())
                {
                    report.AppendLine("Errors:");
                    foreach (var error in fileResult.Errors)
                    {
                        report.AppendLine($"  - {error}");
                    }
                }
                
                if (!string.IsNullOrEmpty(fileResult.Message))
                {
                    report.AppendLine($"Message: {fileResult.Message}");
                }
            }
            
            return report.ToString();
        }

        private void LoadAvailableFamilies()
        {
            try
            {
                AvailableFamilies.Clear();
                
                var columnFamilies = _familyService.GetAvailableColumnFamilies();
                
                foreach (var family in columnFamilies)
                {
                    var familyItem = new FamilyItem
                    {
                        Family = family,
                        DisplayName = family.Name,
                        FamilySymbols = _familyService.GetFamilySymbols(family).ToList()
                    };
                    AvailableFamilies.Add(familyItem);
                }

                // Auto-select M_Concrete-Rectangular-Column if available
                var preferredFamily = AvailableFamilies.FirstOrDefault(f => 
                    f.DisplayName.Contains("M_Concrete-Rectangular-Column") ||
                    f.DisplayName.Contains("Concrete") && f.DisplayName.Contains("Column"));
                
                if (preferredFamily != null)
                {
                    PrimaryFamilyComboBox.SelectedItem = preferredFamily;
                }
                else if (AvailableFamilies.Any())
                {
                    PrimaryFamilyComboBox.SelectedItem = AvailableFamilies.First();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading available families");
                System.Windows.MessageBox.Show($"Error loading families: {ex.Message}", "Family Loading Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }

        private FamilySymbol FindBestMatchingFamily(double lineLength)
        {
            var selectedFamily = PrimaryFamilyComboBox.SelectedItem as FamilyItem;
            if (selectedFamily?.FamilySymbols == null || !selectedFamily.FamilySymbols.Any())
                return null;

            // Find the family symbol with dimensions closest to the line length
            FamilySymbol bestMatch = null;
            double bestScore = double.MaxValue;

            foreach (var symbol in selectedFamily.FamilySymbols)
            {
                try
                {
                    // Get width and depth parameters
                    var widthParam = symbol.LookupParameter("Width") ?? symbol.LookupParameter("b");
                    var depthParam = symbol.LookupParameter("Depth") ?? symbol.LookupParameter("h");

                    if (widthParam != null && depthParam != null)
                    {
                        var width = widthParam.AsDouble();
                        var depth = depthParam.AsDouble();
                        
                        // Calculate how well this symbol matches the line length
                        // Use the smaller dimension as the primary match criteria
                        var minDimension = Math.Min(width, depth);
                        var score = Math.Abs(minDimension - lineLength);
                        
                        if (score < bestScore)
                        {
                            bestScore = score;
                            bestMatch = symbol;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Error evaluating family symbol {symbol.Name}: {ex.Message}");
                }
            }

            return bestMatch;
        }

        private async Task<BatchResult> ProcessSelectedLines(FamilyItem selectedFamily, IProgress<BatchProgress> progress, CancellationToken cancellationToken)
        {
            var result = new BatchResult
            {
                ProcessingStartTime = DateTime.Now
            };

            var totalLines = SelectedLines.Count;
            var processedLines = 0;

            using (var transaction = new Transaction(_document, "Batch Create Columns"))
            {
                transaction.Start();

                try
                {
                    foreach (var lineItem in SelectedLines)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            result.WasCancelled = true;
                            break;
                        }

                        // Report progress
                        var progressInfo = new BatchProgress
                        {
                            CurrentFile = processedLines + 1,
                            TotalFiles = totalLines,
                            CurrentFileName = $"Line {lineItem.LineId}",
                            CurrentOperation = "Creating column"
                        };
                        progress?.Report(progressInfo);

                        // Process individual line
                        var lineResult = await ProcessSingleLine(lineItem, selectedFamily, cancellationToken);
                        result.FileResults.Add(lineResult);
                        
                        // Update line status
                        lineItem.Status = lineResult.Success ? "Completed" : "Failed";
                        
                        processedLines++;
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.RollBack();
                    _logger.LogError(ex, "Error during batch processing transaction");
                    throw;
                }
            }

            result.ProcessingEndTime = DateTime.Now;
            result.CalculateSummary();
            return result;
        }

        private async Task<FileProcessingResult> ProcessSingleLine(LineItem lineItem, FamilyItem selectedFamily, CancellationToken cancellationToken)
        {
            var result = new FileProcessingResult
            {
                FileName = $"Line {lineItem.LineId}",
                FilePath = $"Element ID: {lineItem.ElementId}",
                ProcessedAt = DateTime.Now
            };

            try
            {
                var element = _document.GetElement(lineItem.ElementId);
                if (element is CurveElement curveElement && curveElement.GeometryCurve is Line line)
                {
                    // Determine which family symbol to use
                    FamilySymbol symbolToUse = lineItem.MatchedFamilySymbol;
                    if (symbolToUse == null && selectedFamily.FamilySymbols.Any())
                    {
                        symbolToUse = selectedFamily.FamilySymbols.First();
                    }

                    if (symbolToUse == null)
                    {
                        result.Success = false;
                        result.Message = "No suitable family symbol found";
                        result.Errors.Add("No family symbol available for column creation");
                        return result;
                    }

                    // Activate the symbol if needed
                    if (!symbolToUse.IsActive)
                    {
                        symbolToUse.Activate();
                    }

                    // Create column at line location
                    var startPoint = line.GetEndPoint(0);
                    var endPoint = line.GetEndPoint(1);
                    var midPoint = (startPoint + endPoint) / 2;

                    // Create the column
                    var level = _document.ActiveView.GenLevel ?? 
                               new FilteredElementCollector(_document).OfClass(typeof(Level)).FirstElement() as Level;
                    
                    if (level == null)
                    {
                        result.Success = false;
                        result.Message = "No level found for column placement";
                        result.Errors.Add("Cannot create column without a valid level");
                        return result;
                    }

                    var column = _document.Create.NewFamilyInstance(midPoint, symbolToUse, level, Autodesk.Revit.DB.Structure.StructuralType.Column);
                    
                    if (column != null)
                    {
                        result.Success = true;
                        result.ElementsProcessed = 1;
                        result.Message = $"Successfully created column using {symbolToUse.Name}";
                    }
                    else
                    {
                        result.Success = false;
                        result.Message = "Failed to create column instance";
                        result.Errors.Add("Column creation returned null");
                    }
                }
                else
                {
                    result.Success = false;
                    result.Message = "Element is not a valid line";
                    result.Errors.Add("Selected element is not a line or curve element");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error creating column: {ex.Message}";
                result.Errors.Add(ex.Message);
                _logger.LogError(ex, $"Error processing line {lineItem.LineId}");
            }

            return result;
        }
    }

    /// <summary>
    /// Represents a line item in the batch processing list
    /// </summary>
    public class LineItem : INotifyPropertyChanged
    {
        private string _status;
        private string _matchedFamily;

        public int LineId { get; set; }
        public ElementId ElementId { get; set; }
        public string Length { get; set; }
        public double LengthValue { get; set; }
        public string StartPoint { get; set; }
        public string EndPoint { get; set; }
        public FamilySymbol MatchedFamilySymbol { get; set; }
        
        public string MatchedFamily
        {
            get => _matchedFamily;
            set
            {
                _matchedFamily = value;
                OnPropertyChanged(nameof(MatchedFamily));
            }
        }
        
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents a family item in the family selection list
    /// </summary>
    public class FamilyItem
    {
        public Family Family { get; set; }
        public string DisplayName { get; set; }
        public List<FamilySymbol> FamilySymbols { get; set; } = new List<FamilySymbol>();
    }

    /// <summary>
    /// Selection filter for lines only
    /// </summary>
    public class LineSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is CurveElement curveElement && curveElement.GeometryCurve is Line;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}