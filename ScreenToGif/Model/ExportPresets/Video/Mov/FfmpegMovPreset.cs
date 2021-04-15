using System;
using System.Collections.Generic;
using ScreenToGif.Interfaces;
using ScreenToGif.Util;

namespace ScreenToGif.Model.ExportPresets.Video.Mov
{
    public class FfmpegMovPreset : MovPreset, IFfmpegPreset
    {
        public FfmpegMovPreset()
        {
            Encoder = EncoderType.FFmpeg;
            ImageId = "Vector.Ffmpeg";
            RequiresFfmpeg = true;

            VideoCodec = VideoCodecs.X264;
            CodecPreset = VideoCodecPresets.Fast;
            HardwareAcceleration = HardwareAcceleration.Auto;
            Pass = 1;
            ConstantRateFactor = 23;
            PixelFormat = VideoPixelFormats.Yuv420p;
            Parameters = "-vsync passthrough \n-hwaccel auto \n{I} \n-c:v libx264 \n-preset fast \n-pix_fmt yuv420p \n-vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2\" \n-crf 23 \n-f mov \n{O}";
        }

        public static List<FfmpegMovPreset> Defaults => new List<FfmpegMovPreset>
        {
            new FfmpegMovPreset
            {
                TitleKey = "S.Preset.Default.Title",
                DescriptionKey = "S.Preset.Default.Description",
                HasAutoSave = true,
                IsSelectedForEncoder = true,
                IsDefault = true,
                CreationDate = new DateTime(2021, 02, 20),

                VideoCodec = VideoCodecs.X264,
                CodecPreset = VideoCodecPresets.Fast,
                HardwareAcceleration = HardwareAcceleration.Auto,
                Pass = 1,
                ConstantRateFactor = 23,
                PixelFormat = VideoPixelFormats.Yuv420p,
                Parameters = "-vsync passthrough \n-hwaccel auto \n{I} \n-c:v libx264 \n-preset fast \n-pix_fmt yuv420p \n-vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2\" \n-crf 23 \n-f mov \n{O}"
            },

            new FfmpegMovPreset
            {
                TitleKey = "S.Preset.Hevc.Title",
                DescriptionKey = "S.Preset.Hevc.Description",
                HasAutoSave = true,
                IsDefault = true,
                CreationDate = new DateTime(2021, 02, 20),

                VideoCodec = VideoCodecs.X265,
                CodecPreset = VideoCodecPresets.Fast,
                HardwareAcceleration = HardwareAcceleration.Auto,
                Pass = 1,
                ConstantRateFactor = 28,
                PixelFormat = VideoPixelFormats.Yuv420p,
                Parameters = "-vsync passthrough \n-hwaccel auto \n{I} \n-c:v libx265 \n-preset fast \n-pix_fmt yuv420p \n-vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2\" \n-crf 28 \n-f mov \n{O}"
            }
        };
    }
}