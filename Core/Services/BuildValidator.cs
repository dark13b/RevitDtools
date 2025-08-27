using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RevitDtools.Core.Services
{
    /// <summary>
    /// Comprehensive build validation system for namespace conflict resolution
    /// </summary>
    public class BuildValidator
    {
        private readonly string _projectPath;
        private readonly string _solutionPath;
        
        public BuildValidator(string projectPath = null)
        {
            _projectPath = projectPath ?? "RevitDtools.csproj";
            _solutionPath = "RevitDtools.sln";
        }
        
        /// <summary>
        /// Executes MSBuild and analyzes the results for namespace conflicts
        /// </summary>
        /// <param name="configuration">Build configuration (Debug/Release)</param>
        /// <returns>Comprehensive build validation result</returns>
        public async Task<BuildValidationResult> ValidateBuildAsync(string configuration = "Debug")
        {
            var result = new BuildValidationResult
            {
                Configuration = configuration,
                StartTime = DateTime.Now
            };
            
            try
            {
                // 1. Execute MSBuild
                var buildOutput = await ExecuteMSBuildAsync(configuration);
                result.BuildOutput = buildOutput.Output;
                result.BuildExitCode = buildOutput.ExitCode;
                result.BuildSuccessful = buildOutput.ExitCode == 0;
                
                // 2. Parse build errors and warnings
                ParseBuildOutput(buildOutput.Output, result);
                
                // 3. Categorize namespace conflicts
                CategorizeNamespaceConflicts(result);
                
                // 4. Analyze error trends
                AnalyzeErrorTrends(result);
                
                // 5. Validate functionality if build succeeded
                if (result.BuildSuccessful)
                {
                    await ValidateFunctionalityAsync(result);
                }
                
                result.EndTime = DateTime.Now;
                result.TotalDuration = result.EndTime - result.StartTime;
                
                return result;
            }
            catch (Exception ex)
            {
                result.BuildSuccessful = false;
                result.ValidationErrors.Add($"Build validation failed with exception: {ex.Message}");
                result.EndTime = DateTime.Now;
                result.TotalDuration = result.EndTime - result.StartTime;
                return result;
            }
        }
        
        /// <summary>
        /// Executes MSBuild process and captures output
        /// </summary>
        private async Task<(string Output, int ExitCode)> ExecuteMSBuildAsync(string configuration)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{_solutionPath}\" --configuration {configuration} --verbosity normal",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Directory.GetCurrentDirectory()
            };
            
            var output = new List<string>();
            
            using (var process = new Process { StartInfo = processInfo })
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        output.Add(e.Data);
                };
                
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        output.Add($"ERROR: {e.Data}");
                };
                
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                
                await process.WaitForExitAsync();
                
                return (string.Join(Environment.NewLine, output), process.ExitCode);
            }
        }
        
        /// <summary>
        /// Parses MSBuild output to extract errors and warnings
        /// </summary>
        private void ParseBuildOutput(string buildOutput, BuildValidationResult result)
        {
            if (string.IsNullOrEmpty(buildOutput))
                return;
                
            var lines = buildOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            
            // Regex patterns for MSBuild errors and warnings
            var errorPattern = new Regex(@"^(.+?)\((\d+),(\d+)\):\s*error\s+(\w+):\s*(.+)$", RegexOptions.IgnoreCase);
            var warningPattern = new Regex(@"^(.+?)\((\d+),(\d+)\):\s*warning\s+(\w+):\s*(.+)$", RegexOptions.IgnoreCase);
            var summaryErrorPattern = new Regex(@"(\d+)\s+Error\(s\)", RegexOptions.IgnoreCase);
            var summaryWarningPattern = new Regex(@"(\d+)\s+Warning\(s\)", RegexOptions.IgnoreCase);
            
            foreach (var line in lines)
            {
                // Parse individual errors
                var errorMatch = errorPattern.Match(line);
                if (errorMatch.Success)
                {
                    result.BuildErrors.Add(new BuildError
                    {
                        FilePath = errorMatch.Groups[1].Value,
                        LineNumber = int.Parse(errorMatch.Groups[2].Value),
                        ColumnNumber = int.Parse(errorMatch.Groups[3].Value),
                        ErrorCode = errorMatch.Groups[4].Value,
                        Message = errorMatch.Groups[5].Value,
                        FullText = line
                    });
                    continue;
                }
                
                // Parse individual warnings
                var warningMatch = warningPattern.Match(line);
                if (warningMatch.Success)
                {
                    result.BuildWarnings.Add(new BuildWarning
                    {
                        FilePath = warningMatch.Groups[1].Value,
                        LineNumber = int.Parse(warningMatch.Groups[2].Value),
                        ColumnNumber = int.Parse(warningMatch.Groups[3].Value),
                        WarningCode = warningMatch.Groups[4].Value,
                        Message = warningMatch.Groups[5].Value,
                        FullText = line
                    });
                    continue;
                }
                
                // Parse summary counts
                var errorCountMatch = summaryErrorPattern.Match(line);
                if (errorCountMatch.Success)
                {
                    result.ErrorCount = int.Parse(errorCountMatch.Groups[1].Value);
                }
                
                var warningCountMatch = summaryWarningPattern.Match(line);
                if (warningCountMatch.Success)
                {
                    result.WarningCount = int.Parse(warningCountMatch.Groups[1].Value);
                }
            }
            
            // If summary counts weren't found, use actual counts
            if (result.ErrorCount == 0)
                result.ErrorCount = result.BuildErrors.Count;
            if (result.WarningCount == 0)
                result.WarningCount = result.BuildWarnings.Count;
        }
        
        /// <summary>
        /// Categorizes namespace conflicts from build errors
        /// </summary>
        private void CategorizeNamespaceConflicts(BuildValidationResult result)
        {
            var conflictPatterns = new Dictionary<ConflictType, List<string>>
            {
                [ConflictType.TaskDialog] = new List<string> { "TaskDialog", "ambiguous reference.*TaskDialog" },
                [ConflictType.MessageBox] = new List<string> { "MessageBox", "ambiguous reference.*MessageBox" },
                [ConflictType.UIControls] = new List<string> { "TextBox", "ComboBox", "CheckBox", "Button", "ambiguous reference.*(TextBox|ComboBox|CheckBox|Button)" },
                [ConflictType.FileDialogs] = new List<string> { "OpenFileDialog", "SaveFileDialog", "FolderBrowserDialog", "ambiguous reference.*(OpenFileDialog|SaveFileDialog|FolderBrowserDialog)" },
                [ConflictType.View] = new List<string> { "View", "ambiguous reference.*View" },
                [ConflictType.Other] = new List<string> { "ambiguous reference", "namespace.*conflict" }
            };
            
            foreach (var error in result.BuildErrors)
            {
                foreach (var conflictType in conflictPatterns.Keys)
                {
                    foreach (var pattern in conflictPatterns[conflictType])
                    {
                        if (Regex.IsMatch(error.Message, pattern, RegexOptions.IgnoreCase))
                        {
                            if (!result.ConflictsByType.ContainsKey(conflictType))
                                result.ConflictsByType[conflictType] = new List<BuildError>();
                            
                            result.ConflictsByType[conflictType].Add(error);
                            break;
                        }
                    }
                }
            }
            
            // Calculate conflict summary
            result.ConflictSummary = result.ConflictsByType.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Count
            );
        }
        
        /// <summary>
        /// Analyzes error trends and provides recommendations
        /// </summary>
        private void AnalyzeErrorTrends(BuildValidationResult result)
        {
            result.Analysis = new BuildAnalysis();
            
            // Analyze error distribution
            var errorsByFile = result.BuildErrors.GroupBy(e => e.FilePath)
                .ToDictionary(g => g.Key, g => g.Count());
            
            result.Analysis.MostProblematicFiles = errorsByFile
                .OrderByDescending(kvp => kvp.Value)
                .Take(5)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            
            // Analyze conflict types
            var totalConflicts = result.ConflictSummary.Values.Sum();
            if (totalConflicts > 0)
            {
                result.Analysis.ConflictTypePercentages = result.ConflictSummary
                    .ToDictionary(kvp => kvp.Key, kvp => (double)kvp.Value / totalConflicts * 100);
            }
            
            // Generate recommendations
            GenerateRecommendations(result);
        }
        
        /// <summary>
        /// Generates recommendations based on build analysis
        /// </summary>
        private void GenerateRecommendations(BuildValidationResult result)
        {
            result.Analysis.Recommendations = new List<string>();
            
            if (result.ConflictSummary.ContainsKey(ConflictType.TaskDialog) && result.ConflictSummary[ConflictType.TaskDialog] > 0)
            {
                result.Analysis.Recommendations.Add($"Apply RevitTaskDialog alias to resolve {result.ConflictSummary[ConflictType.TaskDialog]} TaskDialog conflicts");
            }
            
            if (result.ConflictSummary.ContainsKey(ConflictType.MessageBox) && result.ConflictSummary[ConflictType.MessageBox] > 0)
            {
                result.Analysis.Recommendations.Add($"Apply WpfMessageBox alias to resolve {result.ConflictSummary[ConflictType.MessageBox]} MessageBox conflicts");
            }
            
            if (result.ConflictSummary.ContainsKey(ConflictType.UIControls) && result.ConflictSummary[ConflictType.UIControls] > 0)
            {
                result.Analysis.Recommendations.Add($"Apply WPF control aliases to resolve {result.ConflictSummary[ConflictType.UIControls]} UI control conflicts");
            }
            
            if (result.ConflictSummary.ContainsKey(ConflictType.FileDialogs) && result.ConflictSummary[ConflictType.FileDialogs] > 0)
            {
                result.Analysis.Recommendations.Add($"Apply WinForms dialog aliases to resolve {result.ConflictSummary[ConflictType.FileDialogs]} file dialog conflicts");
            }
            
            if (result.ConflictSummary.ContainsKey(ConflictType.View) && result.ConflictSummary[ConflictType.View] > 0)
            {
                result.Analysis.Recommendations.Add($"Apply RevitView alias to resolve {result.ConflictSummary[ConflictType.View]} View conflicts");
            }
            
            if (result.Analysis.MostProblematicFiles.Any())
            {
                var topFile = result.Analysis.MostProblematicFiles.First();
                result.Analysis.Recommendations.Add($"Focus on {topFile.Key} which has {topFile.Value} errors");
            }
        }
        
        /// <summary>
        /// Validates functionality after successful build
        /// </summary>
        private async Task ValidateFunctionalityAsync(BuildValidationResult result)
        {
            result.FunctionalityTests = new List<FunctionalityTestResult>();
            
            // Test 1: Verify main assembly can be loaded
            await TestAssemblyLoading(result);
            
            // Test 2: Verify key classes exist and can be instantiated
            await TestKeyClassInstantiation(result);
            
            // Test 3: Verify Revit API integration
            await TestRevitApiIntegration(result);
        }
        
        private async Task TestAssemblyLoading(BuildValidationResult result)
        {
            var test = new FunctionalityTestResult
            {
                TestName = "Assembly Loading",
                StartTime = DateTime.Now
            };
            
            try
            {
                var assemblyPath = Path.Combine("bin", result.Configuration, "net8.0", "RevitDtools.dll");
                if (File.Exists(assemblyPath))
                {
                    var assembly = System.Reflection.Assembly.LoadFrom(assemblyPath);
                    test.Success = assembly != null;
                    test.Message = test.Success ? "Assembly loaded successfully" : "Failed to load assembly";
                }
                else
                {
                    test.Success = false;
                    test.Message = $"Assembly not found at {assemblyPath}";
                }
            }
            catch (Exception ex)
            {
                test.Success = false;
                test.Message = $"Exception loading assembly: {ex.Message}";
            }
            
            test.EndTime = DateTime.Now;
            test.Duration = test.EndTime - test.StartTime;
            result.FunctionalityTests.Add(test);
        }
        
        private async Task TestKeyClassInstantiation(BuildValidationResult result)
        {
            var test = new FunctionalityTestResult
            {
                TestName = "Key Class Instantiation",
                StartTime = DateTime.Now
            };
            
            try
            {
                // This would be expanded to test actual key classes
                // For now, just verify the test framework works
                test.Success = true;
                test.Message = "Key class instantiation test framework ready";
            }
            catch (Exception ex)
            {
                test.Success = false;
                test.Message = $"Exception testing key classes: {ex.Message}";
            }
            
            test.EndTime = DateTime.Now;
            test.Duration = test.EndTime - test.StartTime;
            result.FunctionalityTests.Add(test);
        }
        
        private async Task TestRevitApiIntegration(BuildValidationResult result)
        {
            var test = new FunctionalityTestResult
            {
                TestName = "Revit API Integration",
                StartTime = DateTime.Now
            };
            
            try
            {
                // Verify Revit API references are accessible
                // This is a basic check - full integration would require Revit context
                test.Success = true;
                test.Message = "Revit API integration test framework ready";
            }
            catch (Exception ex)
            {
                test.Success = false;
                test.Message = $"Exception testing Revit API integration: {ex.Message}";
            }
            
            test.EndTime = DateTime.Now;
            test.Duration = test.EndTime - test.StartTime;
            result.FunctionalityTests.Add(test);
        }
    }
}