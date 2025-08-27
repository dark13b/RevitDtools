# RevitDtools Enhanced - Quick Start Guide

## Installation (5 minutes)

### Step 1: Download and Install
1. Download `RevitDtools_Enhanced_v2.0.0.msi`
2. Right-click → "Run as administrator"
3. Follow the installer wizard
4. Select your Revit versions (2024, 2025, 2026)
5. Complete installation

### Step 2: Verify Installation
1. Open Revit
2. Look for the "Dtools" ribbon tab
3. ✅ Success! You should see the RevitDtools commands

## Basic Workflow (10 minutes)

### Convert DWG to Detail Lines

1. **Import DWG File**
   - Use Revit's standard Import CAD tool
   - Import your DWG file into the project

2. **Convert Geometry**
   - Select the imported DWG
   - Click **"Enhanced DWG to Detail Lines"** in the Dtools ribbon
   - Configure layer mapping (or use defaults)
   - Click **"Process"**
   - ✅ DWG geometry is now converted to Revit detail lines

### Create Columns from Detail Lines

1. **Select Detail Lines**
   - Select rectangular detail lines that represent columns
   - Use Ctrl+Click to select multiple lines

2. **Create Columns**
   - Click **"Enhanced Column by Line"** in the Dtools ribbon
   - Choose column family and dimensions
   - Click **"Create Columns"**
   - ✅ Structural columns are created from your detail lines

## Advanced Features (15 minutes)

### Batch Processing Multiple Files

1. **Start Batch Process**
   - Click **"Batch Process"** in the Dtools ribbon
   - Select multiple DWG files or choose a folder
   - Configure processing options

2. **Monitor Progress**
   - Watch real-time progress updates
   - Review processing results
   - Export summary report

### Configure Settings

1. **Open Settings**
   - Click **"Settings"** in the Dtools ribbon
   - Configure your preferences:
     - Default layer mappings
     - Column family preferences
     - Performance settings
     - Update preferences

2. **Save Templates**
   - Create layer mapping templates
   - Save column configuration presets
   - Export settings for team sharing

## Tips for Success

### Best Practices
- ✅ **Clean DWG Files**: Use clean, well-organized DWG files for best results
- ✅ **Layer Organization**: Organize DWG layers logically before processing
- ✅ **Test Small**: Start with small files to understand the workflow
- ✅ **Save Templates**: Create and save mapping templates for repeated use

### Common Pitfalls
- ❌ **Skipping DWG Import**: Always import DWG into Revit first
- ❌ **Complex Geometry**: Very complex curves may need manual cleanup
- ❌ **Missing Families**: Ensure column families are available or let the tool create them
- ❌ **Large Batches**: Start with small batches to test settings

## Troubleshooting

### Installation Issues
**Problem**: Dtools ribbon doesn't appear
- **Solution**: Check that .addin file is in correct Revit addins folder
- **Solution**: Restart Revit after installation
- **Solution**: Run Revit as administrator

### Processing Issues
**Problem**: Geometry doesn't convert properly
- **Solution**: Check that DWG is imported (not linked) into Revit
- **Solution**: Verify layer mapping settings
- **Solution**: Check processing logs for specific errors

**Problem**: Columns don't create
- **Solution**: Ensure detail lines form closed rectangles
- **Solution**: Check that lines are on the correct level
- **Solution**: Verify column family settings

## Getting Help

### Quick Resources
- **Settings Dialog**: Built-in help and tooltips
- **Processing Logs**: Check `%APPDATA%\RevitDtools\Logs\` for detailed logs
- **User Manual**: Complete documentation in the Documentation folder

### Support Channels
- **Email**: support@revitdtools.com
- **Forum**: https://forum.revitdtools.com
- **Documentation**: https://docs.revitdtools.com

## What's Next?

### Explore Advanced Features
- **Circular Columns**: Create round columns with diameter specification
- **Custom Shapes**: Create columns from custom profile curves
- **Column Grids**: Generate column layouts and patterns
- **Performance Monitoring**: Track processing performance and optimization

### Customize Your Workflow
- **Layer Templates**: Create standard layer mapping templates
- **Batch Scripts**: Automate repetitive processing tasks
- **Team Settings**: Share configuration across your team
- **Integration**: Connect with other Revit add-ins and workflows

### Stay Updated
- **Automatic Updates**: Enable automatic update checking
- **Release Notes**: Stay informed about new features
- **Community**: Join the user community for tips and best practices
- **Training**: Consider professional training for advanced usage

---

**Need More Help?**
- 📖 Read the complete [User Manual](UserManual.md)
- 🎥 Watch video tutorials at https://tutorials.revitdtools.com
- 💬 Join the community at https://forum.revitdtools.com
- 📧 Contact support at support@revitdtools.com

*Get productive with RevitDtools Enhanced in under 30 minutes!*