using System;
using System.IO;
using System.Linq;

namespace RevitDtools
{
    /// <summary>
    /// Simplified build validator for testing without Revit API dependencies
    /// </summary>
    public static class TestBuildValidator
    {
        /// <summary>
        /// Simulates build validation without loading Revit assemblies
        /// </summary>
        public static TestBuildValidationResult ValidateProject()
        {
            var result = new TestBuildValidationResult();
            
            try
            {
                // 1. Check project structure
                ValidateProjectStructure(result);
                
                // 2. Check source files
                ValidateSourceFiles(result);
                
                // 3. Check deployment files
                ValidateDeploymentFiles(result);
                
                result.OverallSuccess = result.ProjectStructureValid && 
                                       result.SourceFilesValid && 
                                       result.DeploymentFilesValid;
                
                result.ValidationMessage = result.OverallSuccess 
                    ? "✓ All validation checks passed (simulation mode)!"
                    : "⚠ Some validation checks failed (simulation mode).";
            }
            catch (Exception ex)
            {
                result.OverallSuccess = false;
                result.ValidationMessage = $"❌ Validation failed: {ex.Message}";
                result.Errors.Add($"Exception: {ex.Message}");
            }
            
            return result;
        }
        
        private static void ValidateProjectStructure(TestBuildValidationResult result)
        {
            try
            {
                var projectFile = "RevitDtools.csproj";
                var solutionFile = "RevitDtools.sln";
                var addinFile = "RevitDtools.addin";
                
                bool hasProject = File.Exists(projectFile);
                bool hasSolution = File.Exists(solutionFile);
                bool hasAddin = File.Exists(addinFile);
                
                if (hasProject && hasSolution && hasAddin)
                {
                    result.ProjectStructureValid = true;
                    result.Messages.Add("✓ Project structure is valid");
                }
                else
                {
                    result.ProjectStructureValid = false;
                    if (!hasProject) result.Errors.Add("❌ RevitDtools.csproj not found");
                    if (!hasSolution) result.Errors.Add("❌ RevitDtools.sln not found");
                    if (!hasAddin) result.Errors.Add("❌ RevitDtools.addin not found");
                }
            }
            catch (Exception ex)
            {
                result.ProjectStructureValid = false;
                result.Errors.Add($"❌ Error validating project structure: {ex.Message}");
            }
        }
        
        private static void ValidateSourceFiles(TestBuildValidationResult result)
        {
            try
            {
                var sourceFiles = Directory.GetFiles(".", "*.cs", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))
                    .ToList();
                
                var coreFiles = sourceFiles.Where(f => f.Contains("\\Core\\")).Count();
                var commandFiles = sourceFiles.Where(f => f.Contains("Commands") || f.Contains("DtoolsCommands")).Count();
                
                if (sourceFiles.Count > 0 && coreFiles > 0 && commandFiles > 0)
                {
                    result.SourceFilesValid = true;
                    result.Messages.Add($"✓ Source files valid ({sourceFiles.Count} total, {coreFiles} core, {commandFiles} commands)");
                }
                else
                {
                    result.SourceFilesValid = false;
                    result.Errors.Add($"❌ Insufficient source files (total: {sourceFiles.Count}, core: {coreFiles}, commands: {commandFiles})");
                }
            }
            catch (Exception ex)
            {
                result.SourceFilesValid = false;
                result.Errors.Add($"❌ Error validating source files: {ex.Message}");
            }
        }
        
        private static void ValidateDeploymentFiles(TestBuildValidationResult result)
        {
            try
            {
                var deploymentDir = "Deployment";
                var hasDeploymentDir = Directory.Exists(deploymentDir);
                
                if (hasDeploymentDir)
                {
                    var deploymentFiles = Directory.GetFiles(deploymentDir, "*.*").Length;
                    result.DeploymentFilesValid = deploymentFiles > 0;
                    result.Messages.Add($"✓ Deployment directory exists with {deploymentFiles} files");
                }
                else
                {
                    result.DeploymentFilesValid = false;
                    result.Errors.Add("❌ Deployment directory not found");
                }
            }
            catch (Exception ex)
            {
                result.DeploymentFilesValid = false;
                result.Errors.Add($"❌ Error validating deployment files: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Result of test build validation
    /// </summary>
    public class TestBuildValidationResult
    {
        public bool OverallSuccess { get; set; }
        public bool ProjectStructureValid { get; set; }
        public bool SourceFilesValid { get; set; }
        public bool DeploymentFilesValid { get; set; }
        public string ValidationMessage { get; set; } = "";
        public System.Collections.Generic.List<string> Messages { get; set; } = new();
        public System.Collections.Generic.List<string> Errors { get; set; } = new();
    }
}