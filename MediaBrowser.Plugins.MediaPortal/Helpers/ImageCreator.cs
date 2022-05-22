using System;
using System.Diagnostics;
using System.IO;

namespace MediaBrowser.Plugins.MediaPortal.Helpers
{
    public class ImageCreator
    {
        public void CreateLogoImage(string inputImage, string outputImage)
        {
            if (!File.Exists(outputImage))
            {
                var inputSize = Plugin.ImageProcessor.GetImageSize(inputImage);

                if (inputSize.Width > 0 && inputSize.Height > 0)
                {
                    var outputScale = @"s=500x500";

                    var inputScale = string.Empty;

                    if (inputSize.Width < inputSize.Height)
                        inputScale = @"scale=-1:490";

                    if (inputSize.Height < inputSize.Width)
                        inputScale = @"scale=490:-1";

                    FFmpeg(inputImage, inputScale, outputScale, outputImage);
                }
            }
        }

        public void CreateLandscapeImage(string inputImage, string outputImage)
        {
            if (!File.Exists(outputImage))
            {
                var inputSize = Plugin.ImageProcessor.GetImageSize(inputImage);

                if (inputSize.Width > 0 && inputSize.Height > 0)
                {
                    float scaleWidth = 990 / (float)inputSize.Width;
                    float scaleHeight = 552 / (float)inputSize.Height;
                    float scaleFactor = Math.Min(scaleWidth, scaleHeight);

                    int newWidth = (int)(inputSize.Width * scaleFactor);
                    int newHeight = (int)(inputSize.Height * scaleFactor);

                    var outputScale = @"s=1000x562";

                    var inputScale = string.Empty;

                    if (newWidth < newHeight)
                        inputScale = string.Format(@"scale=-1:{0}", newHeight);

                    if (newHeight < newWidth)
                        inputScale = string.Format(@"scale={0}:-1", newWidth);

                    FFmpeg(inputImage, inputScale, outputScale, outputImage);
                }
            }
        }

        public void CreatePosterImage(string inputImage, string outputImage)
        {
            if (!File.Exists(outputImage))
            {
                var inputSize = Plugin.ImageProcessor.GetImageSize(inputImage);

                if (inputSize.Width > 0 && inputSize.Height > 0)
                {
                    float scaleWidth = 490 / (float)inputSize.Width;
                    float scaleHeight = 740 / (float)inputSize.Height;
                    float scaleFactor = Math.Min(scaleWidth, scaleHeight);

                    int newWidth = (int)(inputSize.Width * scaleFactor);
                    int newHeight = (int)(inputSize.Height * scaleFactor);

                    var outputScale = @"s=500x750";

                    var inputScale = string.Empty;

                    if (newWidth < newHeight)
                        inputScale = string.Format(@"scale=-1:{0}", newHeight);

                    if (newHeight < newWidth)
                        inputScale = string.Format(@"scale={0}:-1", newWidth);

                    FFmpeg(inputImage, inputScale, outputScale, outputImage);
                }
            }
        }

        private void FFmpeg(string inputImage, string inputScale, string outputScale, string outputImage)
        {
            var ffmpegArguments = string.Format(@"-nostdin -threads 1 -y -i ""{0}"" -filter_complex ""[0:0]{1}[img];color=c=0xffffff@0x00:{2},format=rgba[bg];[bg][img]overlay=(main_w-overlay_w)/2:(main_h-overlay_h)/2:shortest=1:format=rgb,format=rgba[out]"" -map [out] -c:v png -frames:v 1 ""{3}""",
                inputImage, inputScale, outputScale, outputImage);

            Process FFmpegProcess;

            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                Arguments = ffmpegArguments,
                FileName = Plugin.FfmpegManager.FfmpegConfiguration.EncoderPath,
                CreateNoWindow = true,
                RedirectStandardInput = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using (FFmpegProcess = Process.Start(processStartInfo))
            {
                FFmpegProcess.WaitForExit();
            }
        }
    }
}