using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RevitDtools.Core.Interfaces;
using RevitDtools.Core.Models;
using RevitDtools.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitDtools.Tests
{
    [TestClass]
    public class FamilyManagementTests
    {
        private Mock<ILogger> _mockLogger;
        private ColumnParameters _testParameters;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger>();
            _testParameters = ColumnParameters.Create(1.0, 1.5, "Test Column");
        }

        [TestMethod]
        public void ColumnParameters_Create_SetsCorrectValues()
        {
            // Arrange
            double width = 2.0;
            double height = 3.0;
            string familyName = "Custom Column";

            // Act
            var parameters = ColumnParameters.Create(width, height, familyName);

            // Assert
            Assert.AreEqual(width, parameters.Width);
            Assert.AreEqual(height, parameters.Height);
            Assert.AreEqual(familyName, parameters.FamilyName);
            Assert.IsNotNull(parameters.CustomParameters);
        }

        [TestMethod]
        public void ColumnParameters_IsValid_ValidParameters_ReturnsTrue()
        {
            // Arrange
            var parameters = ColumnParameters.Create(1.0, 2.0);

            // Act
            bool isValid = parameters.IsValid(out string message);

            // Assert
            Assert.IsTrue(isValid);
            Assert.IsNull(message);
        }

        [TestMethod]
        public void ColumnParameters_IsValid_ZeroWidth_ReturnsFalse()
        {
            // Arrange
            var parameters = ColumnParameters.Create(0.0, 2.0);

            // Act
            bool isValid = parameters.IsValid(out string message);

            // Assert
            Assert.IsFalse(isValid);
            Assert.AreEqual("Width must be greater than zero", message);
        }

        [TestMethod]
        public void ColumnParameters_IsValid_ZeroHeight_ReturnsFalse()
        {
            // Arrange
            var parameters = ColumnParameters.Create(1.0, 0.0);

            // Act
            bool isValid = parameters.IsValid(out string message);

            // Assert
            Assert.IsFalse(isValid);
            Assert.AreEqual("Height must be greater than zero", message);
        }

        [TestMethod]
        public void ColumnParameters_IsValid_ExcessiveWidth_ReturnsFalse()
        {
            // Arrange
            var parameters = ColumnParameters.Create(15.0, 2.0);

            // Act
            bool isValid = parameters.IsValid(out string message);

            // Assert
            Assert.IsFalse(isValid);
            Assert.AreEqual("Width exceeds maximum allowed size (10 feet)", message);
        }

        [TestMethod]
        public void ColumnParameters_IsValid_ExcessiveHeight_ReturnsFalse()
        {
            // Arrange
            var parameters = ColumnParameters.Create(2.0, 15.0);

            // Act
            bool isValid = parameters.IsValid(out string message);

            // Assert
            Assert.IsFalse(isValid);
            Assert.AreEqual("Height exceeds maximum allowed size (10 feet)", message);
        }

        [TestMethod]
        public void ColumnParameters_GetUniqueId_GeneratesCorrectId()
        {
            // Arrange
            var parameters = ColumnParameters.Create(1.5, 2.5, "Test Family");

            // Act
            string uniqueId = parameters.GetUniqueId();

            // Assert
            Assert.AreEqual("Test Family_1.500x2.500", uniqueId);
        }

        [TestMethod]
        public void ColumnParameters_CustomParameters_CanAddAndRetrieve()
        {
            // Arrange
            var parameters = ColumnParameters.Create(1.0, 2.0);
            string paramName = "TestParam";
            string paramValue = "TestValue";

            // Act
            parameters.CustomParameters[paramName] = paramValue;

            // Assert
            Assert.IsTrue(parameters.CustomParameters.ContainsKey(paramName));
            Assert.AreEqual(paramValue, parameters.CustomParameters[paramName]);
        }

        [TestMethod]
        public void ColumnParameters_StructuralType_DefaultsToColumn()
        {
            // Arrange & Act
            var parameters = ColumnParameters.Create(1.0, 2.0);

            // Assert
            Assert.AreEqual(Autodesk.Revit.DB.Structure.StructuralType.Column, parameters.ColumnType);
        }

        [TestMethod]
        public void ColumnParameters_SymbolName_CanBeSet()
        {
            // Arrange
            var parameters = ColumnParameters.Create(1.0, 2.0);
            string symbolName = "Custom Symbol";

            // Act
            parameters.SymbolName = symbolName;

            // Assert
            Assert.AreEqual(symbolName, parameters.SymbolName);
        }

        [TestMethod]
        public void ColumnParameters_MaterialName_CanBeSet()
        {
            // Arrange
            var parameters = ColumnParameters.Create(1.0, 2.0);
            string materialName = "Concrete";

            // Act
            parameters.MaterialName = materialName;

            // Assert
            Assert.AreEqual(materialName, parameters.MaterialName);
        }

        [TestMethod]
        public void ColumnParameters_MultipleCustomParameters_CanBeAdded()
        {
            // Arrange
            var parameters = ColumnParameters.Create(1.0, 2.0);

            // Act
            parameters.CustomParameters["Param1"] = "Value1";
            parameters.CustomParameters["Param2"] = 42;
            parameters.CustomParameters["Param3"] = 3.14;

            // Assert
            Assert.AreEqual(3, parameters.CustomParameters.Count);
            Assert.AreEqual("Value1", parameters.CustomParameters["Param1"]);
            Assert.AreEqual(42, parameters.CustomParameters["Param2"]);
            Assert.AreEqual(3.14, parameters.CustomParameters["Param3"]);
        }

        [TestMethod]
        public void ColumnParameters_IsValid_BoundaryValues_HandledCorrectly()
        {
            // Test minimum valid values
            var minParams = ColumnParameters.Create(0.001, 0.001);
            Assert.IsTrue(minParams.IsValid(out _));

            // Test maximum valid values
            var maxParams = ColumnParameters.Create(10.0, 10.0);
            Assert.IsTrue(maxParams.IsValid(out _));

            // Test just over maximum
            var overMaxWidth = ColumnParameters.Create(10.001, 5.0);
            Assert.IsFalse(overMaxWidth.IsValid(out _));

            var overMaxHeight = ColumnParameters.Create(5.0, 10.001);
            Assert.IsFalse(overMaxHeight.IsValid(out _));
        }

        [TestMethod]
        public void ColumnParameters_GetUniqueId_HandlesNullFamilyName()
        {
            // Arrange
            var parameters = new ColumnParameters
            {
                Width = 1.0,
                Height = 2.0,
                FamilyName = null
            };

            // Act
            string uniqueId = parameters.GetUniqueId();

            // Assert
            Assert.AreEqual("_1.000x2.000", uniqueId);
        }

        [TestMethod]
        public void ColumnParameters_GetUniqueId_HandlesEmptyFamilyName()
        {
            // Arrange
            var parameters = new ColumnParameters
            {
                Width = 1.0,
                Height = 2.0,
                FamilyName = ""
            };

            // Act
            string uniqueId = parameters.GetUniqueId();

            // Assert
            Assert.AreEqual("_1.000x2.000", uniqueId);
        }

        [TestMethod]
        public void ColumnParameters_IsValid_NegativeValues_ReturnsFalse()
        {
            // Test negative width
            var negativeWidth = ColumnParameters.Create(-1.0, 2.0);
            Assert.IsFalse(negativeWidth.IsValid(out string message1));
            Assert.AreEqual("Width must be greater than zero", message1);

            // Test negative height
            var negativeHeight = ColumnParameters.Create(1.0, -2.0);
            Assert.IsFalse(negativeHeight.IsValid(out string message2));
            Assert.AreEqual("Height must be greater than zero", message2);
        }

        // Note: The following tests would require actual Revit API objects and Document context
        // They are included as examples of what should be tested in an integration test environment

        /*
        [TestMethod]
        public void FamilyManagementService_Constructor_InitializesCorrectly()
        {
            // This would require a mock Document and actual Revit context
            // var mockDocument = new Mock<Document>();
            // var service = new FamilyManagementService(mockDocument.Object, _mockLogger.Object);
            // Assert.IsNotNull(service);
        }

        [TestMethod]
        public void FamilyManagementService_GetAvailableColumnFamilies_ReturnsExpectedFamilies()
        {
            // This would require actual Revit document with column families loaded
        }

        [TestMethod]
        public void FamilyManagementService_FindOrCreateSymbol_CreatesNewSymbolWhenNotFound()
        {
            // This would require actual Revit document and family context
        }

        [TestMethod]
        public void FamilyManagementService_ValidateFamilyCompatibility_ValidatesCorrectly()
        {
            // This would require actual Family objects from Revit
        }
        */
    }

    /// <summary>
    /// Integration tests that would run in a Revit environment
    /// These tests require actual Revit API objects and are commented out for unit testing
    /// </summary>
    [TestClass]
    public class FamilyManagementIntegrationTests
    {
        // These tests would be implemented when running in actual Revit context
        // They would test the actual FamilyManagementService with real Revit objects

        [TestMethod]
        [Ignore("Requires Revit context")]
        public void FamilyManagementService_Integration_LoadStandardFamilies()
        {
            // Test loading standard column families from Revit library
            Assert.Inconclusive("Integration test - requires Revit context");
        }

        [TestMethod]
        [Ignore("Requires Revit context")]
        public void FamilyManagementService_Integration_CreateCustomSymbol()
        {
            // Test creating a custom symbol with specific dimensions
            Assert.Inconclusive("Integration test - requires Revit context");
        }

        [TestMethod]
        [Ignore("Requires Revit context")]
        public void FamilyManagementService_Integration_FindExistingSymbol()
        {
            // Test finding existing symbols with matching dimensions
            Assert.Inconclusive("Integration test - requires Revit context");
        }

        [TestMethod]
        [Ignore("Requires Revit context")]
        public void EnhancedColumnByLineCommand_Integration_CreateColumnFromRectangle()
        {
            // Test the complete workflow of creating a column from detail lines
            Assert.Inconclusive("Integration test - requires Revit context");
        }
    }
}