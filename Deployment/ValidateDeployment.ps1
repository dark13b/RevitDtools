# RevitDtools Enhanced - Deployment Validation Script
# This script validates that the deployment is complete and functional

param(
    [string]$RevitVersion = "2026",
    [switch]$Verbose = $false
)

Write-Host "=== RevitDtools Enhanced Deployment Validation ===" -ForegroundColor Green
Write-Host "Validating deployment for Revit $RevitVersion" -ForegroundColor Yellow

$ErrorCount = 0
$WarningCount = 0

function Write-ValidationResult {
    param(
        [string]$Test,
        [bool]$Passed,
        [string]$Message = "",
        [bool]$IsWarning = $false
    )
    
    if ($Passed) {
        Write-Host "✓ $Test" -ForegroundColor Green
        if ($Verbose -and $Message) {
            Write-Host "  $Message" -ForegroundColor Gray
        }
    } else {
        if ($IsWarning) {
            Write-Host "⚠ $Test" -ForegroundColor Yellow
            $script:WarningCount++
        } else {
            Write-Host "✗ $Test" -ForegroundColor Red
            $script:ErrorCount++
        }
        if ($Message) {
            Write-Host "  $Message" -ForegroundColor Gray
        }
    }
}

# Test 1: Check build output
Write-Host "`n--- Build Output Validation ---" -ForegroundColor Cyan

$BinPath = Join-Path $PSScriptRoot "..\bin\Release"
$DllPath = Join-Path $BinPath "RevitDtools.dll"
$JsonPath = Join-Path $BinPath "Newtonsoft.Json.dll"

Write-ValidationResult "RevitDtools.dll exists" (Test-Path $DllPath) "Path: $DllPath"
Write-ValidationResult "Newtonsoft.Json.dll exists" (Test-Path $JsonPath) "Path: $JsonPath"

if (Test-Path $DllPath) {
    try {
        $Assembly = [System.Reflection.Assembly]::LoadFrom($DllPath)
        $Version = $Assembly.GetName().Version
        Write-ValidationResult "Assembly loads successfully" $true "Version: $Version"
        
        # Check for main classes
        $AppClass = $Assembly.GetType("RevitDtools.App")
        Write-ValidationResult "App class exists" ($AppClass -ne $null)
        
        $CommandClasses = @(
            "RevitDtools.Core.Commands.EnhancedDwgToDetailLineCommand",
            "RevitDtools.Core.Commands.EnhancedColumnByLineCommand",
            "RevitDtools.Core.Commands.BatchProcessCommand",
            "RevitDtools.Core.Commands.SettingsCommand"
        )
        
        foreach ($ClassName in $CommandClasses) {
            $Class = $Assembly.GetType($ClassName)
            Write-ValidationResult "$($ClassName.Split('.')[-1]) exists" ($Class -ne $null)
        }
    }
    catch {
        Write-ValidationResult "Assembly loads successfully" $false $_.Exception.Message
    }
}

# Test 2: Check Revit add-in deployment
Write-Host "`n--- Revit Add-in Deployment ---" -ForegroundColor Cyan

$AddinDir = Join-Path $env:APPDATA "Autodesk\Revit\Addins\$RevitVersion"
$AddinFile = Join-Path $AddinDir "RevitDtools.addin"
$DeployedDll = Join-Path $AddinDir "RevitDtools.dll"

Write-ValidationResult "Add-in directory exists" (Test-Path $AddinDir) "Path: $AddinDir"
Write-ValidationResult "Add-in file exists" (Test-Path $AddinFile) "Path: $AddinFile"
Write-ValidationResult "DLL deployed to add-in directory" (Test-Path $DeployedDll) "Path: $DeployedDll"

