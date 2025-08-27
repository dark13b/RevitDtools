# RevitDtools Enhanced - User Manual

## Table of Contents
1. [Introduction](#introduction)
2. [Installation](#installation)
3. [Getting Started](#getting-started)
4. [Features Overview](#features-overview)
5. [DWG Processing](#dwg-processing)
6. [Column Creation](#column-creation)
7. [Batch Processing](#batch-processing)
8. [Settings and Configuration](#settings-and-configuration)
9. [Advanced Features](#advanced-features)
10. [Troubleshooting](#troubleshooting)
11. [Support and Updates](#support-and-updates)

## Introduction

RevitDtools Enhanced is a professional add-in for Autodesk Revit that provides comprehensive tools for processing DWG imports and creating structural elements. The enhanced version includes advanced geometry processing, batch operations, and professional-grade features for enterprise workflows.

### Key Features
- **Comprehensive DWG Processing**: Convert all types of DWG geometry to native Revit elements
- **Advanced Column Creation**: Create structural columns with various shapes and configurations
- **Batch Processing**: Process multiple files simultaneously with progress tracking
- **Dynamic Family Management**: Automatic family creation and management
- **Professional Logging**: Comprehensive error tracking and performance monitoring
- **User Settings**: Persistent configuration and customization options

### System Requirements
- Autodesk Revit 2024, 2025, or 2026
- Windows 10 or later
- .NET Framework 4.8
- Minimum 4GB RAM (8GB recommended)
- 100MB available disk space

## Installation

### Automatic Installation (Recommended)
1. Download the MSI installer: `RevitDtools_Enhanced_v2.0.0.msi`
2. Right-click the installer and select "Run as administrator"
3. Follow the installation wizard:
   - Accept the license agreement
   - Select which Revit versions to install for
   - Choose installation location (default recommended)
   - Complete the installation

### Manual Installation
1. Extract the installation files to a folder
2. Copy `RevitDtools.dll` to your Revit add-ins folder:
   - Revit 2024: `%APPDATA%\Autodesk\Revit\Addins\2024\`
   - Revit 2025: `%APPDATA%\Autodesk\Revit\Addins\2025\`
   - Revit 2026: `%APPDATA%\Autodesk\Revit\Addins\2026\`
3. Create a `.addin` file in the same directory with the provided content

### Verification
1. Start Revit
2. Look for the "Dtools" ribbon tab
3. If the tab appears, installation was successful
4. If not, check the Revit journal file for error messages

## Getting Started

### First Launch
1. Open Revit and create a new project
2. Navigate to the "Dtools" ribbon tab
3. You'll see several command groups:
   - **DWG Processing**: Tools for converting DWG imports
   - **Column Creation**: Tools for creating structural columns
   - **Batch Operations**: Tools for processing multiple files
   - **Settings**: Configuration and preferences

### Quick Start Workflow
1. **Import a DWG file** into your Revit project using Revit's standard import tools
2. **Select the imported DWG** in the project
3. **Click "Enhanced DWG to Detail Lines"** to convert geometry to Revit elements
4. **Select rectangular detail lines** that represent columns
5. **Click "Enhanced Column by Line"** to create structural columns
6. **Review and adjust** the created elements as needed

## Features Overview

### DWG Processing Tools

#### Enhanced DWG to Detail Lines
Converts imported DWG geometry to native Revit detail lines with support for:
- **Lines and Polylines**: Straight line segments
- **Arcs**: Curved segments with proper radius
- **Splines**: Complex curved paths
- **Ellipses**: Elliptical and circular shapes
- **Text**: Text annotations (converted to text notes)
- **Hatches**: Filled regions with patterns
- **Nested Blocks**: Recursive processing of block references

**Usage:**
1. Select an imported DWG in your project
2. Click "Enhanced DWG to Detail Lines"
3. Configure layer mapping in the dialog
4. Click "Process" to convert geometry
5. Review the processing report

#### Layer Mapping
Configure how DWG layers are mapped to Revit:
- **Automatic Mapping**: Smart layer name matching
- **Custom Mapping**: Define specific layer relationships
- **Layer Templates**: Save and reuse mapping configurations
- **Default Layers**: Set fallback layers for unmapped content

### Column Creation Tools

#### Enhanced Column by Line
Creates structural columns from rectangular detail lines:
- **Automatic Family Selection**: Chooses appropriate column families
- **Dynamic Sizing**: Creates families for custom dimensions
- **Parameter Mapping**: Applies structural properties automatically
- **Validation**: Ensures proper column placement and sizing

**Usage:**
1. Select rectangular detail lines in your project
2. Click "Enhanced Column by Line"
3. Configure column parameters in the dialog
4. Choose family and sizing options
5. Click "Create Columns" to generate elements

#### Advanced Column Creation
Additional column creation tools:
- **Circular Columns**: Create round columns with diameter specification
- **Custom Shape Columns**: Create columns from profile curves
- **Column Grids**: Generate column patterns and layouts
- **Schedule Integration**: Apply column schedule data automatically

### Batch Processing

#### Batch File Processing
Process multiple DWG files simultaneously:
- **File Selection**: Choose individual files or entire folders
- **Progress Tracking**: Real-time progress with file names and status
- **Error Handling**: Continue processing if individual files fail
- **Summary Reports**: Comprehensive results with statistics
- **Cancellation Support**: Stop processing at any time

**Usage:**
1. Click "Batch Process" in the ribbon
2. Select files or folders to process
3. Configure processing options
4. Click "Start Processing"
5. Monitor progress and review results

#### Batch Settings
Configure batch processing behavior:
- **Processing Options**: Choose which operations to perform
- **Error Handling**: Set how to handle failures
- **Output Settings**: Configure result file locations
- **Performance**: Adjust processing speed vs. resource usage

## DWG Processing

### Supported Geometry Types

#### Lines and Polylines
- **Simple Lines**: Converted to Revit detail lines
- **Polylines**: Segmented into individual line elements
- **Line Weights**: Preserved where possible
- **Layer Assignment**: Mapped according to layer settings

#### Curved Elements
- **Arcs**: Converted with proper start/end points and radius
- **Circles**: Full circular elements
- **Ellipses**: Elliptical shapes with major/minor axes
- **Splines**: Complex curves with control points

#### Text Elements
- **Single Line Text**: Converted to Revit text notes
- **Multi-line Text**: Preserved with formatting where possible
- **Text Styles**: Mapped to Revit text types
- **Positioning**: Maintains original placement and rotation

#### Filled Regions
- **Hatches**: Converted to Revit filled regions
- **Pattern Mapping**: DWG patterns mapped to Revit patterns
- **Solid Fills**: Converted to solid filled regions
- **Boundaries**: Proper boundary definition and cleanup

### Processing Options

#### Layer Management
- **Layer Filtering**: Choose which layers to process
- **Layer Mapping**: Define how DWG layers map to Revit
- **New Layer Creation**: Automatically create missing Revit layers
- **Layer Properties**: Transfer layer properties where applicable

#### Geometry Cleanup
- **Duplicate Removal**: Eliminate overlapping geometry
- **Gap Closure**: Close small gaps in polylines
- **Curve Simplification**: Reduce complex curves to manageable segments
- **Coordinate Adjustment**: Align geometry to Revit coordinate system

#### Error Handling
- **Skip Invalid Geometry**: Continue processing when errors occur
- **Error Logging**: Detailed logs of processing issues
- **Recovery Options**: Attempt to fix common geometry problems
- **User Notification**: Clear feedback on processing results

## Column Creation

### Column Types

#### Rectangular Columns
- **Standard Sizes**: Common structural column dimensions
- **Custom Sizes**: Create families for specific dimensions
- **Material Assignment**: Apply structural materials automatically
- **Parameter Mapping**: Set structural properties and parameters

#### Circular Columns
- **Diameter Specification**: Define column diameter
- **Center Point Placement**: Precise positioning
- **Standard Families**: Use existing circular column families
- **Custom Families**: Create new families for unique sizes

#### Custom Shape Columns
- **Profile Definition**: Create columns from custom profile curves
- **Complex Shapes**: Support for non-standard column shapes
- **Family Generation**: Automatic family creation for custom profiles
- **Validation**: Ensure profiles are suitable for column creation

### Family Management

#### Automatic Family Creation
- **Size Detection**: Analyze detail lines to determine column sizes
- **Family Generation**: Create new families as needed
- **Parameter Setup**: Configure family parameters automatically
- **Loading**: Load families into the project automatically

#### Family Reuse
- **Existing Family Detection**: Find and reuse existing families
- **Size Matching**: Match dimensions to existing family symbols
- **Performance Optimization**: Reduce family creation overhead
- **Family Organization**: Maintain clean family structure

#### Validation and Quality Control
- **Geometry Validation**: Ensure column geometry is valid
- **Placement Checking**: Verify column placement is appropriate
- **Conflict Detection**: Identify potential conflicts with existing elements
- **Quality Reports**: Provide feedback on column creation quality

## Batch Processing

### File Selection

#### Individual Files
- **File Browser**: Standard Windows file selection dialog
- **Multiple Selection**: Choose multiple DWG files at once
- **File Filtering**: Show only supported file types
- **Recent Files**: Quick access to recently processed files

#### Folder Processing
- **Folder Selection**: Choose entire folders for processing
- **Recursive Processing**: Include subfolders in processing
- **File Type Filtering**: Process only specific file types
- **Exclusion Patterns**: Skip files matching certain patterns

### Processing Control

#### Progress Monitoring
- **Real-time Progress**: Live updates on processing status
- **File-by-file Status**: See which file is currently being processed
- **Time Estimates**: Estimated completion times
- **Performance Metrics**: Processing speed and efficiency data

#### Cancellation and Pause
- **Graceful Cancellation**: Stop processing without data loss
- **Pause/Resume**: Temporarily pause processing
- **Recovery**: Resume interrupted processing sessions
- **Cleanup**: Proper cleanup of temporary files and resources

### Results and Reporting

#### Processing Reports
- **Summary Statistics**: Overall processing results
- **File-by-file Results**: Detailed results for each processed file
- **Error Reports**: Comprehensive error information
- **Performance Data**: Processing times and efficiency metrics

#### Export Options
- **Report Formats**: Export reports in various formats (PDF, Excel, CSV)
- **Custom Reports**: Configure report content and format
- **Automated Reporting**: Schedule automatic report generation
- **Report Distribution**: Email or save reports automatically

## Settings and Configuration

### User Preferences

#### General Settings
- **Default Behaviors**: Set default processing options
- **UI Preferences**: Customize user interface appearance
- **Performance Settings**: Adjust processing speed vs. quality
- **Logging Levels**: Configure how much detail to log

#### Layer Mapping
- **Default Mappings**: Set up standard layer mapping rules
- **Mapping Templates**: Create and save mapping configurations
- **Auto-mapping Rules**: Define automatic mapping logic
- **Layer Creation**: Configure automatic layer creation behavior

#### Column Settings
- **Default Families**: Set preferred column families
- **Sizing Rules**: Define how column sizes are determined
- **Material Assignments**: Set default materials for columns
- **Parameter Defaults**: Configure default column parameters

### Advanced Configuration

#### Performance Tuning
- **Memory Management**: Configure memory usage limits
- **Processing Threads**: Adjust multi-threading settings
- **Cache Settings**: Configure geometry and family caching
- **Optimization Levels**: Balance speed vs. accuracy

#### Error Handling
- **Error Tolerance**: Set how many errors to allow before stopping
- **Recovery Options**: Configure automatic error recovery
- **Notification Settings**: Set up error notifications
- **Logging Detail**: Configure error logging verbosity

#### Update Settings
- **Automatic Updates**: Enable/disable automatic update checking
- **Update Frequency**: Set how often to check for updates
- **Update Notifications**: Configure update notification preferences
- **Beta Versions**: Opt in/out of beta version updates

## Advanced Features

### Performance Monitoring

#### Real-time Monitoring
- **Processing Speed**: Monitor elements processed per second
- **Memory Usage**: Track memory consumption during processing
- **CPU Utilization**: Monitor processor usage
- **Bottleneck Identification**: Identify performance bottlenecks

#### Performance Reports
- **Historical Data**: Track performance over time
- **Comparison Reports**: Compare performance across different operations
- **Optimization Recommendations**: Suggestions for improving performance
- **Benchmark Data**: Compare against standard benchmarks

### Scripting and Automation

#### Command Line Interface
- **Batch Scripts**: Create scripts for automated processing
- **Parameter Passing**: Pass configuration via command line
- **Return Codes**: Get processing results programmatically
- **Integration**: Integrate with other tools and workflows

#### API Integration
- **External Tools**: Integrate with other Revit add-ins
- **Custom Workflows**: Create custom processing workflows
- **Data Exchange**: Import/export processing data
- **Third-party Integration**: Connect with external systems

### Quality Assurance

#### Validation Tools
- **Geometry Validation**: Verify processed geometry quality
- **Element Checking**: Ensure created elements meet standards
- **Consistency Checks**: Verify consistency across processed files
- **Quality Reports**: Generate quality assurance reports

#### Testing and Verification
- **Test Suites**: Run comprehensive tests on processing results
- **Regression Testing**: Ensure updates don't break existing functionality
- **Performance Testing**: Verify processing meets performance requirements
- **User Acceptance Testing**: Tools for user validation of results

## Troubleshooting

### Common Issues

#### Installation Problems
**Issue**: Add-in doesn't appear in Revit
- **Solution**: Check that the .addin file is in the correct location
- **Solution**: Verify the DLL path in the .addin file is correct
- **Solution**: Ensure Revit is running the correct version
- **Solution**: Check Windows Event Viewer for error messages

**Issue**: "Could not load file or assembly" error
- **Solution**: Ensure all dependencies are installed (.NET Framework 4.8)
- **Solution**: Check that Newtonsoft.Json.dll is in the same folder as RevitDtools.dll
- **Solution**: Verify file permissions allow Revit to read the files
- **Solution**: Try running Revit as administrator

#### Processing Issues
**Issue**: DWG geometry not converting properly
- **Solution**: Check that the DWG is properly imported into Revit first
- **Solution**: Verify the DWG contains supported geometry types
- **Solution**: Check layer mapping settings
- **Solution**: Review processing logs for specific error messages

**Issue**: Columns not creating from detail lines
- **Solution**: Ensure detail lines form closed rectangular shapes
- **Solution**: Check that appropriate column families are available
- **Solution**: Verify detail lines are on the correct level
- **Solution**: Check column creation settings and parameters

#### Performance Issues
**Issue**: Processing is very slow
- **Solution**: Reduce the number of elements being processed at once
- **Solution**: Close other applications to free up system resources
- **Solution**: Check performance settings and adjust as needed
- **Solution**: Consider processing files in smaller batches

**Issue**: Out of memory errors
- **Solution**: Process fewer files at once
- **Solution**: Increase virtual memory settings in Windows
- **Solution**: Close other applications to free up RAM
- **Solution**: Consider upgrading system memory

### Error Messages

#### Common Error Messages and Solutions

**"Failed to process geometry element"**
- **Cause**: Invalid or corrupted geometry in the DWG file
- **Solution**: Check the original DWG file for errors
- **Solution**: Try processing individual elements to isolate the problem
- **Solution**: Use DWG cleanup tools before importing

**"Column family could not be created"**
- **Cause**: Invalid dimensions or family template issues
- **Solution**: Check that column dimensions are reasonable
- **Solution**: Verify family templates are available and accessible
- **Solution**: Try using existing families instead of creating new ones

**"Batch processing failed"**
- **Cause**: File access issues or corrupted files
- **Solution**: Check file permissions and accessibility
- **Solution**: Verify all files in the batch are valid DWG files
- **Solution**: Try processing files individually to identify problematic files

### Getting Help

#### Log Files
Log files are stored in: `%APPDATA%\RevitDtools\Logs\`
- **Error Logs**: Detailed error information
- **Performance Logs**: Processing performance data
- **Usage Logs**: Command usage statistics
- **Debug Logs**: Detailed debugging information (when enabled)

#### Diagnostic Information
To gather diagnostic information:
1. Enable detailed logging in Settings
2. Reproduce the issue
3. Collect log files from the logs directory
4. Include system information (Windows version, Revit version, etc.)
5. Provide sample files that demonstrate the issue

#### Support Channels
- **Documentation**: Check this manual and online documentation
- **Community Forum**: Post questions in the user community
- **Technical Support**: Contact technical support for complex issues
- **Bug Reports**: Report bugs through the official channels

## Support and Updates

### Automatic Updates

#### Update Checking
- **Automatic Checking**: The add-in checks for updates weekly by default
- **Manual Checking**: Use the "Check for Updates" command in Settings
- **Update Notifications**: Receive notifications when updates are available
- **Release Notes**: View what's new in each update

#### Installing Updates
- **Automatic Installation**: Updates can be installed automatically
- **Manual Installation**: Download and install updates manually
- **Rollback**: Revert to previous versions if needed
- **Beta Versions**: Opt in to receive beta updates for early access to new features

### Version History

#### Version 2.0.0 (Current)
- **Enhanced Geometry Processing**: Support for all DWG geometry types
- **Advanced Column Creation**: Multiple column types and shapes
- **Batch Processing**: Process multiple files simultaneously
- **Professional Logging**: Comprehensive error tracking and reporting
- **Performance Monitoring**: Real-time performance analysis
- **User Settings**: Persistent configuration and preferences

#### Previous Versions
- **Version 1.x**: Basic DWG to detail line conversion and simple column creation

### Support Resources

#### Documentation
- **User Manual**: This comprehensive guide
- **Quick Start Guide**: Fast introduction to key features
- **Technical Documentation**: Detailed technical information for advanced users
- **Video Tutorials**: Step-by-step video guides
- **FAQ**: Frequently asked questions and answers

#### Community
- **User Forum**: Community discussion and support
- **Knowledge Base**: Searchable database of solutions
- **User Groups**: Local user groups and meetups
- **Training**: Professional training courses and certification

#### Professional Support
- **Technical Support**: Direct support for technical issues
- **Consulting Services**: Professional implementation and customization
- **Training Services**: On-site training and workshops
- **Custom Development**: Custom features and integrations

### Contact Information

**Technical Support**
- Email: support@revitdtools.com
- Phone: 1-800-REVIT-DT
- Hours: Monday-Friday, 9 AM - 5 PM EST

**Sales and Licensing**
- Email: sales@revitdtools.com
- Phone: 1-800-REVIT-DT
- Hours: Monday-Friday, 9 AM - 6 PM EST

**General Information**
- Website: https://www.revitdtools.com
- Documentation: https://docs.revitdtools.com
- Community Forum: https://forum.revitdtools.com

---

*This manual is for RevitDtools Enhanced version 2.0.0. For the most current version of this documentation, visit https://docs.revitdtools.com*
