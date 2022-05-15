using Color = System.Windows.Media.Color;

namespace ScreenToGif.Util.Codification.Gif.Encoder.Quantization;

public class MostUsedQuantizer : PaletteQuantizer
{
    internal override void FirstPass(byte[] pixels)
    {
        //Pixels are in BGRA.
        Colors = new List<Color>();

        for (var index = 0; index < pixels.Length; index += 4)
        {
            //Transparent colors are ignored.
            if (pixels[index + 3] == 0)
                continue;

            Colors.Add(new Color
            {
                B = pixels[index],
                G = pixels[index + 1],
                R = pixels[index + 2]
            });
        }
    }

    internal override List<Color> BuildPalette()
    {
        MaxColorsWithTransparency = TransparentColor.HasValue ? MaxColors - 1 : MaxColors;

        var colorTable = Colors.AsParallel().GroupBy(x => x) //Grouping based on its value.
            .OrderByDescending(g => g.Count()) //Order by most frequent values.
            .Select(g => g.FirstOrDefault()) //Take the first among the group.
            .Take(MaxColorsWithTransparency).ToList(); //Take all the colors needed.

        if (TransparentColor.HasValue)
            colorTable.Add(TransparentColor.Value);

        return colorTable;
    }
}