if (Test-Path $AddinFile) {
    try {
        [xml]$AddinXml = Get-Content $AddinFile
        $AddIn = $AddinXml.RevitAddIns.AddIn
        
        Write-ValidationResult "Add-in XML is valid" ($AddIn -ne $null)
        Write-ValidationResult "Add-in name is set" (-not [string]::IsNullOrEmpty($AddIn.Name))
        Write-ValidationResult "Assembly path is set" (-not [string]::IsNullOrEmpty($AddIn.Assembly))
        Write-ValidationResult "AddInId is set" (-not [string]::IsNullOrEmpty($AddIn.AddInId))
        Write-ValidationResult "FullClassName is set" (-not [string]::IsNullOrEmpty($AddIn.FullClassName))
        
        if ($Verbose) {
            Write-Host "  Add-in Details:" -ForegroundColor Gray
            Write-Host "    Name: $($AddIn.Name)" -ForegroundColor Gray
            Write-Host "    Assembly: $($AddIn.Assembly)" -ForegroundColor Gray
            Write-Host "    Class: $($AddIn.FullClassName)" -ForegroundColor Gray
            Write-Host "    ID: $($AddIn.AddInId)" -ForegroundColor Gray
        }
    }
    catch {
        Write-ValidationResult "Add-in XML is valid" $false $_.Exception.Message
    }
}

# Test 3: Check documentation
Write-Host "`n--- Documentation Validation ---" -ForegroundColor Cyan

$DocPath = Join-Path $PSScriptRoot "..\Documentation"
$UserManual = Join-Path $DocPath "UserManual.md"
$QuickStart = Join-Path $DocPath "QuickStartGuide.md"

Write-ValidationResult "Documentation directory exists" (Test-Path $DocPath) "Path: $DocPath"
Write-ValidationResult "User Manual exists" (Test-Path $UserManual) "Path: $UserManual"
Write-ValidationResult "Quick Start Guide exists" (Test-Path $QuickStart) "Path: $QuickStart"

if (Test-Path $UserManual) {
    $ManualContent = Get-Content $UserManual -Raw
    $ManualSize = $ManualContent.Length
    Write-ValidationResult "User Manual has content" ($ManualSize -gt 10000) "Size: $ManualSize characters"
}

# Test 4: Check installer components
Write-Host "`n--- Installer Components ---" -ForegroundColor Cyan

$WixFile = Join-Path $PSScriptRoot "RevitDtools.wxs"
$BuildScript = Join-Path $PSScriptRoot "BuildInstaller.bat"

Write-ValidationResult "WiX installer definition exists" (Test-Path $WixFile) "Path: $WixFile"
Write-ValidationResult "Build installer script exists" (Test-Path $BuildScript) "Path: $BuildScript"

# Test 5: Check test coverage
Write-Host "`n--- Test Coverage Validation ---" -ForegroundColor Cyan

$TestPath = Join-Path $PSScriptRoot "..\RevitDtools.Tests"
$TestFiles = @(
    "EndToEndTests.cs",
    "RequirementsValidationTests.cs",
    "GeometryProcessingTests.cs",
    "FamilyManagementTests.cs",
    "BatchProcessingTests.cs",
    "SettingsServiceTests.cs",
    "IntegrationTests.cs",
    "WorkflowTests.cs",
    "UITests.cs"
)

foreach ($TestFile in $TestFiles) {
    $TestFilePath = Join-Path $TestPath $TestFile
    Write-ValidationResult "$TestFile exists" (Test-Path $TestFilePath)
}

# Test 6: Performance and logging components
Write-Host "`n--- Performance and Logging ---" -ForegroundColor Cyan

$UtilitiesPath = Join-Path $PSScriptRoot "..\Utilities"
$PerformanceMonitor = Join-Path $UtilitiesPath "PerformanceMonitor.cs"
$Logger = Join-Path $UtilitiesPath "Logger.cs"
$ErrorHandler = Join-Path $UtilitiesPath "ErrorHandler.cs"

Write-ValidationResult "PerformanceMonitor exists" (Test-Path $PerformanceMonitor)
Write-ValidationResult "Logger exists" (Test-Path $Logger)
Write-ValidationResult "ErrorHandler exists" (Test-Path $ErrorHandler)

