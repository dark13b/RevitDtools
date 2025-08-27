using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RevitDtools.Core.Interfaces;
using RevitDtools.Utilities;
namespace
 RevitDtools.Core.Services
{
    /// <summary>
    /// Orchestrates the complete namespace conflict resolution process
    /// Coordinates detection, categorization, resolution, and validation
    /// </summary>
    public class ConflictResolutionOrchestrator
    {
        private readonly ILogger _logger;
        private readonly TaskDialogResolver _taskDialogResolver;
        private readonly MessageBoxResolver _messageBoxResolver;
        private readonly UIControlResolver _uiControlResolver;
        private readonly DialogResolver _dialogResolver;
        private readonly ViewResolver _viewResolver;
        private readonly BuildValidator _buildValidator;
        private readonly BackupManager _backupManager;
        
        public ConflictResolutionOrchestrator(ILogger? logger = null)
        {
            _logger = logger ?? Logger.Instance;
            _taskDialogResolver = new TaskDialogResolver();
            _messageBoxResolver = new MessageBoxResolver();
            _uiControlResolver = new UIControlResolver(_logger);
            _dialogResolver = new DialogResolver();
            _viewResolver = new ViewResolver();
            _buildValidator = new BuildValidator();
            _backupManager = new BackupManager();
        }
        
        /// <summary>
        /// Executes the complete conflict resolution process
        /// </summary>
        /// <param name="projectPath">Path to the project root</param>
        /// <param name="createBackup">Whether to create a backup before making changes</param>
        /// <returns>Complete orchestration result</returns>
        public async Task<OrchestrationResult> ResolveAllConflictsAsync(string projectPath = null, bool createBackup = true)
        {
            var result = new OrchestrationResult
            {
                ProjectPath = projectPath ?? Directory.GetCurrentDirectory(),
                StartTime = DateTime.Now
            };
            
            _logger.LogInfo("Starting namespace conflict resolution orchestration");
            
            try
            {
                // Phase 1: Initial Detection and Analysis
                _logger.LogInfo("Phase 1: Detecting and analyzing conflicts");
                await DetectAndAnalyzeConflicts(result);
                
                if (result.InitialConflictCount == 0)
                {
                    _logger.LogInfo("No conflicts detected. Resolution complete.");
                    result.Success = true;
                    result.EndTime = DateTime.Now;
                    return result;
                }
                
                // Phase 2: Create Backup
                if (createBackup)
                {
                    _logger.LogInfo("Phase 2: Creating backup");
                    await CreateProjectBackup(result);
                }
                
                // Phase 3: Systematic Resolution
                _logger.LogInfo("Phase 3: Resolving conflicts systematically");
                await ResolveConflictsSystematically(result);
                
                // Phase 4: Build Validation
                _logger.LogInfo("Phase 4: Validating build after resolution");
                await ValidateBuildAfterResolution(result);
                
                // Phase 5: Final Analysis
                _logger.LogInfo("Phase 5: Performing final analysis");
                await PerformFinalAnalysis(result);
                
                result.Success = result.FinalBuildResult?.BuildSuccessful ?? false;
                result.EndTime = DateTime.Now;
                
                _logger.LogInfo($"Orchestration completed. Success: {result.Success}");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Orchestration failed with exception");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTime.Now;
                return result;
            }
        }
        
        /// <summary>
        /// Phase 1: Detect and analyze all conflicts in the project
        /// </summary>
        private async Task DetectAndAnalyzeConflicts(OrchestrationResult result)
        {
            try
            {
                // Get initial build status
                _logger.LogInfo("Getting initial build status");
                result.InitialBuildResult = await _buildValidator.ValidateBuildAsync();
                result.InitialConflictCount = result.InitialBuildResult.ErrorCount;
                
                // Detect specific conflict types
                _logger.LogInfo("Detecting TaskDialog conflicts");
                result.ConflictDetection.TaskDialogConflicts = await DetectTaskDialogConflictsAsync(result.ProjectPath);
                
                _logger.LogInfo("Detecting MessageBox conflicts");
                result.ConflictDetection.MessageBoxConflicts = _messageBoxResolver.DetectConflicts(result.ProjectPath);
                
                _logger.LogInfo("Detecting UI Control conflicts");
                result.ConflictDetection.UIControlConflicts = _uiControlResolver.ScanForConflicts(result.ProjectPath);
                
                _logger.LogInfo("Detecting Dialog conflicts");
                result.ConflictDetection.DialogConflicts = _dialogResolver.ScanForDialogConflicts(result.ProjectPath);
                
                _logger.LogInfo("Detecting View conflicts");
                result.ConflictDetection.ViewConflicts = await DetectViewConflictsAsync(result.ProjectPath);
                
                // Log detection summary
                LogDetectionSummary(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during conflict detection");
                throw;
            }
        }
        
        /// <summary>
        /// Phase 2: Create comprehensive backup of all files that will be modified
        /// </summary>
        private async Task CreateProjectBackup(OrchestrationResult result)
        {
            try
            {
                var filesToBackup = new HashSet<string>();
                
                // Add files from each conflict type
                filesToBackup.UnionWith(result.ConflictDetection.TaskDialogConflicts);
                filesToBackup.UnionWith(result.ConflictDetection.MessageBoxConflicts.Select(c => c.FilePath));
                filesToBackup.UnionWith(result.ConflictDetection.UIControlConflicts);
                filesToBackup.UnionWith(result.ConflictDetection.DialogConflicts.Select(c => c.FilePath));
                filesToBackup.UnionWith(result.ConflictDetection.ViewConflicts);
                
                if (filesToBackup.Any())
                {
                    var backupName = $"ConflictResolution_{DateTime.Now:yyyyMMdd_HHmmss}";
                    result.BackupSession = await _backupManager.CreateBackupAsync(filesToBackup, backupName);
                    
                    _logger.LogInfo($"Created backup session '{backupName}' with {filesToBackup.Count} files");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating backup");
                throw;
            }
        }
        
        /// <summary>
        /// Phase 3: Resolve conflicts systematically by type
        /// </summary>
        private async Task ResolveConflictsSystematically(OrchestrationResult result)
        {
            try
            {
                // Step 1: Resolve TaskDialog conflicts
                await ResolveTaskDialogConflicts(result);
                
                // Step 2: Resolve MessageBox conflicts
                await ResolveMessageBoxConflicts(result);
                
                // Step 3: Resolve UI Control conflicts
                await ResolveUIControlConflicts(result);
                
                // Step 4: Resolve Dialog conflicts
                await ResolveDialogConflicts(result);
                
                // Step 5: Resolve View conflicts
                await ResolveViewConflicts(result);
                
                // Intermediate build validation after each major step
                await ValidateIntermediateBuild(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during systematic resolution");
                throw;
            }
        }
        
        /// <summary>
        /// Resolve TaskDialog conflicts
        /// </summary>
        private async Task ResolveTaskDialogConflicts(OrchestrationResult result)
        {
            if (!result.ConflictDetection.TaskDialogConflicts.Any())
            {
                _logger.LogInfo("No TaskDialog conflicts to resolve");
                return;
            }
            
            _logger.LogInfo($"Resolving TaskDialog conflicts in {result.ConflictDetection.TaskDialogConflicts.Count} files");
            
            var stepResult = new ResolutionStepResult
            {
                StepName = "TaskDialog Resolution",
                StartTime = DateTime.Now
            };
            
            try
            {
                var modifiedFiles = _taskDialogResolver.ProcessFiles(result.ConflictDetection.TaskDialogConflicts);
                stepResult.FilesModified = modifiedFiles;
                stepResult.Success = true;
                
                _logger.LogInfo($"TaskDialog resolution completed. Modified {modifiedFiles.Count} files");
            }
            catch (Exception ex)
            {
                stepResult.Success = false;
                stepResult.ErrorMessage = ex.Message;
                _logger.LogError(ex, "TaskDialog resolution failed");
            }
            
            stepResult.EndTime = DateTime.Now;
            result.ResolutionSteps.Add(stepResult);
        }
        
        /// <summary>
        /// Resolve MessageBox conflicts
        /// </summary>
        private async Task ResolveMessageBoxConflicts(OrchestrationResult result)
        {
            if (!result.ConflictDetection.MessageBoxConflicts.Any())
            {
                _logger.LogInfo("No MessageBox conflicts to resolve");
                return;
            }
            
            _logger.LogInfo($"Resolving MessageBox conflicts in {result.ConflictDetection.MessageBoxConflicts.Count} files");
            
            var stepResult = new ResolutionStepResult
            {
                StepName = "MessageBox Resolution",
                StartTime = DateTime.Now
            };
            
            try
            {
                var resolutionResults = _messageBoxResolver.ResolveAllConflicts(result.ProjectPath);
                stepResult.FilesModified = resolutionResults.Where(r => r.Success).Select(r => r.FilePath).ToList();
                stepResult.Success = resolutionResults.All(r => r.Success);
                
                if (!stepResult.Success)
                {
                    var failures = resolutionResults.Where(r => !r.Success).Select(r => r.Message);
                    stepResult.ErrorMessage = string.Join("; ", failures);
                }
                
                _logger.LogInfo($"MessageBox resolution completed. Modified {stepResult.FilesModified.Count} files");
            }
            catch (Exception ex)
            {
                stepResult.Success = false;
                stepResult.ErrorMessage = ex.Message;
                _logger.LogError(ex, "MessageBox resolution failed");
            }
            
            stepResult.EndTime = DateTime.Now;
            result.ResolutionSteps.Add(stepResult);
        }
        
        /// <summary>
        /// Resolve UI Control conflicts
        /// </summary>
        private async Task ResolveUIControlConflicts(OrchestrationResult result)
        {
            if (!result.ConflictDetection.UIControlConflicts.Any())
            {
                _logger.LogInfo("No UI Control conflicts to resolve");
                return;
            }
            
            _logger.LogInfo($"Resolving UI Control conflicts in {result.ConflictDetection.UIControlConflicts.Count} files");
            
            var stepResult = new ResolutionStepResult
            {
                StepName = "UI Control Resolution",
                StartTime = DateTime.Now
            };
            
            try
            {
                var modifiedCount = _uiControlResolver.ResolveConflictsInFiles(result.ConflictDetection.UIControlConflicts);
                stepResult.FilesModified = result.ConflictDetection.UIControlConflicts.Take(modifiedCount).ToList();
                stepResult.Success = true;
                
                _logger.LogInfo($"UI Control resolution completed. Modified {modifiedCount} files");
            }
            catch (Exception ex)
            {
                stepResult.Success = false;
                stepResult.ErrorMessage = ex.Message;
                _logger.LogError(ex, "UI Control resolution failed");
            }
            
            stepResult.EndTime = DateTime.Now;
            result.ResolutionSteps.Add(stepResult);
        }
        
        /// <summary>
        /// Resolve Dialog conflicts
        /// </summary>
        private async Task ResolveDialogConflicts(OrchestrationResult result)
        {
            if (!result.ConflictDetection.DialogConflicts.Any())
            {
                _logger.LogInfo("No Dialog conflicts to resolve");
                return;
            }
            
            _logger.LogInfo($"Resolving Dialog conflicts in {result.ConflictDetection.DialogConflicts.Count} files");
            
            var stepResult = new ResolutionStepResult
            {
                StepName = "Dialog Resolution",
                StartTime = DateTime.Now
            };
            
            try
            {
                var modifiedFiles = new List<string>();
                
                foreach (var conflictInfo in result.ConflictDetection.DialogConflicts)
                {
                    var resolutionResult = _dialogResolver.ResolveDialogConflicts(conflictInfo.FilePath);
                    if (resolutionResult.Success && resolutionResult.ChangesApplied)
                    {
                        modifiedFiles.Add(conflictInfo.FilePath);
                    }
                }
                
                stepResult.FilesModified = modifiedFiles;
                stepResult.Success = true;
                
                _logger.LogInfo($"Dialog resolution completed. Modified {modifiedFiles.Count} files");
            }
            catch (Exception ex)
            {
                stepResult.Success = false;
                stepResult.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Dialog resolution failed");
            }
            
            stepResult.EndTime = DateTime.Now;
            result.ResolutionSteps.Add(stepResult);
        }
        
        /// <summary>
        /// Resolve View conflicts
        /// </summary>
        private async Task ResolveViewConflicts(OrchestrationResult result)
        {
            if (!result.ConflictDetection.ViewConflicts.Any())
            {
                _logger.LogInfo("No View conflicts to resolve");
                return;
            }
            
            _logger.LogInfo($"Resolving View conflicts in {result.ConflictDetection.ViewConflicts.Count} files");
            
            var stepResult = new ResolutionStepResult
            {
                StepName = "View Resolution",
                StartTime = DateTime.Now
            };
            
            try
            {
                var modifiedFiles = _viewResolver.ProcessFiles(result.ConflictDetection.ViewConflicts);
                stepResult.FilesModified = modifiedFiles;
                stepResult.Success = true;
                
                _logger.LogInfo($"View resolution completed. Modified {modifiedFiles.Count} files");
            }
            catch (Exception ex)
            {
                stepResult.Success = false;
                stepResult.ErrorMessage = ex.Message;
                _logger.LogError(ex, "View resolution failed");
            }
            
            stepResult.EndTime = DateTime.Now;
            result.ResolutionSteps.Add(stepResult);
        }
        
        /// <summary>
        /// Validate build after intermediate steps
        /// </summary>
        private async Task ValidateIntermediateBuild(OrchestrationResult result)
        {
            try
            {
                _logger.LogInfo("Performing intermediate build validation");
                var buildResult = await _buildValidator.ValidateBuildAsync();
                
                _logger.LogInfo($"Intermediate build result: {(buildResult.BuildSuccessful ? "SUCCESS" : "FAILED")} " +
                              $"({buildResult.ErrorCount} errors, {buildResult.WarningCount} warnings)");
                
                // Store intermediate results for analysis
                result.IntermediateBuildResults.Add(buildResult);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Intermediate build validation failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Phase 4: Final build validation
        /// </summary>
        private async Task ValidateBuildAfterResolution(OrchestrationResult result)
        {
            try
            {
                _logger.LogInfo("Performing final build validation");
                result.FinalBuildResult = await _buildValidator.ValidateBuildAsync();
                
                _logger.LogInfo($"Final build result: {(result.FinalBuildResult.BuildSuccessful ? "SUCCESS" : "FAILED")} " +
                              $"({result.FinalBuildResult.ErrorCount} errors, {result.FinalBuildResult.WarningCount} warnings)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Final build validation failed");
                throw;
            }
        }
        
        /// <summary>
        /// Phase 5: Perform final analysis and generate report
        /// </summary>
        private async Task PerformFinalAnalysis(OrchestrationResult result)
        {
            try
            {
                // Calculate resolution effectiveness
                var initialErrors = result.InitialBuildResult?.ErrorCount ?? 0;
                var finalErrors = result.FinalBuildResult?.ErrorCount ?? 0;
                var errorsResolved = Math.Max(0, initialErrors - finalErrors);
                
                result.Analysis = new OrchestrationAnalysis
                {
                    InitialErrorCount = initialErrors,
                    FinalErrorCount = finalErrors,
                    ErrorsResolved = errorsResolved,
                    ResolutionEffectiveness = initialErrors > 0 ? (double)errorsResolved / initialErrors * 100 : 100,
                    TotalFilesModified = result.ResolutionSteps.SelectMany(s => s.FilesModified).Distinct().Count(),
                    SuccessfulSteps = result.ResolutionSteps.Count(s => s.Success),
                    TotalSteps = result.ResolutionSteps.Count
                };
                
                // Generate recommendations
                GenerateRecommendations(result);
                
                _logger.LogInfo($"Analysis complete: {errorsResolved}/{initialErrors} errors resolved " +
                              $"({result.Analysis.ResolutionEffectiveness:F1}% effectiveness)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Final analysis failed");
            }
        }
        
        /// <summary>
        /// Generate recommendations based on results
        /// </summary>
        private void GenerateRecommendations(OrchestrationResult result)
        {
            result.Analysis.Recommendations = new List<string>();
            
            if (result.FinalBuildResult?.BuildSuccessful == true)
            {
                result.Analysis.Recommendations.Add("✅ All namespace conflicts successfully resolved!");
                result.Analysis.Recommendations.Add("✅ Project builds without errors");
                
                if (result.BackupSession != null)
                {
                    result.Analysis.Recommendations.Add($"💾 Backup created: {result.BackupSession.Name}");
                }
            }
            else
            {
                if (result.FinalBuildResult?.ErrorCount > 0)
                {
                    result.Analysis.Recommendations.Add($"⚠️ {result.FinalBuildResult.ErrorCount} errors remain after resolution");
                    
                    if (result.FinalBuildResult.ConflictSummary.Any())
                    {
                        result.Analysis.Recommendations.Add("🔍 Remaining conflicts by type:");
                        foreach (var conflict in result.FinalBuildResult.ConflictSummary)
                        {
                            result.Analysis.Recommendations.Add($"   • {conflict.Key}: {conflict.Value} conflicts");
                        }
                    }
                }
                
                var failedSteps = result.ResolutionSteps.Where(s => !s.Success).ToList();
                if (failedSteps.Any())
                {
                    result.Analysis.Recommendations.Add("❌ Failed resolution steps:");
                    foreach (var step in failedSteps)
                    {
                        result.Analysis.Recommendations.Add($"   • {step.StepName}: {step.ErrorMessage}");
                    }
                }
                
                if (result.BackupSession != null)
                {
                    result.Analysis.Recommendations.Add($"🔄 Consider rollback using session: {result.BackupSession.Id}");
                }
            }
        }
        
        /// <summary>
        /// Rollback all changes using the backup session
        /// </summary>
        public async Task<RollbackResult> RollbackChangesAsync(string sessionId = null)
        {
            try
            {
                var targetSessionId = sessionId;
                
                if (string.IsNullOrEmpty(targetSessionId))
                {
                    // Find the most recent session
                    var sessions = await _backupManager.ListBackupSessionsAsync();
                    var recentSession = sessions.OrderByDescending(s => s.CreatedAt).FirstOrDefault();
                    targetSessionId = recentSession?.Id;
                }
                
                if (string.IsNullOrEmpty(targetSessionId))
                {
                    return new RollbackResult
                    {
                        Success = false,
                        ErrorMessage = "No backup session found for rollback"
                    };
                }
                
                _logger.LogInfo($"Rolling back changes using session: {targetSessionId}");
                var result = await _backupManager.RollbackAsync(targetSessionId);
                
                _logger.LogInfo($"Rollback completed. Success: {result.Success}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rollback failed");
                return new RollbackResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
        
        /// <summary>
        /// Generate comprehensive progress report
        /// </summary>
        public string GenerateProgressReport(OrchestrationResult result)
        {
            var report = new List<string>();
            
            report.Add("Namespace Conflict Resolution Report");
            report.Add("=====================================");
            report.Add($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.Add($"Project: {result.ProjectPath}");
            report.Add($"Duration: {(result.EndTime - result.StartTime).TotalMinutes:F2} minutes");
            report.Add($"Status: {(result.Success ? "SUCCESS" : "FAILED")}");
            report.Add("");
            
            // Initial state
            report.Add("Initial State:");
            report.Add($"  Build Status: {(result.InitialBuildResult?.BuildSuccessful == true ? "FAILED" : "FAILED")}");
            report.Add($"  Total Errors: {result.InitialBuildResult?.ErrorCount ?? 0}");
            report.Add($"  Total Warnings: {result.InitialBuildResult?.WarningCount ?? 0}");
            report.Add("");
            
            // Conflict detection summary
            report.Add("Conflicts Detected:");
            report.Add($"  TaskDialog: {result.ConflictDetection.TaskDialogConflicts.Count} files");
            report.Add($"  MessageBox: {result.ConflictDetection.MessageBoxConflicts.Count} files");
            report.Add($"  UI Controls: {result.ConflictDetection.UIControlConflicts.Count} files");
            report.Add($"  Dialogs: {result.ConflictDetection.DialogConflicts.Count} files");
            report.Add($"  Views: {result.ConflictDetection.ViewConflicts.Count} files");
            report.Add("");
            
            // Resolution steps
            report.Add("Resolution Steps:");
            foreach (var step in result.ResolutionSteps)
            {
                var status = step.Success ? "✅" : "❌";
                var duration = (step.EndTime - step.StartTime).TotalSeconds;
                report.Add($"  {status} {step.StepName}: {step.FilesModified.Count} files modified ({duration:F1}s)");
                
                if (!step.Success && !string.IsNullOrEmpty(step.ErrorMessage))
                {
                    report.Add($"      Error: {step.ErrorMessage}");
                }
            }
            report.Add("");
            
            // Final state
            if (result.FinalBuildResult != null)
            {
                report.Add("Final State:");
                report.Add($"  Build Status: {(result.FinalBuildResult.BuildSuccessful ? "SUCCESS" : "FAILED")}");
                report.Add($"  Total Errors: {result.FinalBuildResult.ErrorCount}");
                report.Add($"  Total Warnings: {result.FinalBuildResult.WarningCount}");
                report.Add("");
            }
            
            // Analysis
            if (result.Analysis != null)
            {
                report.Add("Analysis:");
                report.Add($"  Errors Resolved: {result.Analysis.ErrorsResolved}/{result.Analysis.InitialErrorCount}");
                report.Add($"  Resolution Effectiveness: {result.Analysis.ResolutionEffectiveness:F1}%");
                report.Add($"  Files Modified: {result.Analysis.TotalFilesModified}");
                report.Add($"  Successful Steps: {result.Analysis.SuccessfulSteps}/{result.Analysis.TotalSteps}");
                report.Add("");
                
                if (result.Analysis.Recommendations.Any())
                {
                    report.Add("Recommendations:");
                    foreach (var recommendation in result.Analysis.Recommendations)
                    {
                        report.Add($"  {recommendation}");
                    }
                    report.Add("");
                }
            }
            
            // Backup information
            if (result.BackupSession != null)
            {
                report.Add("Backup Information:");
                report.Add($"  Session ID: {result.BackupSession.Id}");
                report.Add($"  Session Name: {result.BackupSession.Name}");
                report.Add($"  Files Backed Up: {result.BackupSession.BackedUpFiles.Count}");
                report.Add($"  Created: {result.BackupSession.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                report.Add("");
            }
            
            return string.Join(Environment.NewLine, report);
        }
        
        // Helper methods for conflict detection
        private async Task<List<string>> DetectTaskDialogConflictsAsync(string projectPath)
        {
            var conflicts = new List<string>();
            var csFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))
                .ToList();
            
            foreach (var file in csFiles)
            {
                try
                {
                    var summary = _taskDialogResolver.AnalyzeFile(file);
                    if (summary.HasTaskDialogUsage && !summary.HasRevitTaskDialogAlias)
                    {
                        conflicts.Add(file);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Error analyzing TaskDialog conflicts in {file}: {ex.Message}");
                }
            }
            
            return conflicts;
        }
        
        private async Task<List<string>> DetectViewConflictsAsync(string projectPath)
        {
            var conflicts = new List<string>();
            var csFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))
                .ToList();
            
            foreach (var file in csFiles)
            {
                try
                {
                    var summary = _viewResolver.AnalyzeFile(file);
                    if (summary.HasViewUsage && summary.HasRevitContext && !summary.HasRevitViewAlias)
                    {
                        conflicts.Add(file);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Error analyzing View conflicts in {file}: {ex.Message}");
                }
            }
            
            return conflicts;
        }
        
        private void LogDetectionSummary(OrchestrationResult result)
        {
            _logger.LogInfo("Conflict Detection Summary:");
            _logger.LogInfo($"  Initial Build Errors: {result.InitialBuildResult?.ErrorCount ?? 0}");
            _logger.LogInfo($"  TaskDialog Conflicts: {result.ConflictDetection.TaskDialogConflicts.Count} files");
            _logger.LogInfo($"  MessageBox Conflicts: {result.ConflictDetection.MessageBoxConflicts.Count} files");
            _logger.LogInfo($"  UI Control Conflicts: {result.ConflictDetection.UIControlConflicts.Count} files");
            _logger.LogInfo($"  Dialog Conflicts: {result.ConflictDetection.DialogConflicts.Count} files");
            _logger.LogInfo($"  View Conflicts: {result.ConflictDetection.ViewConflicts.Count} files");
        }
    }
    
    /// <summary>
    /// Complete result of the orchestration process
    /// </summary>
    public class OrchestrationResult
    {
        public string ProjectPath { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        
        // Initial state
        public BuildValidationResult? InitialBuildResult { get; set; }
        public int InitialConflictCount { get; set; }
        
        // Conflict detection results
        public ConflictDetectionResult ConflictDetection { get; set; } = new ConflictDetectionResult();
        
        // Backup information
        public BackupSession? BackupSession { get; set; }
        
        // Resolution process
        public List<ResolutionStepResult> ResolutionSteps { get; set; } = new List<ResolutionStepResult>();
        public List<BuildValidationResult> IntermediateBuildResults { get; set; } = new List<BuildValidationResult>();
        
        // Final state
        public BuildValidationResult? FinalBuildResult { get; set; }
        
        // Analysis
        public OrchestrationAnalysis? Analysis { get; set; }
        
        public TimeSpan TotalDuration => EndTime - StartTime;
    }
    
    /// <summary>
    /// Results of conflict detection phase
    /// </summary>
    public class ConflictDetectionResult
    {
        public List<string> TaskDialogConflicts { get; set; } = new List<string>();
        public List<MessageBoxConflict> MessageBoxConflicts { get; set; } = new List<MessageBoxConflict>();
        public List<string> UIControlConflicts { get; set; } = new List<string>();
        public List<DialogConflictInfo> DialogConflicts { get; set; } = new List<DialogConflictInfo>();
        public List<string> ViewConflicts { get; set; } = new List<string>();
        
        public int TotalConflictingFiles => 
            TaskDialogConflicts.Count + 
            MessageBoxConflicts.Count + 
            UIControlConflicts.Count + 
            DialogConflicts.Count + 
            ViewConflicts.Count;
    }
    
    /// <summary>
    /// Result of a single resolution step
    /// </summary>
    public class ResolutionStepResult
    {
        public string StepName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> FilesModified { get; set; } = new List<string>();
        
        public TimeSpan Duration => EndTime - StartTime;
    }
    
    /// <summary>
    /// Analysis of the orchestration results
    /// </summary>
    public class OrchestrationAnalysis
    {
        public int InitialErrorCount { get; set; }
        public int FinalErrorCount { get; set; }
        public int ErrorsResolved { get; set; }
        public double ResolutionEffectiveness { get; set; }
        public int TotalFilesModified { get; set; }
        public int SuccessfulSteps { get; set; }
        public int TotalSteps { get; set; }
        public List<string> Recommendations { get; set; } = new List<string>();
    }
}