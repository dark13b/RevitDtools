# RevitDtools - Revit Development Tools

> **Inspired by DTools** - A comprehensive Revit add-in for enhanced productivity in architectural and structural design workflows.

[![Revit 2026](https://img.shields.io/badge/Revit-2026-blue.svg)](https://www.autodesk.com/products/revit)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## 🚀 Features

### 📐 Geometry Processing
- **DWG to Detail Lines**: Convert imported DWG layers to Revit detail lines with comprehensive geometry support
- **Enhanced DWG Processing**: Advanced processing with support for arcs, splines, ellipses, text, and hatch patterns
- **Batch Processing**: Process multiple DWG files simultaneously with progress tracking

### 🏗️ Column Creation Tools
- **Column by Line**: Create structural columns from rectangular detail lines
- **Enhanced Column Creation**: Dynamic family management with automatic dimension calculation
- **Batch Column Creation**: Automatically detect rectangles and create columns in bulk
- **Advanced Column Features**: Support for circular columns, custom shapes, and column grids

### 🔧 Diagnostic & Management Tools
- **Family Diagnostics**: Analyze available column families and troubleshoot creation issues
- **Standard Family Loader**: Automatically load common Revit column families
- **Settings Management**: Comprehensive configuration for all tools and preferences

## 📦 Installation

### Prerequisites
- Autodesk Revit 2026
- .NET 8.0 Runtime
- Windows 10/11

### Quick Install
1. Download the latest release from [Releases](../../releases)
2. Extract the files to a local directory
3. Copy `RevitDtools.addin` to your Revit add-ins folder:
   ```
   %APPDATA%\Autodesk\Revit\Addins\2026\
   ```
4. Restart Revit
5. Look for the "Dtools" tab in the ribbon

### Manual Build
```bash
git clone https://github.com/yourusername/RevitDtools.git
cd RevitDtools
dotnet build --configuration Release
```

## 🎯 Quick Start

### Basic Column Creation
1. Import or draw detail lines forming rectangles
2. Select the lines or use the batch detection
3. Click **Batch Columns** in the Dtools ribbon
4. Review the detected rectangles and confirm creation

### DWG Processing
1. Import a DWG file into your Revit project
2. Click **DWG to Detail Line** in the Dtools ribbon
3. Select the DWG import and choose layers to convert
4. Click OK to create detail lines

### Troubleshooting Column Issues
1. Click **Diagnose Family Issues** to analyze available families
2. Use **Load Standard Families** if column families are missing
3. Check the diagnostic report for specific recommendations

## 🐛 Known Issues

### Transaction Context Errors
**Issue**: "Starting a transaction from an external application running outside of API context is not allowed"

**Status**: ✅ **FIXED** in v1.1.0

**Solution**: Implemented proper transaction management with separated symbol activation and column creation phases.

### Batch Processing Failures
**Issue**: Batch column processing only creates some columns (e.g., 6 out of 66)

**Status**: ✅ **FIXED** in v1.1.0

**Root Cause**: Missing column families or parameter mapping issues

**Solution**: 
- Enhanced fallback system for family symbol matching
- Added diagnostic tools to identify missing families
- Improved error reporting and recovery

### Logger Method Errors
**Issue**: Compilation errors with Logger static method calls

**Status**: ✅ **FIXED** in v1.1.0

**Solution**: Updated all Logger calls to use proper static method signatures with context parameters.

## 🏗️ Architecture

### Core Components
- **Commands**: External command implementations for Revit integration
- **Services**: Business logic and data processing services
- **Models**: Data models and parameter definitions
- **Utilities**: Logging, validation, and helper utilities
- **UI**: Windows and user interface components

### Key Services
- `FamilyManagementService`: Handles column family creation and management
- `GeometryProcessingService`: Processes DWG geometry and conversions
- `BatchProcessingService`: Manages bulk operations with progress tracking
- `ColumnScheduleService`: Integrates with Revit schedules and data

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details.

### Development Setup
1. Clone the repository
2. Open in Visual Studio 2022 or VS Code
3. Ensure Revit 2026 SDK is installed
4. Build and test with a Revit instance

### Code Style
- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Include unit tests for new features

## 📋 Changelog

### v1.1.0 (Current)
- ✅ Fixed transaction context errors in batch processing
- ✅ Enhanced fallback system for family symbol matching
- ✅ Added diagnostic tools for troubleshooting
- ✅ Improved error handling and logging
- ✅ Added standard family loading capabilities

### v1.0.0
- Initial release with basic DWG processing
- Column creation from detail lines
- Basic batch processing functionality

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Inspired by the original DTools concept
- Built with the Revit API and .NET ecosystem
- Thanks to the Revit development community for guidance and best practices

## 📞 Support

- 📖 [Documentation](../../wiki)
- 🐛 [Report Issues](../../issues)
- 💬 [Discussions](../../discussions)
- 📧 Contact: [your-email@example.com]

---

**Made with ❤️ for the Revit community**