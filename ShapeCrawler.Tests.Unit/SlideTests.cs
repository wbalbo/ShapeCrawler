using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using FluentAssertions;
using ShapeCrawler.Drawing;
using ShapeCrawler.Shapes;
using ShapeCrawler.Tests.Unit.Helpers;
using Xunit;

// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable TooManyChainedReferences
// ReSharper disable TooManyDeclarations

namespace ShapeCrawler.Tests.Unit
{
    [SuppressMessage("ReSharper", "SuggestVarOrType_SimpleTypes")]
    public class SlideTests : IClassFixture<PresentationFixture>
    {
        private readonly PresentationFixture _fixture;

        public SlideTests(PresentationFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void Hide_MethodHidesSlide_WhenItIsExecuted()
        {
            // Arrange
            var pre = SCPresentation.Open(Properties.Resources._001, true);
            var slide = pre.Slides.First();

            // Act
            slide.Hide();

            // Assert
            slide.Hidden.Should().Be(true);
        }

        [Fact]
        public void Hidden_GetterReturnsTrue_WhenTheSlideIsHidden()
        { 
            // Arrange
            ISlide slideEx = _fixture.Pre002.Slides[2];

            // Act
            bool hidden = slideEx.Hidden;

            // Assert
            hidden.Should().BeTrue();
        }


        [Fact]
        public void SaveScheme_CreatesAndSavesSlideSchemeImageInSpecifiedStream()
        {
            // Arrange
            ISlide slide = _fixture.Pre025.Slides[2];
            var stream = new MemoryStream();

            // Act
            slide.SaveScheme(stream);

            // Assert
            stream.Length.Should().BeGreaterThan(0);
        }

        [Fact]
        public async void BackgroundSetImage_ChangesBackground_WhenImageStreamIsPassed()
        {
            // Arrange
            var pre = SCPresentation.Open(Properties.Resources._009, true);
            var backgroundImage = pre.Slides[0].Background;
            var imgStream = new MemoryStream(Properties.Resources.test_image_2);
            var bytesBefore = await backgroundImage.GetBytes().ConfigureAwait(false);

            // Act
            backgroundImage.SetImage(imgStream);
            backgroundImage.SetImage(imgStream);

            // Assert
            var bytesAfter = await backgroundImage.GetBytes().ConfigureAwait(false);
            bytesAfter.Length.Should().NotBe(bytesBefore.Length);
        }

        [Fact]
        public void Background_ImageIsNull_WhenTheSlideHasNotBackground()
        {
            // Arrange
            ISlide slide = _fixture.Pre009.Slides[1];

            // Act
            SCImage backgroundImage = slide.Background;

            // Assert
            backgroundImage.Should().BeNull();
        }

        [Fact]
        public void CustomData_ReturnsData_WhenCustomDataWasAssigned()
        {
            // Arrange
            const string customDataString = "Test custom data";
            var originPre = SCPresentation.Open(Properties.Resources._001, true);
            var slide = originPre.Slides.First();

            // Act
            slide.CustomData = customDataString;

            var savedPreStream = new MemoryStream();
            originPre.SaveAs(savedPreStream);
            var savedPre = SCPresentation.Open(savedPreStream, false);
            var customData = savedPre.Slides.First().CustomData;

            // Assert
            customData.Should().Be(customDataString);
        }

        [Fact]
        public void Shapes_ContainsParticularShapeTypes()
        {
            // Arrange
            var pre = _fixture.Pre003;

            // Act
            var shapes = pre.Slides.First().Shapes;

            // Assert
            Assert.Single(shapes.Where(sp => sp is IAutoShape));
            Assert.Single(shapes.Where(sp => sp is IPicture));
            Assert.Single(shapes.Where(sp => sp is ITable));
            Assert.Single(shapes.Where(sp => sp is IChart));
            Assert.Single(shapes.Where(sp => sp is IGroupShape));
        }

        [Theory]
        [MemberData(nameof(TestCasesShapesCount))]
        public void ShapesCount_ReturnsNumberOfShapesOnTheSlide(ISlide slide, int expectedShapesCount)
        {
            // Act
            int shapesCount = slide.Shapes.Count;

            // Assert
            shapesCount.Should().Be(expectedShapesCount);
        }

        public static IEnumerable<object[]> TestCasesShapesCount()
        {
            SCPresentation presentation = SCPresentation.Open(Properties.Resources._009, false);
            
            ISlide slide = presentation.Slides[0];
            yield return new object[] { slide, 6 };
            
            slide = presentation.Slides[1];
            yield return new object[] { slide, 6 };
            
            slide = SCPresentation.Open(Properties.Resources._002, false).Slides[0];
            yield return new object[] { slide, 4 };
            
            slide = SCPresentation.Open(Properties.Resources._003, false).Slides[0];
            yield return new object[] { slide, 5 };
            
            slide = SCPresentation.Open(Properties.Resources._013, false).Slides[0];
            yield return new object[] { slide, 4 };
            
            slide = SCPresentation.Open(Properties.Resources._023, false).Slides[0];
            yield return new object[] { slide, 1 };

            slide = SCPresentation.Open(Properties.Resources._014, false).Slides[2];
            yield return new object[] { slide, 5 };
        }

        [Fact]
        public void CustomData_PropertyIsNull_WhenTheSlideHasNotCustomData()
        {
            // Arrange
            var slide = _fixture.Pre001.Slides.First();

            // Act
            var sldCustomData = slide.CustomData;

            // Assert
            sldCustomData.Should().BeNull();
        }

        [Fact]
        public void NumberSetter_MovesSlide()
        {
            // Arrange
            Stream preStream = TestFiles.Presentations.pre001_stream;
            IPresentation presentation = SCPresentation.Open(preStream, true);
            ISlide slide1 = presentation.Slides[0];
            slide1.CustomData = "old-number-1";
            ISlide slide2 = presentation.Slides[1];

            // Act
            slide1.Number = 2;

            // Assert
            slide1.Number.Should().Be(2);
            slide2.Number.Should().Be(1, "because the first slide was inserted to its position.");
            
            presentation.Close();
            presentation = SCPresentation.Open(preStream, false);
            slide2 = presentation.Slides.First(s => s.CustomData == "old-number-1");
            slide2.Number.Should().Be(2);
        }

        [Fact]
        public void Shape_IsAPicture()
        {
            // Arrange
            IShape shape = _fixture.Pre009.Slides[1].Shapes.First(sp => sp.Id == 3);

            // Act-Assert
            IPicture picture = shape as IPicture;
            picture.Should().NotBeNull();
        }

#if DEBUG
        [Fact(Skip = "The feature is in progress")]
        public void SaveImage_GenerateAndSavesSlideImageInSpecifiedFilePath()
        {
            // Arrange
            ISlide slide = _fixture.Pre001.Slides[0];

            // Act
            slide.SaveImage(@"c:\1\SlideScSaveImage.png");
        }
#endif
    }
}
