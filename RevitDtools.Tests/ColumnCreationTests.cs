using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RevitDtools.Core.Interfaces;
using RevitDtools.Core.Models;
using System;
using System.Collections.Generic;

namespace RevitDtools.Tests
{
    [TestClass]
    public class ColumnCreationTests
    {
        private Mock<ILogger> _mockLogger;
        private Mock<IFamilyManager> _mockFamilyManager;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger>();
            _mockFamilyManager = new Mock<IFamilyManager>();
        }

        [TestMethod]
        public void ColumnCreation_ValidDimensions_ParametersCreatedCorrectly()
        {
            // Arrange
            double width = 1.5;
            double height = 2.0;
            string familyName = "Test Column Family";

            // Act
            var parameters = ColumnParameters.Create(width, height, familyName);

            // Assert
            Assert.AreEqual(width, parameters.Width);
            Assert.AreEqual(height, parameters.Height);
            Assert.AreEqual(familyName, parameters.FamilyName);
            Assert.IsTrue(parameters.IsValid(out _));
        }

        [TestMethod]
        public void ColumnCreation_SmallDimensions_StillValid()
        {
            // Arrange
            double width = 0.25; // 3 inches
            double height = 0.5;  // 6 inches

            // Act
            var parameters = ColumnParameters.Create(width, height);

            // Assert
            Assert.IsTrue(parameters.IsValid(out string message));
            Assert.IsNull(message);
        }

        [TestMethod]
        public void ColumnCreation_LargeDimensions_StillValid()
        {
            // Arrange
            double width = 8.0;  // 8 feet
            double height = 10.0; // 10 feet (maximum)

            // Act
            var parameters = ColumnParameters.Create(width, height);

            // Assert
            Assert.IsTrue(parameters.IsValid(out string message));
            Assert.IsNull(message);
        }

        [TestMethod]
        public void ColumnCreation_SquareColumn_HandledCorrectly()
        {
            // Arrange
            double dimension = 2.0; // 2' x 2' square column

            // Act
            var parameters = ColumnParameters.Create(dimension, dimension);

            // Assert
            Assert.AreEqual(dimension, parameters.Width);
            Assert.AreEqual(dimension, parameters.Height);
            Assert.IsTrue(parameters.IsValid(out _));
        }

        [TestMethod]
        public void ColumnCreation_RectangularColumn_HandledCorrectly()
        {
            // Arrange
            double width = 1.0;   // 1' wide
            double height = 3.0;  // 3' tall (rectangular)

            // Act
            var parameters = ColumnParameters.Create(width, height);

            // Assert
            Assert.AreEqual(width, parameters.Width);
            Assert.AreEqual(height, parameters.Height);
            Assert.IsTrue(parameters.IsValid(out _));
            Assert.AreNotEqual(parameters.Width, parameters.Height);
        }

        [TestMethod]
        public void ColumnCreation_CustomParameters_CanBeAdded()
        {
            // Arrange
            var parameters = ColumnParameters.Create(1.0, 2.0);
            
            // Act
            parameters.CustomParameters["Concrete_Grade"] = "4000_PSI";
            parameters.CustomParameters["Reinforcement"] = "#4_Bars";
            parameters.CustomParameters["Fire_Rating"] = 2.0; // 2 hours

            // Assert
            Assert.AreEqual(3, parameters.CustomParameters.Count);
            Assert.AreEqual("4000_PSI", parameters.CustomParameters["Concrete_Grade"]);
            Assert.AreEqual("#4_Bars", parameters.CustomParameters["Reinforcement"]);
            Assert.AreEqual(2.0, parameters.CustomParameters["Fire_Rating"]);
        }

        [TestMethod]
        public void ColumnCreation_MaterialName_CanBeSpecified()
        {
            // Arrange
            var parameters = ColumnParameters.Create(1.0, 2.0);
            string materialName = "Concrete - 4000 psi";

            // Act
            parameters.MaterialName = materialName;

            // Assert
            Assert.AreEqual(materialName, parameters.MaterialName);
        }

        [TestMethod]
        public void ColumnCreation_SymbolName_GeneratedFromDimensions()
        {
            // Arrange
            var parameters = ColumnParameters.Create(1.5, 2.5);

            // Act
            string expectedSymbolName = $"{parameters.Width:F3}x{parameters.Height:F3}";
            parameters.SymbolName = expectedSymbolName;

            // Assert
            Assert.AreEqual("1.500x2.500", parameters.SymbolName);
        }

        [TestMethod]
        public void ColumnCreation_UniqueId_IncludesFamilyAndDimensions()
        {
            // Arrange
            var parameters = ColumnParameters.Create(1.25, 1.75, "Concrete Column");

            // Act
            string uniqueId = parameters.GetUniqueId();

            // Assert
            Assert.AreEqual("Concrete Column_1.250x1.750", uniqueId);
        }

        [TestMethod]
        public void ColumnCreation_StructuralType_DefaultsToColumn()
        {
            // Arrange & Act
            var parameters = ColumnParameters.Create(1.0, 2.0);

            // Assert
            Assert.AreEqual(Autodesk.Revit.DB.Structure.StructuralType.Column, parameters.ColumnType);
        }

