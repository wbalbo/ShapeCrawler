﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using ShapeCrawler.Drawing;
using ShapeCrawler.OLEObjects;
using ShapeCrawler.Shapes;
using ShapeCrawler.Tests.Unit.Helpers;
using ShapeCrawler.Tests.Unit.Properties;
using Xunit;

// ReSharper disable TooManyDeclarations
// ReSharper disable InconsistentNaming
// ReSharper disable TooManyChainedReferences

namespace ShapeCrawler.Tests.Unit
{
    public class ShapeTests : IClassFixture<PresentationFixture>
    {
        private readonly PresentationFixture _fixture;

        public ShapeTests(PresentationFixture fixture)
        {
            _fixture = fixture;
        }

        [Theory]
        [MemberData(nameof(TestCasesPlaceholderType))]
        public void PlaceholderType_GetterReturnsPlaceholderTypeOfTheShape(IShape shape, PlaceholderType expectedType)
        {
            // Act
            PlaceholderType actualType = shape.Placeholder.Type;

            // Assert
            actualType.Should().Be(expectedType);
        }

        public static IEnumerable<object[]> TestCasesPlaceholderType()
        {
            IShape shape = SCPresentation.Open(Resources._021, false).Slides[3].Shapes.First(sp => sp.Id == 2);
            yield return new object[] { shape, PlaceholderType.Footer };

            shape = SCPresentation.Open(Resources._008, false).Slides[0].Shapes.First(sp => sp.Id == 3);
            yield return new object[] { shape, PlaceholderType.DateAndTime };

            shape = SCPresentation.Open(Resources._019, false).Slides[0].Shapes.First(sp => sp.Id == 2);
            yield return new object[] { shape, PlaceholderType.SlideNumber };

            shape = SCPresentation.Open(Resources._013, false).Slides[0].Shapes.First(sp => sp.Id == 281);
            yield return new object[] { shape, PlaceholderType.Custom };
        }

        [Fact]
        public void AutoShapeFill_ReturnsNull_WhenAutoShapeIsNotFilled()
        {
            // Arrange
            IAutoShape autoShape = (IAutoShape)_fixture.Pre009.Slides[1].Shapes.First(sp => sp.Id == 6);

            // Act
            ShapeFill shapeFill = autoShape.Fill;

            // Assert
            shapeFill.Should().BeNull();
        }

        [Fact]
        public void AutoShapeFill_IsNotNull_WhenAutoShapeIsFilled()
        {
            // Arrange
            IAutoShape autoShape = (IAutoShape)_fixture.Pre021.Slides[0].Shapes.First(sp => sp.Id == 108);

            // Act-Assert
            autoShape.Fill.Should().NotBeNull();
        }

        [Fact]
        public void AutoShapeFillType_GetterReturnsFillTypeByWhichTheAutoShapeIsFilled()
        {
            // Arrange
            IAutoShape autoShape1 = (IAutoShape)_fixture.Pre009.Slides[2].Shapes.First(sp => sp.Id == 4);
            IAutoShape autoShape2 = (IAutoShape)_fixture.Pre009.Slides[1].Shapes.First(sp => sp.Id == 2);

            // Act
            FillType shapeFillTypeCase1 = autoShape1.Fill.Type;
            FillType shapeFillTypeCase2 = autoShape2.Fill.Type;

            // Assert
            shapeFillTypeCase1.Should().Be(FillType.Picture);
            shapeFillTypeCase2.Should().Be(FillType.Solid);
        }

        [Fact]
        public void AutoShapeFillSolidColorName_GetterReturnsSolidColorNameByWhichTheAutoShapeIsFilled()
        {
            // Arrange
            IAutoShape autoShape = (IAutoShape)_fixture.Pre009.Slides[1].Shapes.First(sp => sp.Id == 2);

            // Act
            var shapeSolidColorName = autoShape.Fill.SolidColor.Name;

            // Assert
            shapeSolidColorName.Should().BeEquivalentTo("ff0000");
        }


        [Fact]
        public async void AutoShapeFillPictureGetImageBytes_ReturnsImageByWhichTheAutoShapeIsFilled()
        {
            // Arrange
            IAutoShape shape = (IAutoShape)_fixture.Pre009.Slides[2].Shapes.First(sp => sp.Id == 4);

            // Act
            byte[] imageBytes = await shape.Fill.Picture.GetBytes().ConfigureAwait(false);

            // Assert
            imageBytes.Length.Should().BePositive();
        }

