using System;
using System.Collections.Generic;
using ScreenToGif.Interfaces;
using ScreenToGif.Util;

namespace ScreenToGif.Model.ExportPresets.Video.Mp4
{
    public class FfmpegMp4Preset : Mp4Preset, IFfmpegPreset
    {
        public FfmpegMp4Preset()
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
            Parameters = "-vsync passthrough \n-hwaccel auto \n{I} \n-c:v libx264 \n-preset fast \n-pix_fmt yuv420p \n-vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2\" \n-crf 23 \n-f mp4 \n{O}";
        }

        public static List<FfmpegMp4Preset> Defaults => new List<FfmpegMp4Preset>
        {
            new FfmpegMp4Preset
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
                Parameters = "-vsync passthrough \n-hwaccel auto \n{I} \n-c:v libx264 \n-preset fast \n-pix_fmt yuv420p \n-vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2\" \n-crf 23 \n-f mp4 \n{O}"
            },

            new FfmpegMp4Preset
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
                Parameters = "-vsync passthrough \n-hwaccel auto \n{I} \n-c:v libx265 \n-preset fast \n-pix_fmt yuv420p \n-vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2\" \n-crf 28 \n-f mp4 \n{O}"
            },
        };
    }
}