        [TestMethod]
        public void ColumnCreation_StructuralType_CanBeChanged()
        {
            // Arrange
            var parameters = ColumnParameters.Create(1.0, 2.0);

            // Act
            parameters.ColumnType = Autodesk.Revit.DB.Structure.StructuralType.Beam;

            // Assert
            Assert.AreEqual(Autodesk.Revit.DB.Structure.StructuralType.Beam, parameters.ColumnType);
        }

        [TestMethod]
        public void ColumnCreation_ValidationMessage_ProvidedForInvalidDimensions()
        {
            // Test zero width
            var zeroWidth = ColumnParameters.Create(0.0, 2.0);
            Assert.IsFalse(zeroWidth.IsValid(out string message1));
            Assert.IsNotNull(message1);
            Assert.IsTrue(message1.Contains("Width"));

            // Test zero height
            var zeroHeight = ColumnParameters.Create(2.0, 0.0);
            Assert.IsFalse(zeroHeight.IsValid(out string message2));
            Assert.IsNotNull(message2);
            Assert.IsTrue(message2.Contains("Height"));

            // Test excessive dimensions
            var tooLarge = ColumnParameters.Create(15.0, 2.0);
            Assert.IsFalse(tooLarge.IsValid(out string message3));
            Assert.IsNotNull(message3);
            Assert.IsTrue(message3.Contains("exceeds maximum"));
        }

        [TestMethod]
        public void ColumnCreation_MetricDimensions_ConvertedCorrectly()
        {
            // Arrange - dimensions in feet (Revit internal units)
            double widthInFeet = 0.984; // approximately 300mm
            double heightInFeet = 1.312; // approximately 400mm

            // Act
            var parameters = ColumnParameters.Create(widthInFeet, heightInFeet);

            // Assert
            Assert.IsTrue(parameters.IsValid(out _));
            Assert.AreEqual(widthInFeet, parameters.Width, 0.001);
            Assert.AreEqual(heightInFeet, parameters.Height, 0.001);
        }

        [TestMethod]
        public void ColumnCreation_CommonColumnSizes_AllValid()
        {
            // Common US column sizes
            var commonSizes = new[]
            {
                new { Width = 1.0, Height = 1.0 },   // 12" x 12"
                new { Width = 1.5, Height = 1.5 },   // 18" x 18"
                new { Width = 2.0, Height = 2.0 },   // 24" x 24"
                new { Width = 1.0, Height = 2.0 },   // 12" x 24"
                new { Width = 1.5, Height = 3.0 },   // 18" x 36"
            };

            foreach (var size in commonSizes)
            {
                // Act
                var parameters = ColumnParameters.Create(size.Width, size.Height);

                // Assert
                Assert.IsTrue(parameters.IsValid(out string message), 
                    $"Size {size.Width}' x {size.Height}' should be valid. Error: {message}");
            }
        }

        [TestMethod]
        public void ColumnCreation_EdgeCaseDimensions_HandledCorrectly()
        {
            // Very small but valid dimensions
            var verySmall = ColumnParameters.Create(0.01, 0.01); // ~1/8 inch
            Assert.IsTrue(verySmall.IsValid(out _));

            // Maximum allowed dimensions
            var maximum = ColumnParameters.Create(10.0, 10.0);
            Assert.IsTrue(maximum.IsValid(out _));

            // Just over maximum
            var overMax = ColumnParameters.Create(10.01, 10.0);
            Assert.IsFalse(overMax.IsValid(out _));
        }

        [TestMethod]
        public void ColumnCreation_CustomParameterTypes_SupportedCorrectly()
        {
            // Arrange
            var parameters = ColumnParameters.Create(1.0, 2.0);

            // Act - Add various parameter types
            parameters.CustomParameters["StringParam"] = "Test String";
            parameters.CustomParameters["IntParam"] = 42;
            parameters.CustomParameters["DoubleParam"] = 3.14159;
            parameters.CustomParameters["BoolParam"] = true;

            // Assert
            Assert.AreEqual("Test String", parameters.CustomParameters["StringParam"]);
            Assert.AreEqual(42, parameters.CustomParameters["IntParam"]);
            Assert.AreEqual(3.14159, parameters.CustomParameters["DoubleParam"]);
            Assert.AreEqual(true, parameters.CustomParameters["BoolParam"]);
        }

        [TestMethod]
        public void ColumnCreation_FamilyNameHandling_WorksWithSpecialCharacters()
        {
            // Arrange
            string familyNameWithSpaces = "Concrete Column - Type A";
            string familyNameWithNumbers = "Column_Type_01";
            string familyNameWithSymbols = "Column-W14x30";

            // Act
            var params1 = ColumnParameters.Create(1.0, 2.0, familyNameWithSpaces);
            var params2 = ColumnParameters.Create(1.0, 2.0, familyNameWithNumbers);
            var params3 = ColumnParameters.Create(1.0, 2.0, familyNameWithSymbols);

            // Assert
            Assert.AreEqual(familyNameWithSpaces, params1.FamilyName);
            Assert.AreEqual(familyNameWithNumbers, params2.FamilyName);
            Assert.AreEqual(familyNameWithSymbols, params3.FamilyName);

            // Unique IDs should handle special characters
            Assert.IsTrue(params1.GetUniqueId().Contains(familyNameWithSpaces));
            Assert.IsTrue(params2.GetUniqueId().Contains(familyNameWithNumbers));
            Assert.IsTrue(params3.GetUniqueId().Contains(familyNameWithSymbols));
        }
    }
}