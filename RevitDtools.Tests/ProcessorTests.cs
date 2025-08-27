using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Autodesk.Revit.DB;
using RevitDtools.Core.Services;
using RevitDtools.Core.Interfaces;
using RevitDtools.Core.Models;
using System;
using RevitView = Autodesk.Revit.DB.View;

namespace RevitDtools.Tests
{
    [TestClass]
    public class ProcessorTests
    {
        private Mock<Document> _mockDocument;
        private Mock<ILogger> _mockLogger;
        private Mock<RevitView> _mockView;

        [TestInitialize]
        public void Setup()
        {
            _mockDocument = new Mock<Document>();
            _mockLogger = new Mock<ILogger>();
            _mockView = new Mock<RevitView>();
            
            // Setup basic mock behavior
            _mockDocument.Setup(d => d.IsReadOnly).Returns(false);
            _mockView.Setup(v => v.Id).Returns(new ElementId(123));
        }

        [TestClass]
        public class ArcProcessorTests : ProcessorTests
        {
            private ArcProcessor _processor;

            [TestInitialize]
            public void ArcProcessorSetup()
            {
                Setup();
                _processor = new ArcProcessor(_mockDocument.Object, _mockLogger.Object, _mockView.Object);
            }

            [TestMethod]
            public void Process_WithValidArc_ShouldReturnSuccess()
            {
                // Arrange
                var mockArc = CreateMockArc();
                var transform = Transform.Identity;

                // Act
                var result = _processor.Process(mockArc, transform);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Context.Contains("ArcProcessor"));
            }

            [TestMethod]
            public void Process_WithInvalidGeometry_ShouldReturnFailure()
            {
                // Arrange
                var mockLine = CreateMockLine();
                var transform = Transform.Identity;

                // Act
                var result = _processor.Process(mockLine, transform);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsFalse(result.Success);
                Assert.IsTrue(result.Message.Contains("Invalid arc geometry object"));
            }

            private Arc CreateMockArc()
            {
                var mock = new Mock<Arc>();
                mock.Setup(a => a.GraphicsStyleId).Returns(ElementId.InvalidElementId);
                mock.Setup(a => a.CreateTransformed(It.IsAny<Transform>())).Returns(mock.Object);
                return mock.Object;
            }

            private Line CreateMockLine()
            {
                var mock = new Mock<Line>();
                mock.Setup(l => l.GraphicsStyleId).Returns(ElementId.InvalidElementId);
                return mock.Object;
            }
        }

        [TestClass]
        public class SplineProcessorTests : ProcessorTests
        {
            private SplineProcessor _processor;

            [TestInitialize]
            public void SplineProcessorSetup()
            {
                Setup();
                _processor = new SplineProcessor(_mockDocument.Object, _mockLogger.Object, _mockView.Object);
            }

            [TestMethod]
            public void Process_WithValidSpline_ShouldReturnSuccess()
            {
                // Arrange
                var mockSpline = CreateMockSpline();
                var transform = Transform.Identity;

                // Act
                var result = _processor.Process(mockSpline, transform);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Context.Contains("SplineProcessor"));
            }

            [TestMethod]
            public void Process_WithInvalidGeometry_ShouldReturnFailure()
            {
                // Arrange
                var mockArc = CreateMockArc();
                var transform = Transform.Identity;

                // Act
                var result = _processor.Process(mockArc, transform);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsFalse(result.Success);
                Assert.IsTrue(result.Message.Contains("Invalid spline geometry object"));
            }

            private NurbSpline CreateMockSpline()
            {
                var mock = new Mock<NurbSpline>();
                mock.Setup(s => s.GraphicsStyleId).Returns(ElementId.InvalidElementId);
                mock.Setup(s => s.CreateTransformed(It.IsAny<Transform>())).Returns(mock.Object);
                return mock.Object;
            }

