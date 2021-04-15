using System;
using System.Collections.Generic;
using ScreenToGif.Interfaces;
using ScreenToGif.Util;

namespace ScreenToGif.Model.ExportPresets.Video.Webm
{
    public class FfmpegWebmPreset : WebmPreset, IFfmpegPreset
    {
        public FfmpegWebmPreset()
        {
            Encoder = EncoderType.FFmpeg;
            ImageId = "Vector.Ffmpeg";
            RequiresFfmpeg = true;

            VideoCodec = VideoCodecs.Vp9;
            CodecPreset = VideoCodecPresets.Fast;
            HardwareAcceleration = HardwareAcceleration.Auto;
            Pass = 1;
            ConstantRateFactor = 30;
            BitRate = 0;
            BitRateUnit = RateUnit.Megabits;
            PixelFormat = VideoPixelFormats.Yuv420p;
            Parameters = "-vsync passthrough \n-hwaccel auto \n{I} \n-c:v libvpx-vp9 \n-tile-columns 6 -frame-parallel 1 \n-auto-alt-ref 1 -lag-in-frames 25 \n-preset fast \n-pix_fmt yuv420p \n-vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2\" \n-crf 30 \n-b:v 0 \n-f webm \n{O}";
        }

        public static List<FfmpegWebmPreset> Defaults => new List<FfmpegWebmPreset>
        {
            new FfmpegWebmPreset
            {
                TitleKey = "S.Preset.Default.Title",
                DescriptionKey = "S.Preset.Default.Description",
                HasAutoSave = true,
                IsSelectedForEncoder = true,
                IsDefault = true,
                CreationDate = new DateTime(2021, 02, 20),

                VideoCodec = VideoCodecs.Vp9,
                CodecPreset = VideoCodecPresets.Fast,
                HardwareAcceleration = HardwareAcceleration.Auto,
                Pass = 1,
                ConstantRateFactor = 30,
                BitRate = 0,
                BitRateUnit = RateUnit.Megabits,
                PixelFormat = VideoPixelFormats.Yuv420p,
                Parameters = "-vsync passthrough \n-hwaccel auto \n{I} \n-c:v libvpx-vp9 \n-tile-columns 6 -frame-parallel 1 \n-auto-alt-ref 1 -lag-in-frames 25 \n-preset fast \n-pix_fmt yuv420p \n-vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2\" \n-crf 30 \n-b:v 0 \n-f webm \n{O}"
            },

            new FfmpegWebmPreset
            {
                TitleKey = "S.Preset.Vp8.Title",
                DescriptionKey = "S.Preset.Vp8.Description",
                HasAutoSave = true,
                IsSelectedForEncoder = true,
                IsDefault = true,
                CreationDate = new DateTime(2021, 02, 20),

                VideoCodec = VideoCodecs.Vp8,
                CodecPreset = VideoCodecPresets.Fast,
                HardwareAcceleration = HardwareAcceleration.Auto,
                Pass = 1,
                ConstantRateFactor = 30,
                BitRate = 0,
                BitRateUnit = RateUnit.Megabits,
                PixelFormat = VideoPixelFormats.Yuv420p,
                Parameters = "-vsync passthrough \n-hwaccel auto \n{I} \n-c:v libvpx \n-tile-columns 6 -frame-parallel 1 \n-auto-alt-ref 1 -lag-in-frames 25 \n-preset fast \n-pix_fmt yuv420p \n-vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2\" \n-crf 30 \n-b:v 0 \n-f webm \n{O}"
            }
        };
    }
}