# Test 7: Update service
Write-Host "`n--- Update Service ---" -ForegroundColor Cyan

$UpdateService = Join-Path $PSScriptRoot "..\Core\Services\UpdateService.cs"
Write-ValidationResult "UpdateService exists" (Test-Path $UpdateService)

# Test 8: Settings and configuration
Write-Host "`n--- Settings and Configuration ---" -ForegroundColor Cyan

$SettingsService = Join-Path $PSScriptRoot "..\Core\Services\SettingsService.cs"
$SettingsWindow = Join-Path $PSScriptRoot "..\UI\Windows\SettingsWindow.xaml"

Write-ValidationResult "SettingsService exists" (Test-Path $SettingsService)
Write-ValidationResult "SettingsWindow exists" (Test-Path $SettingsWindow)

# Test 9: Batch processing
Write-Host "`n--- Batch Processing ---" -ForegroundColor Cyan

$BatchService = Join-Path $PSScriptRoot "..\Core\Services\BatchProcessingService.cs"
$BatchWindow = Join-Path $PSScriptRoot "..\UI\Windows\BatchProcessingWindow.xaml"

Write-ValidationResult "BatchProcessingService exists" (Test-Path $BatchService)
Write-ValidationResult "BatchProcessingWindow exists" (Test-Path $BatchWindow)

# Test 10: Advanced column features
Write-Host "`n--- Advanced Column Features ---" -ForegroundColor Cyan

$AdvancedColumnCommands = @(
    "CircularColumnCommand.cs",
    "CustomShapeColumnCommand.cs",
    "ColumnGridCommand.cs",
    "AdvancedColumnCommand.cs"
)

$CommandsPath = Join-Path $PSScriptRoot "..\Core\Commands"
foreach ($Command in $AdvancedColumnCommands) {
    $CommandPath = Join-Path $CommandsPath $Command
    Write-ValidationResult "$Command exists" (Test-Path $CommandPath)
}

# Summary
Write-Host "`n=== Validation Summary ===" -ForegroundColor Green

if ($ErrorCount -eq 0 -and $WarningCount -eq 0) {
    Write-Host "✓ All validation tests passed successfully!" -ForegroundColor Green
    Write-Host "RevitDtools Enhanced is ready for deployment." -ForegroundColor Green
    exit 0
} elseif ($ErrorCount -eq 0) {
    Write-Host "✓ All critical tests passed with $WarningCount warnings." -ForegroundColor Yellow
    Write-Host "RevitDtools Enhanced is ready for deployment with minor issues." -ForegroundColor Yellow
    exit 0
} else {
    Write-Host "✗ Validation failed with $ErrorCount errors and $WarningCount warnings." -ForegroundColor Red
    Write-Host "Please fix the errors before deploying RevitDtools Enhanced." -ForegroundColor Red
    exit 1
}

# Additional deployment instructions
Write-Host "`n=== Deployment Instructions ===" -ForegroundColor Cyan
Write-Host "1. Build the solution in Release mode" -ForegroundColor White
Write-Host "2. Run BuildInstaller.bat to create the MSI installer" -ForegroundColor White
Write-Host "3. Test the installer on a clean system" -ForegroundColor White
Write-Host "4. Distribute the MSI file to end users" -ForegroundColor White
Write-Host "5. Provide documentation and support resources" -ForegroundColor White

Write-Host "`n=== Support Information ===" -ForegroundColor Cyan
Write-Host "Documentation: Documentation/UserManual.md" -ForegroundColor White
Write-Host "Quick Start: Documentation/QuickStartGuide.md" -ForegroundColor White
Write-Host "Log Files: %APPDATA%\RevitDtools\Logs\" -ForegroundColor White
Write-Host "Settings: %APPDATA%\RevitDtools\settings.json" -ForegroundColor White