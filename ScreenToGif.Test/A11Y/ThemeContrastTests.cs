using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using Xunit;

namespace ScreenToGif.Test.A11Y
{
    public class ThemeContrastTests
    {
        private static readonly string ThemeRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ScreenToGif", "Themes", "Colors"));

        private static readonly string[] Themes = ["Dark.xaml", "Light.xaml", "Medium.xaml", "VeryDark.xaml"];

        // Explicit pairs
        private static readonly (string fg, string bg, double minRatio)[] ColorPairs = [
            ("Brush.Button.Paypal.Foreground", "Brush.Button.Paypal.Background", WCAGContrast.LargeAA),
            ("Brush.DataGrid.Header.Foreground", "Brush.DataGrid.Header.Background", WCAGContrast.NormalAA),
        ];

        // Generates contrast test cases based on prefix matching
        private static IEnumerable<(string theme, string fg, string bg, double minRatio)> DynamicColorPairs()
        {
            (string fgPrefix, string bgPrefix, double minRatio)[] dynamicPairs = [
                ("Brush.Hyperlink", "Panel.Background", WCAGContrast.NormalAA),
            ];
            foreach (var theme in Themes)
            {
                var dict = LoadTheme(theme);
                var keys = dict.Keys.OfType<string>().ToList();

                foreach (var (fgPrefix, bgPrefix, minRatio) in dynamicPairs)
                {
                    var fgKeys = keys.Where(k => k.StartsWith(fgPrefix));
                    var bgKeys = keys.Where(k => k.StartsWith(bgPrefix));

                    foreach (string fgKey in fgKeys)
                    {
                        foreach (string bgKey in bgKeys)
                        {
                            yield return (theme, fgKey, bgKey, minRatio);
                        }
                    }
                }
            }
        }
        public static IEnumerable<object[]> ContrastTestCases()
        {
            foreach (var theme in Themes)
            {
                foreach (var (fg, bg, minRatio) in ColorPairs)
                {
                    yield return new object[] { theme, fg, bg, minRatio };
                }
            }

            foreach (var (theme, fg, bg, minRatio) in DynamicColorPairs())
            {
                yield return new object[] { theme, fg, bg, minRatio };
            }
        }

        [Theory]
        [MemberData(nameof(ContrastTestCases))]
        public void ThemeContrastShouldMeetWCAG(string theme, string fgKey, string bgKey, double minRatio)
        {
            var dict = LoadTheme(theme);
            var fg = ((SolidColorBrush)dict[fgKey]).Color;
            var bg = ((SolidColorBrush)dict[bgKey]).Color;

            var contrast = WCAGContrast.CheckContrast(fg, bg);

            Assert.True(contrast >= minRatio, $"[{fgKey}] on [{bgKey}] contrast {contrast:F2} < {minRatio}");
        }

        private static ResourceDictionary LoadTheme(string fileName)
        {
            var filePath = Path.Combine(ThemeRoot, fileName);
            using var stream = File.OpenRead(filePath);
            return (ResourceDictionary)XamlReader.Load(stream);
        }
    }


    public static class WCAGContrast
    {
        public const double NormalAA = 4.5;
        public const double LargeAA = 3.0;

        public const double NormalAAA = 7.0;
        public const double LargeAAA = 4.5;

        public static double CheckContrast(Color fg, Color bg)
        {
            var alpha = fg.A / 255.0;
            var r = (byte)(fg.R * alpha + bg.R * (1 - alpha));
            var g = (byte)(fg.G * alpha + bg.G * (1 - alpha));
            var b = (byte)(fg.B * alpha + bg.B * (1 - alpha));

            var blendedFg = Color.FromRgb(r, g, b);

            var l1 = GetLuminance(blendedFg);
            var l2 = GetLuminance(bg);

            var light = Math.Max(l1, l2);
            var dark = Math.Min(l1, l2);

            return (light + 0.05) / (dark + 0.05);
        }

        private static double GetLuminance(Color c)
        {
            static double ChannelToLinear(byte channel)
            {
                var v = channel / 255.0;
                return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
            }

            return 0.2126 * ChannelToLinear(c.R) + 0.7152 * ChannelToLinear(c.G) + 0.0722 * ChannelToLinear(c.B);
        }
    }
}
