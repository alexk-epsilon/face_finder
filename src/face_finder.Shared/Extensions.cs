using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using face_finder.Shared.Models;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace face_finder.Shared
{
    public static class Extensions
    {
        private static readonly FontFamily FontFamily;

        static Extensions()
        {
            var fonts = new FontCollection();
            FontFamily = fonts.Install("./fonts/arial.ttf");
        }

        public static MemoryStream GetStream(this Image image, IImageFormat format)
        {
            using var ms = new MemoryStream();
            image.Save(ms, format);
            return ms;
        }

        public static string ToBase64String(this Image source, IImageFormat format)
        {
            using var stream = new MemoryStream();
            source.Save(stream, format);
            stream.TryGetBuffer(out ArraySegment<byte> buffer);
            return Convert.ToBase64String(buffer.Array, 0, (int)stream.Length);
        }

        public static Stream ToStream(this Image image, IImageFormat format)
        {
            var stream = new MemoryStream();
            image.Save(stream, format);
            stream.Position = 0;
            return stream;
        }

        public static Image DrawFrames(this Image source, IEnumerable<Subject> faces)
        {
            foreach (var subject in faces)
            {
                var color = Color.Red;

                if (subject.Person.Similarity > 20.0)
                    color = Color.Yellow;

                if (subject.Person.Similarity > 95.0)
                    color = Color.Green;

                var rectangle = new RectangleF(subject.Box.XMin, subject.Box.YMax, subject.Box.XMax - subject.Box.XMin,
                    subject.Box.YMin - subject.Box.YMax);

                source.Mutate(x => x.Draw(color, 3, rectangle));

                var font = new Font(FontFamily, 30, FontStyle.Bold);
                source.Mutate(x => x.DrawText(subject.Id.ToString(), font, color,
                    new PointF(subject.Box.XMin - 20, subject.Box.YMax - 20)));
            }

            return source;
        }

        public static IImageProcessingContext ApplyScalingWaterMark(
            this IImageProcessingContext processingContext, string text = "@OkoZmagara_bot")
        {
            var imgSize = processingContext.GetCurrentSize();

            const float padding = 10F;
            var targetWidth = imgSize.Width - (padding * 2);
            var targetHeight = imgSize.Height - (padding * 2);

            var font = new Font(FontFamily, 3);
            // measure the text size
            var size = TextMeasurer.Measure(text, new RendererOptions(font));

            //find out how much we need to scale the text to fill the space (up or down)
            var scalingFactor = Math.Min(targetWidth / size.Width, targetHeight / size.Height);

            //create a new font
            var scaledFont = new Font(font, scalingFactor * font.Size);

            var center = new PointF(imgSize.Width / 2, imgSize.Height * 0.9F);
            var textGraphicOptions = new DrawingOptions
            {
                TextOptions =
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            return processingContext.DrawText(textGraphicOptions, text, scaledFont, Rgba32.ParseHex("ff00ff40"), center);
        }

        public static string ConvertAsciiStringToHexString(this string input)
        {
            var charValues = input.ToCharArray();
            var hexOutput = string.Empty;
            foreach (var eachChar in charValues)
            {
                var value = Convert.ToInt32(eachChar);
                hexOutput += $"{value:X}";
            }

            return hexOutput;
        }

        public static string ConvertHexStringToAsciiString(this string hexString)
        {
            var ascii = string.Empty;

            for (var i = 0; i < hexString.Length; i += 2)
            {
                var hs = string.Empty;

                hs = hexString.Substring(i, 2);
                var decval = Convert.ToUInt32(hs, 16);
                var character = Convert.ToChar(decval);
                ascii += character;
            }

            return ascii;
        }

        public static MemoryStream CropImage(this Image image, int x, int y, int width, int height)
        {
            //var x = Convert.ToInt32(Math.Truncate(xF));
            //var y = Convert.ToInt32(Math.Ceiling(yF));
            //var width = Convert.ToInt32(Math.Ceiling(widthF));
            //var height = Convert.ToInt32(Math.Ceiling(heightF));

            //x = x < 0 ? 0 : x;
            //y = y > image.Height ? image.Height : y;
            //width = x + width >= image.Width ? image.Width - x : width;
            //height = y - height <= 0 ? image.Height - y : height;

            var cropArea = new Rectangle(x, y, width, height);

            using var outStream = new MemoryStream();
            var clone = image.Clone(
                i => i.Crop(cropArea));

            clone.Save(outStream, new JpegEncoder());
            //clone.Save($"E:\\face_finder\\{Guid.NewGuid()}.jpg", new JpegEncoder());

            return outStream;
        }

        public static JsonSerializerOptions GetJsonSerializerOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
                WriteIndented = true
            };
        }

        public static string RotateAndRecodeImage(this string base64String)
        {
            var imageBytes = Convert.FromBase64String(base64String);

            using var image = Image.Load(imageBytes, out var imageFormat);
            IImageEncoder imageEncoderForJpeg = new JpegEncoder()
            {
                Quality = 80
            };

            using var ms = new MemoryStream();
            image.Save(ms, imageEncoderForJpeg);
            var bytes = ms.ToArray();
            return Convert.ToBase64String(bytes);
        }
    }
}