        [Fact]
        public void AutoShapeFillPictureSetImage_ChangesPicture()
        {
            // Arrange
            IPresentation presentation = SCPresentation.Open(TestFiles.Presentations.pre009, true);
            IAutoShape autoShape = (IAutoShape)presentation.Slides[2].Shapes.First(sp => sp.Id == 4);
            MemoryStream newImage = TestFiles.Images.img02_stream;
            int imageSizeBefore = autoShape.Fill.Picture.GetBytes().GetAwaiter().GetResult().Length;

            // Act
            autoShape.Fill.Picture.SetImage(newImage);

            // Assert
            int imageSizeAfter = autoShape.Fill.Picture.GetBytes().GetAwaiter().GetResult().Length;
            imageSizeAfter.Should().NotBe(imageSizeBefore, "because image has been changed");
        }

        [Fact]
        public void PictureSetImage_ShouldNotImpactOtherPictureImage_WhenItsOriginImageIsShared()
        {
            // Arrange
            IPresentation presentation = SCPresentation.Open(TestFiles.Presentations.pre009, true);
            IPicture picture5 = (IPicture)presentation.Slides[3].Shapes.First(sp => sp.Id == 5);
            IPicture picture6 = (IPicture)presentation.Slides[3].Shapes.First(sp => sp.Id == 6);
            int pic6LengthBefore = picture6.Image.GetBytes().GetAwaiter().GetResult().Length;
            MemoryStream modifiedPresentation = new();

            // Act
            picture5.Image.SetImage(TestFiles.Images.img02);

            // Assert
            int pic6LengthAfter = picture6.Image.GetBytes().GetAwaiter().GetResult().Length;
            pic6LengthAfter.Should().Be(pic6LengthBefore);

            presentation.SaveAs(modifiedPresentation);
            presentation = SCPresentation.Open(modifiedPresentation, false);
            picture6 = (IPicture)presentation.Slides[3].Shapes.First(sp => sp.Id == 6);
            pic6LengthBefore = picture6.Image.GetBytes().GetAwaiter().GetResult().Length;
            pic6LengthAfter.Should().Be(pic6LengthBefore);
        }

        [Fact]
        public void XAndY_ReturnXAndYAxesShapeCoordinatesOnTheSlide()
        {
            // Arrange
            IShape shapeExCase1 = _fixture.Pre021.Slides[3].Shapes.First(sp => sp.Id == 2);
            IShape shapeExCase2 = _fixture.Pre008.Slides[0].Shapes.First(sp => sp.Id == 3);
            IShape shapeExCase3 = _fixture.Pre006.Slides[0].Shapes.First(sp => sp.Id == 2);
            IGroupShape groupShape = (IGroupShape)_fixture.Pre009.Slides[1].Shapes.First(sp => sp.Id == 7);
            IShape shapeExCase4 = groupShape.Shapes.First(sp => sp.Id.Equals(5));
            IShape shapeExCase5 = _fixture.Pre018.Slides[0].Shapes.First(sp => sp.Id == 7);
            IShape shapeExCase6 = _fixture.Pre009.Slides[1].Shapes.First(sp => sp.Id == 9);
            IShape shapeExCase7 = _fixture.Pre025.Slides[2].Shapes.First(sp => sp.Id == 7);

            // Act
            long xCoordinateCase1 = shapeExCase1.X;
            long xCoordinateCase2 = shapeExCase2.X;
            long xCoordinateCase3 = shapeExCase3.X;
            long xCoordinateCase4 = shapeExCase4.X;
            long xCoordinateCase6 = shapeExCase6.X;
            long xCoordinateCase7 = shapeExCase7.X;
            long yCoordinateCase3 = shapeExCase3.Y;
            long yCoordinateCase5 = shapeExCase5.Y;
            long yCoordinateCase6 = shapeExCase6.Y;

            // Assert
            xCoordinateCase1.Should().Be(3653579);
            xCoordinateCase2.Should().Be(628650);
            xCoordinateCase3.Should().Be(1524000);
            xCoordinateCase4.Should().Be(1581846);
            xCoordinateCase6.Should().Be(699323);
            xCoordinateCase7.Should().Be(757383);
            yCoordinateCase3.Should().Be(1122363);
            yCoordinateCase5.Should().Be(4);
            yCoordinateCase6.Should().Be(3463288);
        }

        [Fact]
        public void XAndWidth_SettersSetXAndWidthOfTheShape()
        {
            // Arrange
            var presentation = SCPresentation.Open(Resources._006_1_slides, true);
            var shape = presentation.Slides.First().Shapes.First(sp => sp.Id == 3);
            var stream = new MemoryStream();
            const int newX = 4000000;
            const int newWidth = 6000000;

            // Act
            shape.X = newX;
            shape.Width = newWidth;
            presentation.SaveAs(stream);

            // Assert
            presentation = SCPresentation.Open(stream, false);
            shape = presentation.Slides.First().Shapes.First(sp => sp.Id == 3);
            shape.X.Should().Be(newX);
            shape.Width.Should().Be(newWidth);
        }

