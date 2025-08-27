using Microsoft.VisualStudio.TestTools.UnitTesting;
using RevitDtools.Core.Commands;
using RevitDtools.Core.Services;
using RevitDtools.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitDtools.Tests
{
    /// <summary>
    /// Unit tests for advanced column creation features
    /// </summary>
    [TestClass]
    public class AdvancedColumnCreationTests
    {
        private TestLogger _logger;
        private MockDocument _mockDocument;
        private FamilyManagementService _familyManager;
        private ColumnScheduleService _scheduleService;

        [TestInitialize]
        public void Setup()
        {
            _logger = new TestLogger();
            _mockDocument = new MockDocument();
            _familyManager = new FamilyManagementService(_mockDocument, _logger);
            _scheduleService = new ColumnScheduleService(_mockDocument, _logger);
        }

        #region Circular Column Tests

        [TestMethod]
        public void CircularColumn_ValidDiameter_CreatesSuccessfully()
        {
            // Arrange
            double diameter = 2.0; // 2 feet
            var centerPoint = new MockXYZ(10, 10, 0);
            var level = new MockLevel("Level 1", 0);
            var familySymbol = new MockFamilySymbol("Circular Column", "24in");

            // Act
            var result = CreateMockCircularColumn(centerPoint, diameter, level, familySymbol);

            // Assert
            Assert.IsTrue(result.Success, $"Circular column creation should succeed: {result.Message}");
            Assert.AreEqual(1, result.ElementsProcessed, "Should process exactly 1 element");
            Assert.IsTrue(_logger.InfoMessages.Any(m => m.Contains("Circular column created")), 
                "Should log successful creation");
        }

        [TestMethod]
        public void CircularColumn_InvalidDiameter_ReturnsFailure()
        {
            // Arrange
            double invalidDiameter = -1.0;
            var centerPoint = new MockXYZ(10, 10, 0);

            // Act
            var result = ValidateCircularColumnDiameter(invalidDiameter);

            // Assert
            Assert.IsFalse(result.IsValid, "Invalid diameter should fail validation");
            Assert.IsTrue(result.ErrorMessage.Contains("diameter"), "Error message should mention diameter");
        }

        [TestMethod]
        public void CircularColumn_LargeDiameter_ReturnsWarning()
        {
            // Arrange
            double largeDiameter = 15.0; // 15 feet - very large
            var centerPoint = new MockXYZ(10, 10, 0);

            // Act
            var result = ValidateCircularColumnDiameter(largeDiameter);

            // Assert
            Assert.IsTrue(result.IsValid, "Large diameter should be valid but with warning");
            Assert.IsTrue(_logger.WarningMessages.Any(m => m.Contains("large diameter")), 
                "Should log warning for large diameter");
        }

        [TestMethod]
        public void CircularColumn_FindExistingSymbol_ReturnsCorrectSymbol()
        {
            // Arrange
            double targetDiameter = 1.5;
            var existingSymbols = new List<MockFamilySymbol>
            {
                new MockFamilySymbol("Circular Column", "12in") { DiameterParameter = 1.0 },
                new MockFamilySymbol("Circular Column", "18in") { DiameterParameter = 1.5 },
                new MockFamilySymbol("Circular Column", "24in") { DiameterParameter = 2.0 }
            };

            // Act
            var foundSymbol = FindCircularSymbolByDiameter(existingSymbols, targetDiameter);

            // Assert
            Assert.IsNotNull(foundSymbol, "Should find existing symbol with matching diameter");
            Assert.AreEqual("18in", foundSymbol.Name, "Should return the 18in symbol");
            Assert.AreEqual(1.5, foundSymbol.DiameterParameter, 0.01, "Diameter should match");
        }

        #endregion

        #region Custom Shape Column Tests

        [TestMethod]
        public void CustomShapeColumn_ValidProfile_CreatesSuccessfully()
        {
            // Arrange
            var profileCurves = CreateMockLShapeProfile();
            var placementPoint = new MockXYZ(5, 5, 0);
            var level = new MockLevel("Level 1", 0);

            // Act
            var profileAnalysis = AnalyzeMockProfile(profileCurves);
            var result = CreateMockCustomShapeColumn(placementPoint, profileAnalysis, level);

            // Assert
            Assert.IsTrue(profileAnalysis.IsValid, $"Profile analysis should succeed: {profileAnalysis.ErrorMessage}");
            Assert.IsTrue(result.Success, $"Custom shape column creation should succeed: {result.Message}");
            Assert.AreEqual(4, profileAnalysis.CurveCount, "L-shape should have 4 curves");
        }

        [TestMethod]
        public void CustomShapeColumn_DisconnectedCurves_ShowsWarning()
        {
            // Arrange
            var disconnectedCurves = CreateMockDisconnectedProfile();

            // Act
            var profileAnalysis = AnalyzeMockProfile(disconnectedCurves);

            // Assert
            Assert.IsTrue(profileAnalysis.IsValid, "Should still be valid but with warning");
            Assert.IsFalse(profileAnalysis.IsClosedProfile, "Should detect disconnected profile");
            Assert.IsTrue(_logger.WarningMessages.Any(m => m.Contains("may not form a closed loop")), 
                "Should warn about disconnected curves");
        }

        [TestMethod]
        public void CustomShapeColumn_TooSmallProfile_ReturnsFailure()
        {
            // Arrange
            var tinyProfile = CreateMockTinyProfile();

            // Act
            var profileAnalysis = AnalyzeMockProfile(tinyProfile);

            // Assert
            Assert.IsFalse(profileAnalysis.IsValid, "Tiny profile should fail validation");
            Assert.IsTrue(profileAnalysis.ErrorMessage.Contains("too small"), 
                "Error message should mention size issue");
        }

        [TestMethod]
        public void CustomShapeColumn_ComplexProfile_CalculatesBoundsCorrectly()
        {
            // Arrange
            var complexProfile = CreateMockComplexProfile();

            // Act
            var profileAnalysis = AnalyzeMockProfile(complexProfile);

            // Assert
            Assert.IsTrue(profileAnalysis.IsValid, "Complex profile should be valid");
            Assert.IsTrue(profileAnalysis.BoundingWidth > 0, "Should calculate positive width");
            Assert.IsTrue(profileAnalysis.BoundingHeight > 0, "Should calculate positive height");
            Assert.IsNotNull(profileAnalysis.CenterPoint, "Should calculate center point");
        }

        #endregion

        #region Column Grid Tests

        [TestMethod]
        public void ColumnGrid_3x3Grid_CreatesCorrectNumber()
        {
            // Arrange
            var gridParams = new MockColumnGridParameters
            {
                ColumnsX = 3,
                ColumnsY = 3,
                SpacingX = 20.0,
                SpacingY = 20.0,
                ColumnWidth = 1.0,
                ColumnHeight = 1.0
            };
            var originPoint = new MockXYZ(0, 0, 0);
            var level = new MockLevel("Level 1", 0);

            // Act
            var result = CreateMockColumnGrid(originPoint, gridParams, level);

            // Assert
            Assert.IsTrue(result.Success, $"Grid creation should succeed: {result.Message}");
            Assert.AreEqual(9, result.ElementsProcessed, "3x3 grid should create 9 columns");
        }

        [TestMethod]
        public void ColumnGrid_AsymmetricGrid_CreatesCorrectLayout()
        {
            // Arrange
            var gridParams = new MockColumnGridParameters
            {
                ColumnsX = 5,
                ColumnsY = 2,
                SpacingX = 25.0,
                SpacingY = 30.0,
                ColumnWidth = 1.0,
                ColumnHeight = 1.0
            };
            var originPoint = new MockXYZ(10, 10, 0);

            // Act
            var columnPositions = CalculateGridPositions(originPoint, gridParams);

            // Assert
            Assert.AreEqual(10, columnPositions.Count, "5x2 grid should have 10 positions");
            
            // Check first column position
            var firstColumn = columnPositions[0];
            Assert.AreEqual(10.0, firstColumn.X, 0.01, "First column X should match origin");
            Assert.AreEqual(10.0, firstColumn.Y, 0.01, "First column Y should match origin");
            
            // Check last column position
            var lastColumn = columnPositions.Last();
            Assert.AreEqual(110.0, lastColumn.X, 0.01, "Last column X should be origin + 4*spacing");
            Assert.AreEqual(40.0, lastColumn.Y, 0.01, "Last column Y should be origin + 1*spacing");
        }

        [TestMethod]
        public void ColumnGrid_LargeGrid_PerformsWithinTimeLimit()
        {
            // Arrange
            var gridParams = new MockColumnGridParameters
            {
                ColumnsX = 10,
                ColumnsY = 10,
                SpacingX = 20.0,
                SpacingY = 20.0,
                ColumnWidth = 1.0,
                ColumnHeight = 1.0
            };
            var originPoint = new MockXYZ(0, 0, 0);
            var startTime = DateTime.Now;

            // Act
            var columnPositions = CalculateGridPositions(originPoint, gridParams);
            var endTime = DateTime.Now;
            var duration = endTime - startTime;

            // Assert
            Assert.AreEqual(100, columnPositions.Count, "10x10 grid should have 100 positions");
            Assert.IsTrue(duration.TotalSeconds < 1.0, "Large grid calculation should complete within 1 second");
        }

        [TestMethod]
        public void ColumnGrid_SetColumnMarks_GeneratesCorrectMarks()
        {
            // Arrange
            var gridParams = new MockColumnGridParameters
            {
                ColumnsX = 3,
                ColumnsY = 2,
                SpacingX = 20.0,
                SpacingY = 20.0
            };

            // Act
            var columnMarks = GenerateColumnMarks(gridParams);

            // Assert
            Assert.AreEqual(6, columnMarks.Count, "3x2 grid should have 6 marks");
            Assert.IsTrue(columnMarks.Contains("C1A"), "Should contain C1A");
            Assert.IsTrue(columnMarks.Contains("C3B"), "Should contain C3B");
            Assert.IsFalse(columnMarks.Contains("C4A"), "Should not contain C4A (out of range)");
        }

        #endregion

        #region Column Schedule Integration Tests

        [TestMethod]
        public void ColumnSchedule_ApplyScheduleData_SetsParametersCorrectly()
        {
            // Arrange
            var column = new MockFamilyInstance("Test Column");
            var scheduleData = new ColumnScheduleData
            {
                ColumnMark = "C1",
                Width = 1.5,
                Height = 1.5,
                Material = "Concrete",
                StructuralUsage = "Column",
                LoadBearing = true,
                FireRating = "2 Hour",
                Comments = "Test column with schedule data"
            };

            // Act
            var result = _scheduleService.ApplyScheduleData(column, scheduleData);

            // Assert
            Assert.IsTrue(result.Success, $"Schedule data application should succeed: {result.Message}");
            Assert.IsTrue(result.ElementsProcessed > 0, "Should process at least one parameter group");
        }

        [TestMethod]
        public void ColumnSchedule_FindMatchingData_MatchesByMark()
        {
            // Arrange
            var scheduleData = new ColumnScheduleData
            {
                ColumnMark = "C2",
                Width = 2.0,
                Height = 1.0,
                Material = "Steel"
            };
            _scheduleService.AddOrUpdateScheduleData(scheduleData);
            
            var column = new MockFamilyInstance("Test Column");
            column.SetParameter("Mark", "C2");

            // Act
            var result = _scheduleService.ApplyScheduleData(column);

            // Assert
            Assert.IsTrue(result.Success, "Should find and apply matching schedule data");
        }

        [TestMethod]
        public void ColumnSchedule_FindMatchingData_MatchesByDimensions()
        {
            // Arrange
            var scheduleData = new ColumnScheduleData
            {
                ColumnMark = "C3",
                Width = 1.0,
                Height = 2.0,
                Material = "Concrete"
            };
            _scheduleService.AddOrUpdateScheduleData(scheduleData);
            
            var column = new MockFamilyInstance("Test Column");
            column.SetParameter("Width", 1.0);
            column.SetParameter("Height", 2.0);

            // Act
            var result = _scheduleService.ApplyScheduleData(column);

            // Assert
            Assert.IsTrue(result.Success, "Should find and apply schedule data by dimensions");
        }

        [TestMethod]
        public void ColumnSchedule_LoadFromProject_FindsColumnSchedules()
        {
            // Arrange
            _mockDocument.AddSchedule("Column Schedule", "Structural Columns");
            _mockDocument.AddSchedule("Beam Schedule", "Structural Framing");
            _mockDocument.AddSchedule("Foundation Schedule", "Structural Foundations");

            // Act
            var result = _scheduleService.LoadScheduleDataFromProject();

            // Assert
            Assert.IsTrue(result.Success, "Should successfully load schedule data from project");
            Assert.IsTrue(_logger.InfoMessages.Any(m => m.Contains("Column Schedule")), 
                "Should find and process column schedule");
        }

        #endregion

        #region Integration Tests

        [TestMethod]
        public void AdvancedColumnCreation_EndToEndWorkflow_CompletesSuccessfully()
        {
            // Arrange
            var originPoint = new MockXYZ(0, 0, 0);
            var level = new MockLevel("Level 1", 0);
            
            // Create schedule data
            var scheduleData = new ColumnScheduleData
            {
                ColumnMark = "C1",
                Width = 1.0,
                Height = 1.0,
                Material = "Concrete",
                Comments = "Integration test column"
            };
            _scheduleService.AddOrUpdateScheduleData(scheduleData);

            // Act
            // 1. Create circular column
            var circularResult = CreateMockCircularColumn(originPoint, 2.0, level, null);
            
            // 2. Create custom shape column
            var customProfile = CreateMockLShapeProfile();
            var customResult = CreateMockCustomShapeColumn(new MockXYZ(10, 0, 0), AnalyzeMockProfile(customProfile), level);
            
            // 3. Create column grid
            var gridParams = new MockColumnGridParameters { ColumnsX = 2, ColumnsY = 2, SpacingX = 20, SpacingY = 20 };
            var gridResult = CreateMockColumnGrid(new MockXYZ(20, 0, 0), gridParams, level);

            // Assert
            Assert.IsTrue(circularResult.Success, "Circular column creation should succeed");
            Assert.IsTrue(customResult.Success, "Custom shape column creation should succeed");
            Assert.IsTrue(gridResult.Success, "Column grid creation should succeed");
            
            // Verify total elements created
            int totalElements = circularResult.ElementsProcessed + customResult.ElementsProcessed + gridResult.ElementsProcessed;
            Assert.AreEqual(6, totalElements, "Should create 6 total elements (1 circular + 1 custom + 4 grid)");
        }

        [TestMethod]
        public void AdvancedColumnCreation_ErrorHandling_RecoverGracefully()
        {
            // Arrange
            var invalidPoint = new MockXYZ(double.NaN, double.NaN, 0);
            var level = new MockLevel("Level 1", 0);

            // Act
            var result = CreateMockCircularColumn(invalidPoint, 1.0, level, null);

            // Assert
            Assert.IsFalse(result.Success, "Should fail with invalid coordinates");
            Assert.IsTrue(result.Message.Contains("error"), "Error message should indicate the problem");
            Assert.IsTrue(_logger.ErrorMessages.Any(), "Should log error details");
        }

        #endregion

        #region Helper Methods

        private ProcessingResult CreateMockCircularColumn(MockXYZ centerPoint, double diameter, MockLevel level, MockFamilySymbol familySymbol)
        {
            try
            {
                if (double.IsNaN(centerPoint.X) || double.IsNaN(centerPoint.Y))
                {
                    return ProcessingResult.CreateFailure("Invalid center point coordinates");
                }

                if (diameter <= 0)
                {
                    return ProcessingResult.CreateFailure("Invalid diameter");
                }

                if (diameter > 10.0)
                {
                    _logger.LogWarning($"Large diameter specified: {diameter:F1}' - this may be unusually large");
                }

                _logger.LogInfo($"Circular column created successfully at ({centerPoint.X:F2}, {centerPoint.Y:F2}) with diameter {diameter:F3}'");
                return ProcessingResult.CreateSuccess(1, $"Circular column created at ({centerPoint.X:F2}, {centerPoint.Y:F2})");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateMockCircularColumn");
                return ProcessingResult.CreateFailure($"Error creating circular column: {ex.Message}", ex);
            }
        }

        private MockValidationResult ValidateCircularColumnDiameter(double diameter)
        {
            if (diameter <= 0)
            {
                return new MockValidationResult { IsValid = false, ErrorMessage = "Diameter must be greater than zero" };
            }

            if (diameter > 10.0)
            {
                _logger.LogWarning($"Large diameter specified: {diameter:F1}' - this may be unusually large");
            }

            return new MockValidationResult { IsValid = true };
        }

        private MockFamilySymbol FindCircularSymbolByDiameter(List<MockFamilySymbol> symbols, double targetDiameter)
        {
            const double tolerance = 0.01;
            return symbols.FirstOrDefault(s => Math.Abs(s.DiameterParameter - targetDiameter) < tolerance);
        }

        private List<MockCurve> CreateMockLShapeProfile()
        {
            return new List<MockCurve>
            {
                new MockCurve(new MockXYZ(0, 0, 0), new MockXYZ(3, 0, 0)), // Bottom horizontal
                new MockCurve(new MockXYZ(3, 0, 0), new MockXYZ(3, 1, 0)), // Right vertical (short)
                new MockCurve(new MockXYZ(3, 1, 0), new MockXYZ(1, 1, 0)), // Top horizontal (short)
                new MockCurve(new MockXYZ(1, 1, 0), new MockXYZ(1, 3, 0)), // Left vertical (long)
                new MockCurve(new MockXYZ(1, 3, 0), new MockXYZ(0, 3, 0)), // Top horizontal (end)
                new MockCurve(new MockXYZ(0, 3, 0), new MockXYZ(0, 0, 0))  // Left vertical (close)
            };
        }

        private List<MockCurve> CreateMockDisconnectedProfile()
        {
            return new List<MockCurve>
            {
                new MockCurve(new MockXYZ(0, 0, 0), new MockXYZ(1, 0, 0)),
                new MockCurve(new MockXYZ(2, 0, 0), new MockXYZ(2, 1, 0)), // Gap between curves
                new MockCurve(new MockXYZ(2, 1, 0), new MockXYZ(0, 1, 0)),
                new MockCurve(new MockXYZ(0, 1, 0), new MockXYZ(0, 0, 0))
            };
        }

        private List<MockCurve> CreateMockTinyProfile()
        {
            return new List<MockCurve>
            {
                new MockCurve(new MockXYZ(0, 0, 0), new MockXYZ(0.005, 0, 0)),
                new MockCurve(new MockXYZ(0.005, 0, 0), new MockXYZ(0.005, 0.005, 0)),
                new MockCurve(new MockXYZ(0.005, 0.005, 0), new MockXYZ(0, 0.005, 0)),
                new MockCurve(new MockXYZ(0, 0.005, 0), new MockXYZ(0, 0, 0))
            };
        }

        private List<MockCurve> CreateMockComplexProfile()
        {
            var curves = new List<MockCurve>();
            
            // Create a complex star-like profile
            int points = 8;
            double outerRadius = 2.0;
            double innerRadius = 1.0;
            
            for (int i = 0; i < points; i++)
            {
                double angle1 = (i * 2 * Math.PI) / points;
                double angle2 = ((i + 0.5) * 2 * Math.PI) / points;
                double angle3 = ((i + 1) * 2 * Math.PI) / points;
                
                var outerPoint1 = new MockXYZ(outerRadius * Math.Cos(angle1), outerRadius * Math.Sin(angle1), 0);
                var innerPoint = new MockXYZ(innerRadius * Math.Cos(angle2), innerRadius * Math.Sin(angle2), 0);
                var outerPoint2 = new MockXYZ(outerRadius * Math.Cos(angle3), outerRadius * Math.Sin(angle3), 0);
                
                curves.Add(new MockCurve(outerPoint1, innerPoint));
                curves.Add(new MockCurve(innerPoint, outerPoint2));
            }
            
            return curves;
        }

        private MockProfileAnalysis AnalyzeMockProfile(List<MockCurve> curves)
        {
            try
            {
                if (!curves.Any())
                {
                    return new MockProfileAnalysis
                    {
                        IsValid = false,
                        ErrorMessage = "No curves provided for profile analysis"
                    };
                }

                // Get all endpoints
                var allPoints = new List<MockXYZ>();
                foreach (var curve in curves)
                {
                    allPoints.Add(curve.StartPoint);
                    allPoints.Add(curve.EndPoint);
                }

                // Calculate bounding box
                double minX = allPoints.Min(p => p.X);
                double minY = allPoints.Min(p => p.Y);
                double maxX = allPoints.Max(p => p.X);
                double maxY = allPoints.Max(p => p.Y);

                double boundingWidth = maxX - minX;
                double boundingHeight = maxY - minY;
                var centerPoint = new MockXYZ((minX + maxX) / 2, (minY + maxY) / 2, allPoints[0].Z);

                // Basic validation
                if (boundingWidth < 0.01 || boundingHeight < 0.01)
                {
                    return new MockProfileAnalysis
                    {
                        IsValid = false,
                        ErrorMessage = $"Profile is too small: {boundingWidth:F3}' × {boundingHeight:F3}'"
                    };
                }

                // Check connectivity
                bool isConnected = CheckMockCurveConnectivity(curves);
                if (!isConnected)
                {
                    _logger.LogWarning("Profile curves may not form a closed loop - proceeding anyway");
                }

                return new MockProfileAnalysis
                {
                    IsValid = true,
                    CurveCount = curves.Count,
                    BoundingWidth = boundingWidth,
                    BoundingHeight = boundingHeight,
                    CenterPoint = centerPoint,
                    IsClosedProfile = isConnected
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AnalyzeMockProfile");
                return new MockProfileAnalysis
                {
                    IsValid = false,
                    ErrorMessage = $"Error analyzing profile: {ex.Message}"
                };
            }
        }

        private bool CheckMockCurveConnectivity(List<MockCurve> curves)
        {
            const double tolerance = 1e-6;
            
            foreach (var curve in curves)
            {
                bool startConnected = false;
                bool endConnected = false;
                
                foreach (var otherCurve in curves)
                {
                    if (curve == otherCurve) continue;
                    
                    if (curve.StartPoint.IsAlmostEqualTo(otherCurve.StartPoint, tolerance) || 
                        curve.StartPoint.IsAlmostEqualTo(otherCurve.EndPoint, tolerance))
                        startConnected = true;
                    
                    if (curve.EndPoint.IsAlmostEqualTo(otherCurve.StartPoint, tolerance) || 
                        curve.EndPoint.IsAlmostEqualTo(otherCurve.EndPoint, tolerance))
                        endConnected = true;
                }
                
                if (!startConnected || !endConnected)
                    return false;
            }
            
            return true;
        }

        private ProcessingResult CreateMockCustomShapeColumn(MockXYZ placementPoint, MockProfileAnalysis profileAnalysis, MockLevel level)
        {
            try
            {
                if (!profileAnalysis.IsValid)
                {
                    return ProcessingResult.CreateFailure($"Invalid profile: {profileAnalysis.ErrorMessage}");
                }

                _logger.LogInfo($"Custom shape column created successfully at ({placementPoint.X:F2}, {placementPoint.Y:F2})");
                return ProcessingResult.CreateSuccess(1, $"Custom shape column created at ({placementPoint.X:F2}, {placementPoint.Y:F2})");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateMockCustomShapeColumn");
                return ProcessingResult.CreateFailure($"Error creating custom shape column: {ex.Message}", ex);
            }
        }

        private ProcessingResult CreateMockColumnGrid(MockXYZ originPoint, MockColumnGridParameters gridParams, MockLevel level)
        {
            try
            {
                int totalColumns = gridParams.ColumnsX * gridParams.ColumnsY;
                _logger.LogInfo($"Column grid created: {totalColumns} columns in {gridParams.ColumnsX}×{gridParams.ColumnsY} pattern");
                return ProcessingResult.CreateSuccess(totalColumns, $"Created {totalColumns} columns in grid");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateMockColumnGrid");
                return ProcessingResult.CreateFailure($"Error creating column grid: {ex.Message}", ex);
            }
        }

        private List<MockXYZ> CalculateGridPositions(MockXYZ originPoint, MockColumnGridParameters gridParams)
        {
            var positions = new List<MockXYZ>();
            
            for (int x = 0; x < gridParams.ColumnsX; x++)
            {
                for (int y = 0; y < gridParams.ColumnsY; y++)
                {
                    double columnX = originPoint.X + (x * gridParams.SpacingX);
                    double columnY = originPoint.Y + (y * gridParams.SpacingY);
                    positions.Add(new MockXYZ(columnX, columnY, originPoint.Z));
                }
            }
            
            return positions;
        }

        private List<string> GenerateColumnMarks(MockColumnGridParameters gridParams)
        {
            var marks = new List<string>();
            
            for (int x = 0; x < gridParams.ColumnsX; x++)
            {
                for (int y = 0; y < gridParams.ColumnsY; y++)
                {
                    string mark = $"C{x + 1}{(char)('A' + y)}";
                    marks.Add(mark);
                }
            }
            
            return marks;
        }

        #endregion

        #region Mock Classes

        private class MockValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; }
        }

        private class MockProfileAnalysis
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; }
            public int CurveCount { get; set; }
            public double BoundingWidth { get; set; }
            public double BoundingHeight { get; set; }
            public MockXYZ CenterPoint { get; set; }
            public bool IsClosedProfile { get; set; }
        }

        private class MockColumnGridParameters
        {
            public int ColumnsX { get; set; }
            public int ColumnsY { get; set; }
            public double SpacingX { get; set; }
            public double SpacingY { get; set; }
            public double ColumnWidth { get; set; } = 1.0;
            public double ColumnHeight { get; set; } = 1.0;
        }

        private class MockXYZ
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }

            public MockXYZ(double x, double y, double z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public bool IsAlmostEqualTo(MockXYZ other, double tolerance)
            {
                return Math.Abs(X - other.X) < tolerance &&
                       Math.Abs(Y - other.Y) < tolerance &&
                       Math.Abs(Z - other.Z) < tolerance;
            }
        }

        private class MockCurve
        {
            public MockXYZ StartPoint { get; set; }
            public MockXYZ EndPoint { get; set; }

            public MockCurve(MockXYZ start, MockXYZ end)
            {
                StartPoint = start;
                EndPoint = end;
            }
        }

        private class MockLevel
        {
            public string Name { get; set; }
            public double Elevation { get; set; }

            public MockLevel(string name, double elevation)
            {
                Name = name;
                Elevation = elevation;
            }
        }

        private class MockFamilySymbol
        {
            public string FamilyName { get; set; }
            public string Name { get; set; }
            public double DiameterParameter { get; set; }

            public MockFamilySymbol(string familyName, string name)
            {
                FamilyName = familyName;
                Name = name;
            }
        }

        private class MockFamilyInstance
        {
            public string Name { get; set; }
            private Dictionary<string, object> _parameters = new Dictionary<string, object>();

            public MockFamilyInstance(string name)
            {
                Name = name;
            }

            public void SetParameter(string paramName, object value)
            {
                _parameters[paramName] = value;
            }

            public T GetParameter<T>(string paramName)
            {
                return _parameters.ContainsKey(paramName) ? (T)_parameters[paramName] : default(T);
            }
        }

        private class MockDocument
        {
            private List<string> _schedules = new List<string>();

            public void AddSchedule(string name, string category)
            {
                _schedules.Add($"{name}|{category}");
            }

            public List<string> GetSchedules()
            {
                return _schedules;
            }
        }

        #endregion
    }
}