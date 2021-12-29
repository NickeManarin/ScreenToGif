using ScreenToGif.Model;
using Xunit;

namespace ScreenToGif.Test.Fact
{
    public class ImageComparison
    {
        [Fact]
        public void CanCalculateDifference()
        {
            var f1 = new FrameInfo()
            {
                Path = "./Data/b1.bmp",
                Index = 0
            };

            var f2 = new FrameInfo()
            {
                Path = "./Data/b2.bmp",
                Index = 1
            };

            var diff = ImageUtil.ImageMethods.CalculateDifference(f1, f2);

            Assert.Equal(25, diff);
        }
    }
}