using System;
using System.Collections.Generic;

namespace RevitDtools.Core.Services
{
    /// <summary>
    /// Comprehensive result of build validation process
    /// </summary>
    public class BuildValidationResult
    {
        public string Configuration { get; set; } = "Debug";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan TotalDuration { get; set; }
        
        // Build execution results
        public bool BuildSuccessful { get; set; }
        public int BuildExitCode { get; set; }
        public string BuildOutput { get; set; } = string.Empty;
        
        // Error and warning analysis
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public List<BuildError> BuildErrors { get; set; } = new List<BuildError>();
        public List<BuildWarning> BuildWarnings { get; set; } = new List<BuildWarning>();
        
        // Namespace conflict categorization
        public Dictionary<ConflictType, List<BuildError>> ConflictsByType { get; set; } = new Dictionary<ConflictType, List<BuildError>>();
        public Dictionary<ConflictType, int> ConflictSummary { get; set; } = new Dictionary<ConflictType, int>();
        
        // Analysis and recommendations
        public BuildAnalysis Analysis { get; set; } = new BuildAnalysis();
        
        // Functionality testing results
        public List<FunctionalityTestResult> FunctionalityTests { get; set; } = new List<FunctionalityTestResult>();
        
        // Validation errors (system-level issues)
        public List<string> ValidationErrors { get; set; } = new List<string>();
        
        /// <summary>
        /// Gets a summary of the validation results
        /// </summary>
        public string GetSummary()
        {
            var summary = new List<string>();
            
            summary.Add($"Build Validation Summary ({Configuration} configuration)");
            summary.Add($"Duration: {TotalDuration.TotalSeconds:F2} seconds");
            summary.Add($"Build Status: {(BuildSuccessful ? "SUCCESS" : "FAILED")}");
            summary.Add($"Errors: {ErrorCount}, Warnings: {WarningCount}");
            
            if (ConflictSummary.Count > 0)
            {
                summary.Add("\nNamespace Conflicts by Type:");
                foreach (var conflict in ConflictSummary)
                {
                    summary.Add($"  {conflict.Key}: {conflict.Value} conflicts");
                }
            }
            
            if (Analysis.Recommendations.Count > 0)
            {
                summary.Add("\nRecommendations:");
                foreach (var recommendation in Analysis.Recommendations)
                {
                    summary.Add($"  • {recommendation}");
                }
            }
            
            if (FunctionalityTests.Count > 0)
            {
                var passedTests = FunctionalityTests.Count(t => t.Success);
                summary.Add($"\nFunctionality Tests: {passedTests}/{FunctionalityTests.Count} passed");
            }
            
            return string.Join(Environment.NewLine, summary);
        }
        
        /// <summary>
        /// Gets detailed error information
        /// </summary>
        public string GetDetailedErrors()
        {
            if (BuildErrors.Count == 0)
                return "No build errors found.";
                
            var details = new List<string>();
            details.Add("Detailed Build Errors:");
            details.Add(new string('=', 50));
            
            foreach (var error in BuildErrors.Take(20)) // Limit to first 20 errors
            {
                details.Add($"\nFile: {error.FilePath}");
                details.Add($"Line: {error.LineNumber}, Column: {error.ColumnNumber}");
                details.Add($"Code: {error.ErrorCode}");
                details.Add($"Message: {error.Message}");
                details.Add(new string('-', 30));
            }
            
            if (BuildErrors.Count > 20)
            {
                details.Add($"\n... and {BuildErrors.Count - 20} more errors");
            }
            
            return string.Join(Environment.NewLine, details);
        }
    }
    
    /// <summary>
    /// Represents a build error
    /// </summary>
    public class BuildError
    {
        public string FilePath { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public int ColumnNumber { get; set; }
        public string ErrorCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string FullText { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Represents a build warning
    /// </summary>
    public class BuildWarning
    {
        public string FilePath { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public int ColumnNumber { get; set; }
        public string WarningCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string FullText { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Types of namespace conflicts
    /// </summary>
    public enum ConflictType
    {
        TaskDialog,
        MessageBox,
        UIControls,
        FileDialogs,
        View,
        Other
    }
    
    /// <summary>
    /// Analysis of build results
    /// </summary>
    public class BuildAnalysis
    {
        public Dictionary<string, int> MostProblematicFiles { get; set; } = new Dictionary<string, int>();
        public Dictionary<ConflictType, double> ConflictTypePercentages { get; set; } = new Dictionary<ConflictType, double>();
        public List<string> Recommendations { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Result of a functionality test
    /// </summary>
    public class FunctionalityTestResult
    {
        public string TestName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
    }
}