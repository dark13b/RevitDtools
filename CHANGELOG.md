# Changelog

All notable changes to RevitDtools will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2025-08-28

### 🎉 Major Fixes & Enhancements

#### Fixed
- **Transaction Context Errors**: Resolved "Starting a transaction from an external application running outside of API context is not allowed" error
  - Implemented proper transaction separation for symbol activation and column creation
  - Eliminated nested transaction conflicts
  - Added graceful error handling for activation failures

- **Batch Processing Failures**: Fixed issue where only partial columns were created (e.g., 6 out of 66)
  - Enhanced fallback system for family symbol matching
  - Added similarity-based symbol selection when exact matches aren't found
  - Improved error reporting with specific failure reasons

- **Logger Compilation Errors**: Fixed all Logger static method call issues
  - Updated Logger calls to use proper static method signatures
  - Added required context parameters to all logging calls
  - Resolved TaskDialog namespace conflicts

#### Added
- **Diagnostic Tools**: New family diagnostic capabilities
  - `DiagnoseFamilyIssues` command to analyze available families and symbols
  - Detailed reporting of why batch processing might fail
  - Parameter analysis and recommendations

- **Standard Family Loader**: Automatic family loading functionality
  - `LoadStandardColumnFamilies` command to load common Revit families
  - Searches standard Revit installation paths
  - Reports loading results and provides manual loading instructions

- **Enhanced Fallback System**: Improved symbol matching
  - Similarity scoring based on area and aspect ratio
  - Graceful degradation to any available column symbol
  - Better error messages explaining fallback choices

#### Improved
- **Error Handling**: More robust error handling throughout the application
  - Detailed error messages with context
  - Graceful failure recovery
  - Better user feedback during operations

- **Performance**: Optimized batch processing workflow
  - Pre-processing of family symbols to avoid repeated lookups
  - Separated symbol activation from column creation
  - Reduced API calls through caching

- **User Experience**: Enhanced user interface and feedback
  - Detailed progress reporting during batch operations
  - Clear success/failure summaries
  - Actionable error messages with solutions

### 🔧 Technical Changes

#### Architecture
- Separated transaction management for better reliability
- Implemented symbol caching to improve performance
- Added comprehensive logging throughout the application
- Enhanced service layer with better error handling

#### Code Quality
- Fixed all compilation warnings related to Logger usage
- Improved code organization and separation of concerns
- Added proper exception handling and recovery
- Enhanced documentation and code comments

## [1.0.0] - 2025-08-27

### 🚀 Initial Release

#### Added
- **DWG Processing**: Convert DWG layers to Revit detail lines
  - Support for lines, arcs, polylines
  - Layer selection and filtering
  - Comprehensive geometry conversion

- **Column Creation**: Create structural columns from detail lines
  - Rectangle detection from connected lines
  - Automatic dimension calculation
  - Family symbol management

- **Batch Operations**: Process multiple elements simultaneously
  - Rectangle detection from detail line groups
  - Bulk column creation
  - Progress tracking and reporting

- **User Interface**: Comprehensive ribbon integration
  - Organized command panels
  - Context-sensitive availability
  - User-friendly dialogs and feedback

#### Core Features
- Family management service for column families
- Geometry processing for DWG imports
- Logging and error handling utilities
- Settings and configuration management

---

## 🔮 Upcoming Features

### Planned for v1.2.0
- [ ] Enhanced DWG geometry support (splines, ellipses, text)
- [ ] Column grid creation tools
- [ ] Integration with Revit schedules
- [ ] Custom family creation capabilities
- [ ] Advanced batch processing options

### Future Considerations
- [ ] Support for additional Revit versions
- [ ] MEP element creation tools
- [ ] Advanced geometry analysis
- [ ] Cloud-based family libraries
- [ ] API for third-party integrations

---

## 📝 Notes

### Breaking Changes
None in this release.

### Migration Guide
No migration required for new installations.

### Known Issues
All major known issues have been resolved in v1.1.0. See the [Issues](../../issues) page for any newly discovered issues.

### Contributors
- Initial development and architecture
- Transaction management fixes
- Diagnostic tools implementation
- Documentation and testing

---

**For detailed technical information about any release, see the corresponding [GitHub Release](../../releases) page.**