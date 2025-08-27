using System;
using System.IO;
using System.Reflection;

namespace RevitDtools
{
    /// <summary>
    /// Build validation utility to verify project compilation and deployment
    /// </summary>
    public static class BuildValidator
    {
        /// <summary>
        /// Validates the build system and deployment
        /// </summary>
        /// <returns>BuildValidationResult with validation status</returns>
        public static BuildValidationResult ValidateProject()
        {
            var result = new BuildValidationResult();
            
            try
            {
                // 1. Validate main assembly exists and can be loaded
                ValidateMainAssembly(result);
                
                // 2. Validate Revit API references
                ValidateRevitApiReferences(result);
                
                // 3. Validate deployment files
                ValidateDeployment(result);
                
                // 4. Validate test assembly
                ValidateTestAssembly(result);
                
                result.OverallSuccess = result.MainAssemblyValid && 
                                       result.RevitApiReferencesValid && 
                                       result.DeploymentValid && 
                                       result.TestAssemblyValid;
                
                result.ValidationMessage = result.OverallSuccess 
                    ? "✓ All build validation checks passed successfully!"
                    : "⚠ Some build validation checks failed. See details above.";
            }
            catch (Exception ex)
            {
                result.OverallSuccess = false;
                result.ValidationMessage = $"❌ Build validation failed with exception: {ex.Message}";
                result.Errors.Add($"Exception during validation: {ex}");
            }
            
            return result;
        }
        
        private static void ValidateMainAssembly(BuildValidationResult result)
        {
            try
            {
                string assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RevitDtools.dll");
                
                if (!File.Exists(assemblyPath))
                {
                    // Try alternative paths
                    assemblyPath = Path.Combine(Directory.GetCurrentDirectory(), "bin", "Debug", "RevitDtools.dll");
                    if (!File.Exists(assemblyPath))
                    {
                        assemblyPath = Path.Combine(Directory.GetCurrentDirectory(), "bin", "Release", "RevitDtools.dll");
                    }
                }
                
                if (File.Exists(assemblyPath))
                {
                    Assembly assembly = Assembly.LoadFrom(assemblyPath);
                    
                    // Verify key classes exist
                    Type appType = assembly.GetType("RevitDtools.App");
                    Type dwgCommandType = assembly.GetType("RevitDtools.DwgToDetailLineCommand");
                    Type columnCommandType = assembly.GetType("RevitDtools.ColumnByLineCommand");
                    
                    if (appType != null && dwgCommandType != null && columnCommandType != null)
                    {
                        result.MainAssemblyValid = true;
                        result.Messages.Add("✓ Main assembly loaded successfully with all required classes");
                    }
                    else
                    {
                        result.MainAssemblyValid = false;
                        result.Errors.Add("❌ Main assembly missing required classes");
                    }
                }
                else
                {
                    result.MainAssemblyValid = false;
                    result.Errors.Add($"❌ Main assembly not found at expected locations");
                }
            }
            catch (Exception ex)
            {
                result.MainAssemblyValid = false;
                result.Errors.Add($"❌ Error validating main assembly: {ex.Message}");
            }
        }
        
        private static void ValidateRevitApiReferences(BuildValidationResult result)
        {
            try
            {
                string revitApiPath = @"E:\Program Files\Autodesk\Revit 2026\RevitAPI.dll";
                string revitApiUiPath = @"E:\Program Files\Autodesk\Revit 2026\RevitAPIUI.dll";
                
                bool revitApiExists = File.Exists(revitApiPath);
                bool revitApiUiExists = File.Exists(revitApiUiPath);
                
                if (revitApiExists && revitApiUiExists)
                {
                    result.RevitApiReferencesValid = true;
                    result.Messages.Add("✓ Revit API references are valid and accessible");
                }
                else
                {
                    result.RevitApiReferencesValid = false;
                    if (!revitApiExists) result.Errors.Add($"❌ RevitAPI.dll not found at {revitApiPath}");
                    if (!revitApiUiExists) result.Errors.Add($"❌ RevitAPIUI.dll not found at {revitApiUiPath}");
                }
            }
            catch (Exception ex)
            {
                result.RevitApiReferencesValid = false;
                result.Errors.Add($"❌ Error validating Revit API references: {ex.Message}");
            }
        }
        