        [Fact]
        public void WidthAndHeight_ReturnWidthAndHeightSizesOfTheShape()
        {
            // Arrange
            IShape shapeCase1 = _fixture.Pre006.Slides[0].Shapes.First(sp => sp.Id == 2);
            IGroupShape groupShape = (IGroupShape) _fixture.Pre009.Slides[1].Shapes.First(sp => sp.Id == 7);
            IShape shapeCase2 = groupShape.Shapes.First(sp => sp.Id == 5);
            IShape shapeCase3 = _fixture.Pre009.Slides[1].Shapes.First(sp => sp.Id == 9);

            // Act
            long shapeWidthCase1 = shapeCase1.Width;
            long shapeWidthCase2 = shapeCase2.Width;
            long shapeWidthCase3 = shapeCase3.Width;
            long shapeHeightCase1 = shapeCase1.Height;
            long shapeHeightCase2 = shapeCase2.Height;
            long shapeHeightCase3 = shapeCase3.Height;

            // Assert
            shapeWidthCase1.Should().Be(9144000);
            shapeWidthCase2.Should().Be(1181377);
            shapeWidthCase3.Should().Be(485775);
            shapeHeightCase1.Should().Be(1425528);
            shapeHeightCase2.Should().Be(654096);
            shapeHeightCase3.Should().Be(373062);
        }

        [Theory]
        [MemberData(nameof(GeometryTypeTestCases))]
        public void GeometryType_ReturnsShapeGeometryType(IShape shape, GeometryType expectedGeometryType)
        {
            // Assert
            shape.GeometryType.Should().BeEquivalentTo(expectedGeometryType);
        }

        public static IEnumerable<object[]> GeometryTypeTestCases()
        {
            var pre021 = SCPresentation.Open(Resources._021, false);
            var shapes = pre021.Slides[3].Shapes;
            var shapeCase1 = shapes.First(sp => sp.Id == 2);
            var shapeCase2 = shapes.First(sp => sp.Id == 3);

            yield return new object[] { shapeCase1, GeometryType.Rectangle };
            yield return new object[] { shapeCase2, GeometryType.Ellipse };
        }

        [Fact]
        public void Shape_IsOLEObject()
        {
            // Arrange
            IOLEObject oleObject = _fixture.Pre009.Slides[1].Shapes.First(sp => sp.Id == 8) as IOLEObject;

            // Act-Assert
            oleObject.Should().NotBeNull();
        }

        [Fact]
        public void Shape_IsNotGroupShape()
        {
            // Arrange
            IShape shape = _fixture.Pre006.Slides[0].Shapes.First(x => x.Id == 3);

            // Act-Assert
            shape.Should().NotBeOfType<IGroupShape>();
        }

        [Fact]
        public void Shape_IsNotAutoShape()
        {
            // Arrange
            IShape shapeCase1 = _fixture.Pre009.Slides[4].Shapes.First(sp => sp.Id == 5);
            IShape shapeCase2 = _fixture.Pre011.Slides[0].Shapes.First(sp => sp.Id == 4);

            // Act-Assert
            shapeCase1.Should().NotBeOfType<IAutoShape>();
            shapeCase2.Should().NotBeOfType<IAutoShape>();
        }

        [Fact]
        public void CustomData_ReturnsNull_WhenShapeHasNotCustomData()
        {
            // Arrange
            var shape = _fixture.Pre009.Slides.First().Shapes.First();

            // Act
            var shapeCustomData = shape.CustomData;

            // Assert
            shapeCustomData.Should().BeNull();
        }

        [Fact]
        public void CustomData_ReturnsCustomDataOfTheShape_WhenShapeWasAssignedSomeCustomData()
        {
            // Arrange
            const string customDataString = "Test custom data";
            var savedPreStream = new MemoryStream();
            var presentation = SCPresentation.Open(Resources._009, true);
            var shape = presentation.Slides.First().Shapes.First();

            // Act
            shape.CustomData = customDataString;
            presentation.SaveAs(savedPreStream);

            // Assert
            presentation = SCPresentation.Open(savedPreStream, false);
            shape = presentation.Slides.First().Shapes.First();
            shape.CustomData.Should().Be(customDataString);
        }

        [Fact]
        public void Name_ReturnsShapeNameString()
        {
            // Arrange
            IShape shape = _fixture.Pre009.Slides[1].Shapes.First(sp => sp.Id == 8);

            // Act
            string shapeName = shape.Name;

            // Assert
            shapeName.Should().BeEquivalentTo("Object 2");
        }

        [Fact]
        public void Hidden_ReturnsValueIndicatingWhetherShapeIsHiddenFromTheSlide()
        {
            // Arrange
            IShape shapeCase1 = _fixture.Pre004.Slides[0].Shapes[0];
            IShape shapeCase2 = _fixture.Pre004.Slides[0].Shapes[1];

            // Act-Assert
            shapeCase1.Hidden.Should().BeTrue();
            shapeCase2.Hidden.Should().BeFalse();
        }
    }
}
