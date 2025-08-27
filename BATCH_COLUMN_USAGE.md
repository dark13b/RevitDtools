# Batch Column Creation Tool

## Overview
The Batch Column Creation tool automatically detects rectangles formed by detail lines and creates a structural column inside each rectangle. This solves the problem of creating columns for each individual line by instead grouping lines into rectangular shapes.

## How It Works

### Problem Solved
- **Before**: The tool was creating a column for each individual line
- **After**: The tool now detects groups of 4 lines that form rectangles and creates one column inside each rectangle

### Algorithm
1. **Line Selection**: Select all detail lines that form rectangles
2. **Rectangle Detection**: Automatically groups lines into sets of 4 that form closed rectangles
3. **Validation**: Ensures each group forms a proper rectangle with 4 corners
4. **Column Creation**: Places one structural column at the center of each detected rectangle

## Usage Instructions

### Step 1: Access the Tool
- Open Revit with the RevitDtools add-in installed
- Go to the "RevitDtools" tab in the ribbon
- Click on "Batch Columns" in the Column Creation panel

### Step 2: Select Detail Lines
- **Option A**: Pre-select all detail lines that form rectangles before running the tool
- **Option B**: Run the tool first, then select lines when prompted

### Step 3: Review Detection Results
- The tool will analyze your selection and detect rectangular groups
- A confirmation dialog shows how many rectangles were found
- Example: "Found 5 rectangles from 20 detail lines"

### Step 4: Confirm Creation
- Click "Yes" to proceed with column creation
- The tool will create one column inside each detected rectangle

## Requirements

### Line Requirements
- Lines must be **Detail Lines** (not model lines or other elements)
- Each rectangle must consist of **exactly 4 lines**
- Lines must be **connected end-to-end** to form a closed shape
- Lines should be **properly aligned** (horizontal and vertical)

### Rectangle Requirements
- **Minimum size**: Very small rectangles (< 3mm or 0.01') are rejected
- **Maximum size**: Very large rectangles (> 15m or 50') are rejected
- Must form a **closed rectangular shape**
- No overlapping or duplicate lines
- **Units**: Tool automatically works with your document's units (metric or imperial)

## Example Workflow

```
1. Draw detail lines in rectangular patterns:
   ┌─────┐  ┌───┐  ┌─────────┐
   │     │  │   │  │         │
   │     │  │   │  │         │
   └─────┘  └───┘  └─────────┘

2. Select all 12 lines (4 lines × 3 rectangles)

3. Run "Batch Columns" tool

4. Tool detects 3 rectangles

5. Creates 3 columns, one in the center of each rectangle:
   ┌─────┐  ┌───┐  ┌─────────┐
   │  ■  │  │ ■ │  │    ■    │
   │     │  │   │  │         │
   └─────┘  └───┘  └─────────┘
```

## Results and Feedback

### Success Message
- Shows number of columns created
- Lists dimensions and locations of each column
- Example: "3 columns created: 2.5' × 3.0' at (10, 20), ..."

### Error Handling
- **No rectangles found**: Check that lines form closed rectangular shapes
- **Family not found**: Tool will attempt to create appropriate column families
- **Invalid dimensions**: Rectangles must be within size limits

## Tips for Best Results

1. **Draw Clean Rectangles**: Ensure lines connect precisely at corners
2. **Use Consistent Line Types**: All lines should be Detail Lines
3. **Avoid Overlaps**: Don't have duplicate or overlapping lines
4. **Check Alignment**: Lines should be horizontal and vertical
5. **Group Selection**: Select all related lines at once for batch processing

## Troubleshooting

### Common Issues
- **"No rectangles detected"**: Lines may not be properly connected
- **"Transaction error"**: Must run within Revit (not external applications)
- **"Family not found"**: Column families may need to be loaded

### Solutions
- Use snapping to ensure precise line connections
- Check that all lines are Detail Lines
- Verify rectangles are within size limits
- Ensure Revit has appropriate column families loaded

## Technical Details

- **Units**: Respects document units automatically (metric/imperial)
- **Tolerance**: High precision point matching (0.0003mm tolerance)
- **Family Management**: Automatically finds or creates appropriate column families
- **Level Placement**: Uses the level closest to the line elevation
- **Transaction Safety**: All operations are wrapped in proper Revit transactions
- **API Units**: Revit API works internally in feet, but all user-facing values respect document settings