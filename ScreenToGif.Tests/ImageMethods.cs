using System;
using System.Collections.Generic;
using System.Drawing;

using ScreenToGif.Model;

using Xunit;

namespace ScreenToGif.Tests
{
    public class ImageMethods
    {
        [Fact]
        public void CanCalculateDifference()
        {
            var f1 = new FrameInfo()
            {
                Path = "./TestData/b1.bmp",
                Index = 0
            };

            var f2 = new FrameInfo()
            {
                Path = "./TestData/b2.bmp",
                Index = 1
            };

            var diff = ImageUtil.ImageMethods.CalculateDifference(f1, f2);

            Assert.Equal(25, diff);
        }
    }
}