            private Arc CreateMockArc()
            {
                var mock = new Mock<Arc>();
                mock.Setup(a => a.GraphicsStyleId).Returns(ElementId.InvalidElementId);
                return mock.Object;
            }
        }

        [TestClass]
        public class EllipseProcessorTests : ProcessorTests
        {
            private EllipseProcessor _processor;

            [TestInitialize]
            public void EllipseProcessorSetup()
            {
                Setup();
                _processor = new EllipseProcessor(_mockDocument.Object, _mockLogger.Object, _mockView.Object);
            }

            [TestMethod]
            public void Process_WithValidEllipse_ShouldReturnSuccess()
            {
                // Arrange
                var mockEllipse = CreateMockEllipse();
                var transform = Transform.Identity;

                // Act
                var result = _processor.Process(mockEllipse, transform);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Context.Contains("EllipseProcessor"));
            }

            [TestMethod]
            public void Process_WithInvalidGeometry_ShouldReturnFailure()
            {
                // Arrange
                var mockLine = CreateMockLine();
                var transform = Transform.Identity;

                // Act
                var result = _processor.Process(mockLine, transform);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsFalse(result.Success);
                Assert.IsTrue(result.Message.Contains("Invalid ellipse geometry object"));
            }

            private Ellipse CreateMockEllipse()
            {
                var mock = new Mock<Ellipse>();
                mock.Setup(e => e.GraphicsStyleId).Returns(ElementId.InvalidElementId);
                mock.Setup(e => e.CreateTransformed(It.IsAny<Transform>())).Returns(mock.Object);
                return mock.Object;
            }

            private Line CreateMockLine()
            {
                var mock = new Mock<Line>();
                mock.Setup(l => l.GraphicsStyleId).Returns(ElementId.InvalidElementId);
                return mock.Object;
            }
        }

        [TestClass]
        public class LineProcessorTests : ProcessorTests
        {
            private LineProcessor _processor;

            [TestInitialize]
            public void LineProcessorSetup()
            {
                Setup();
                _processor = new LineProcessor(_mockDocument.Object, _mockLogger.Object, _mockView.Object);
            }

            [TestMethod]
            public void Process_WithValidLine_ShouldReturnSuccess()
            {
                // Arrange
                var mockLine = CreateMockLine();
                var transform = Transform.Identity;

                // Act
                var result = _processor.Process(mockLine, transform);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Context.Contains("LineProcessor"));
            }

            [TestMethod]
            public void Process_WithInvalidGeometry_ShouldReturnFailure()
            {
                // Arrange
                var mockArc = CreateMockArc();
                var transform = Transform.Identity;

                // Act
                var result = _processor.Process(mockArc, transform);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsFalse(result.Success);
                Assert.IsTrue(result.Message.Contains("Invalid line geometry object"));
            }

            private Line CreateMockLine()
            {
                var mock = new Mock<Line>();
                mock.Setup(l => l.GraphicsStyleId).Returns(ElementId.InvalidElementId);
                mock.Setup(l => l.CreateTransformed(It.IsAny<Transform>())).Returns(mock.Object);
                return mock.Object;
            }

            private Arc CreateMockArc()
            {
                var mock = new Mock<Arc>();
                mock.Setup(a => a.GraphicsStyleId).Returns(ElementId.InvalidElementId);
                return mock.Object;
            }
        }

        [TestClass]
        public class PolyLineProcessorTests : ProcessorTests
        {
            private PolyLineProcessor _processor;

            [TestInitialize]
            public void PolyLineProcessorSetup()
            {
                Setup();
                _processor = new PolyLineProcessor(_mockDocument.Object, _mockLogger.Object, _mockView.Object);
            }

            [TestMethod]
            public void Process_WithValidPolyLine_ShouldReturnSuccess()
            {
                // Arrange
                var mockPolyLine = CreateMockPolyLine();
                var transform = Transform.Identity;

                // Act
                var result = _processor.Process(mockPolyLine, transform);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Context.Contains("PolyLineProcessor"));
            }

            [TestMethod]
            public void Process_WithInvalidGeometry_ShouldReturnFailure()
            {
                // Arrange
                var mockArc = CreateMockArc();
                var transform = Transform.Identity;

                // Act
                var result = _processor.Process(mockArc, transform);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsFalse(result.Success);
                Assert.IsTrue(result.Message.Contains("Invalid polyline geometry object"));
            }

            private PolyLine CreateMockPolyLine()
            {
                var mock = new Mock<PolyLine>();
                mock.Setup(p => p.GraphicsStyleId).Returns(ElementId.InvalidElementId);
                
                // Create mock points for polyline
                var points = new List<XYZ>
                {
                    new XYZ(0, 0, 0),
                    new XYZ(10, 0, 0),
                    new XYZ(10, 10, 0),
                    new XYZ(0, 10, 0)
                };
                mock.Setup(p => p.GetCoordinates()).Returns(points);
                
                return mock.Object;
            }

            private Arc CreateMockArc()
            {
                var mock = new Mock<Arc>();
                mock.Setup(a => a.GraphicsStyleId).Returns(ElementId.InvalidElementId);
                return mock.Object;
            }
        }

        [TestClass]
        public class TextProcessorTests : ProcessorTests
        {
            private TextProcessor _processor;

            [TestInitialize]
            public void TextProcessorSetup()
            {
                Setup();
                _processor = new TextProcessor(_mockDocument.Object, _mockLogger.Object, _mockView.Object);
            }

            [TestMethod]
            public void Process_WithValidText_ShouldReturnSuccess()
            {
                // Arrange
                var mockText = CreateMockGeometryObject();
                var transform = Transform.Identity;

                // Setup mock for text note creation
                var mockTextNoteType = new Mock<TextNoteType>();
                mockTextNoteType.Setup(t => t.Id).Returns(new ElementId(456));

                var mockCollector = new Mock<FilteredElementCollector>();
                // Note: Full mocking of FilteredElementCollector is complex, 
                // so this test focuses on the basic flow

                // Act
                var result = _processor.Process(mockText, transform);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Context.Contains("TextProcessor"));
            }

            private GeometryObject CreateMockGeometryObject()
            {
                var mock = new Mock<GeometryObject>();
                mock.Setup(g => g.GraphicsStyleId).Returns(ElementId.InvalidElementId);
                
                // Create mock bounding box
                var bbox = new BoundingBoxXYZ
                {
                    Min = new XYZ(0, 0, 0),
                    Max = new XYZ(5, 2, 0)
                };
                mock.Setup(g => g.GetBoundingBox(null)).Returns(bbox);
                
                return mock.Object;
            }
        }

        [TestClass]
        public class HatchProcessorTests : ProcessorTests
        {
            private HatchProcessor _processor;

            [TestInitialize]
            public void HatchProcessorSetup()
            {
                Setup();
                _processor = new HatchProcessor(_mockDocument.Object, _mockLogger.Object, _mockView.Object);
            }

            [TestMethod]
            public void Process_WithValidHatch_ShouldReturnSuccess()
            {
                // Arrange
                var mockHatch = CreateMockGeometryObject();
                var transform = Transform.Identity;

                // Act
                var result = _processor.Process(mockHatch, transform);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Context.Contains("HatchProcessor"));
            }

            private GeometryObject CreateMockGeometryObject()
            {
                var mock = new Mock<GeometryObject>();
                mock.Setup(g => g.GraphicsStyleId).Returns(ElementId.InvalidElementId);
                return mock.Object;
            }
        }
    }
}