        private static void ValidateDeployment(BuildValidationResult result)
        {
            try
            {
                string addinDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Autodesk", "Revit", "Addins", "2026");
                string addinFile = Path.Combine(addinDir, "RevitDtools.addin");
                string dllFile = Path.Combine(addinDir, "RevitDtools.dll");
                
                bool addinExists = File.Exists(addinFile);
                bool dllExists = File.Exists(dllFile);
                
                if (addinExists && dllExists)
                {
                    // Validate .addin file content
                    string addinContent = File.ReadAllText(addinFile);
                    bool hasValidContent = addinContent.Contains("RevitDtools.App") && 
                                         addinContent.Contains("A1E297A6-13A1-4235-B823-3C22B01D237A");
                    
                    if (hasValidContent)
                    {
                        result.DeploymentValid = true;
                        result.Messages.Add("✓ Deployment files are correctly installed in Revit add-ins directory");
                    }
                    else
                    {
                        result.DeploymentValid = false;
                        result.Errors.Add("❌ .addin file exists but has invalid content");
                    }
                }
                else
                {
                    result.DeploymentValid = false;
                    if (!addinExists) result.Errors.Add($"❌ .addin file not found at {addinFile}");
                    if (!dllExists) result.Errors.Add($"❌ DLL not found at {dllFile}");
                }
            }
            catch (Exception ex)
            {
                result.DeploymentValid = false;
                result.Errors.Add($"❌ Error validating deployment: {ex.Message}");
            }
        }
        
        private static void ValidateTestAssembly(BuildValidationResult result)
        {
            try
            {
                string testAssemblyPath = Path.Combine(Directory.GetCurrentDirectory(), 
                    "RevitDtools.Tests", "bin", "Debug", "RevitDtools.Tests.exe");
                
                if (File.Exists(testAssemblyPath))
                {
                    result.TestAssemblyValid = true;
                    result.Messages.Add("✓ Test assembly built successfully");
                }
                else
                {
                    result.TestAssemblyValid = false;
                    result.Errors.Add($"❌ Test assembly not found at {testAssemblyPath}");
                }
            }
            catch (Exception ex)
            {
                result.TestAssemblyValid = false;
                result.Errors.Add($"❌ Error validating test assembly: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Result of build validation
    /// </summary>
    public class BuildValidationResult
    {
        public bool OverallSuccess { get; set; }
        public bool MainAssemblyValid { get; set; }
        public bool RevitApiReferencesValid { get; set; }
        public bool DeploymentValid { get; set; }
        public bool TestAssemblyValid { get; set; }
        public string ValidationMessage { get; set; }
        public System.Collections.Generic.List<string> Messages { get; set; } = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<string> Errors { get; set; } = new System.Collections.Generic.List<string>();
        
        public void PrintResults()
        {
            Console.WriteLine("=== BUILD VALIDATION RESULTS ===");
            Console.WriteLine();
            
            foreach (var message in Messages)
            {
                Console.WriteLine(message);
            }
            
            foreach (var error in Errors)
            {
                Console.WriteLine(error);
            }
            
            Console.WriteLine();
            Console.WriteLine(ValidationMessage);
            Console.WriteLine();
            
            Console.WriteLine($"Main Assembly: {(MainAssemblyValid ? "✓ PASS" : "❌ FAIL")}");
            Console.WriteLine($"Revit API References: {(RevitApiReferencesValid ? "✓ PASS" : "❌ FAIL")}");
            Console.WriteLine($"Deployment: {(DeploymentValid ? "✓ PASS" : "❌ FAIL")}");
            Console.WriteLine($"Test Assembly: {(TestAssemblyValid ? "✓ PASS" : "❌ FAIL")}");
            Console.WriteLine();
            Console.WriteLine($"OVERALL: {(OverallSuccess ? "✓ SUCCESS" : "❌ FAILED")}");
        }
    }
}