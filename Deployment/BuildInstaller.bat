@echo off
echo === Building RevitDtools Enhanced MSI Installer ===

REM Set paths
set WIX_PATH="C:\Program Files (x86)\WiX Toolset v3.11\bin"
set PROJECT_DIR=%~dp0..
set OUTPUT_DIR=%PROJECT_DIR%\bin\Installer
set SOURCE_DIR=%PROJECT_DIR%\bin\Release

REM Create output directory
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

echo Building project in Release mode...
msbuild "%PROJECT_DIR%\RevitDtools.sln" /p:Configuration=Release /p:Platform="Any CPU" /verbosity:minimal

if %ERRORLEVEL% neq 0 (
    echo ERROR: Build failed
    pause
    exit /b 1
)

echo Copying files for installer...
copy "%SOURCE_DIR%\RevitDtools.dll" "%~dp0"
copy "%SOURCE_DIR%\Newtonsoft.Json.dll" "%~dp0"

REM Create version-specific .addin files
echo Creating Revit 2024 .addin file...
(
    echo ^<?xml version="1.0" encoding="utf-8"?^>
    echo ^<RevitAddIns^>
    echo   ^<AddIn Type="Application"^>
    echo     ^<Name^>RevitDtools Enhanced^</Name^>
    echo     ^<Assembly^>RevitDtools.dll^</Assembly^>
    echo     ^<AddInId^>A1E297A6-13A1-4235-B823-3C22B01D237A^</AddInId^>
    echo     ^<FullClassName^>RevitDtools.App^</FullClassName^>
    echo     ^<VendorId^>RevitDtools^</VendorId^>
    echo     ^<VendorDescription^>Professional DWG Processing Tools^</VendorDescription^>
    echo     ^<VisibilityMode^>NotVisibleWhenNoActiveDocument^</VisibilityMode^>
    echo   ^</AddIn^>
    echo ^</RevitAddIns^>
) > "%~dp0RevitDtools2024.addin"

echo Creating Revit 2025 .addin file...
(
    echo ^<?xml version="1.0" encoding="utf-8"?^>
    echo ^<RevitAddIns^>
    echo   ^<AddIn Type="Application"^>
    echo     ^<Name^>RevitDtools Enhanced^</Name^>
    echo     ^<Assembly^>RevitDtools.dll^</Assembly^>
    echo     ^<AddInId^>A1E297A6-13A1-4235-B823-3C22B01D237A^</AddInId^>
    echo     ^<FullClassName^>RevitDtools.App^</FullClassName^>
    echo     ^<VendorId^>RevitDtools^</VendorId^>
    echo     ^<VendorDescription^>Professional DWG Processing Tools^</VendorDescription^>
    echo     ^<VisibilityMode^>NotVisibleWhenNoActiveDocument^</VisibilityMode^>
    echo   ^</AddIn^>
    echo ^</RevitAddIns^>
) > "%~dp0RevitDtools2025.addin"

echo Creating Revit 2026 .addin file...
(
    echo ^<?xml version="1.0" encoding="utf-8"?^>
    echo ^<RevitAddIns^>
    echo   ^<AddIn Type="Application"^>
    echo     ^<Name^>RevitDtools Enhanced^</Name^>
    echo     ^<Assembly^>RevitDtools.dll^</Assembly^>
    echo     ^<AddInId^>A1E297A6-13A1-4235-B823-3C22B01D237A^</AddInId^>
    echo     ^<FullClassName^>RevitDtools.App^</FullClassName^>
    echo     ^<VendorId^>RevitDtools^</VendorId^>
    echo     ^<VendorDescription^>Professional DWG Processing Tools^</VendorDescription^>
    echo     ^<VisibilityMode^>NotVisibleWhenNoActiveDocument^</VisibilityMode^>
    echo   ^</AddIn^>
    echo ^</RevitAddIns^>
) > "%~dp0RevitDtools2026.addin"

echo Creating configuration file...
(
    echo ^<?xml version="1.0" encoding="utf-8"?^>
    echo ^<configuration^>
    echo   ^<appSettings^>
    echo     ^<add key="LogLevel" value="Info" /^>
    echo     ^<add key="EnablePerformanceMonitoring" value="true" /^>
    echo     ^<add key="AutoUpdateCheck" value="true" /^>
    echo     ^<add key="UpdateCheckInterval" value="7" /^>
    echo   ^</appSettings^>
    echo ^</configuration^>
) > "%~dp0RevitDtools.config"

echo Compiling WiX source...
%WIX_PATH%\candle.exe -ext WixUIExtension RevitDtools.wxs -o "%OUTPUT_DIR%\RevitDtools.wixobj"

if %ERRORLEVEL% neq 0 (
    echo ERROR: WiX compilation failed
    pause
    exit /b 1
)

echo Linking MSI package...
%WIX_PATH%\light.exe -ext WixUIExtension "%OUTPUT_DIR%\RevitDtools.wixobj" -o "%OUTPUT_DIR%\RevitDtools_Enhanced_v2.0.0.msi"

if %ERRORLEVEL% neq 0 (
    echo ERROR: MSI linking failed
    pause
    exit /b 1
)

echo Cleaning up temporary files...
del "%~dp0RevitDtools.dll" 2>nul
del "%~dp0Newtonsoft.Json.dll" 2>nul
del "%~dp0RevitDtools2024.addin" 2>nul
del "%~dp0RevitDtools2025.addin" 2>nul
del "%~dp0RevitDtools2026.addin" 2>nul
del "%~dp0RevitDtools.config" 2>nul

echo === MSI Installer built successfully ===
echo Output: %OUTPUT_DIR%\RevitDtools_Enhanced_v2.0.0.msi
echo.
echo To install: Run the MSI file as administrator
echo To uninstall: Use Windows Add/Remove Programs or run: msiexec /x {ProductCode}
echo.
pause