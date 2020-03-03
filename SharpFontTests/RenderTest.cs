using SharpFontManaged;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace SharpFontTests
{
    public class RenderTest
    {
        private const string ComparisonPath = "font_rasters";

        [Fact]
        public unsafe void VisualRenderTest()
        {
            var typeface = LoadTypeface(Path.Combine("Resources", "OpenSans-Regular.ttf"));

            //for (var c = 33; c < 127; c++)
            //{
            //    var comparisonFile = Path.Combine(ComparisonPath, c + ".png");
            //    CompareRender(typeface, (char)c, comparisonFile);
            //}

            if (!Directory.Exists("RESULTS"))
                Directory.CreateDirectory("RESULTS");

            for (var c = 33; c < 127; c++)
            {
                var surface = RenderGlyph(typeface, (char)c);
                
                SaveSurface(surface, Path.Combine("RESULTS", c + "_result.png"));
            }

          
        }        

        private static unsafe void CompareRender(FontFace typeface, char c, string comparisonFile)
        {
            var surface = RenderGlyph(typeface, c);

            // compare against FreeType renders
            var compare = (Bitmap)Image.FromFile(comparisonFile);
            if (compare.Width != surface.Width || compare.Height != surface.Height)
                throw new Exception();

            var bitmapData = compare.LockBits(new Rectangle(0, 0, surface.Width, surface.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            for (var y = 0; y < surface.Height; y++)
            {
                var dest = (byte*)bitmapData.Scan0 + y * bitmapData.Stride;
                var src = (byte*)surface.Bits + y * surface.Pitch;

                for (var x = 0; x < surface.Width; x++)
                {
                    var a = *src++;
                    var b = *dest;
                    if (Math.Abs(a - b) > 12)
                        throw new Exception("Image not the same! RenderTest failed!");
                    dest += 3;
                }
            }

            compare.UnlockBits(bitmapData);
            compare.Dispose();
            Marshal.FreeHGlobal(surface.Bits);
        }

        private static unsafe Surface RenderGlyph(FontFace typeface, char c)
        {
            var glyph = typeface.GetGlyph(c, 32);
            var surface = new Surface
            {
                Bits = Marshal.AllocHGlobal(glyph.RenderWidth * glyph.RenderHeight),
                Width = glyph.RenderWidth,
                Height = glyph.RenderHeight,
                Pitch = glyph.RenderWidth
            };

            var stuff = (byte*)surface.Bits;
            for (var i = 0; i < surface.Width * surface.Height; i++)
                *stuff++ = 0;

            glyph.RenderTo(surface);

            return surface;
        }

        private static unsafe void SaveSurface(Surface surface, string fileName)
        {
            var bitmap = new Bitmap(surface.Width, surface.Height, PixelFormat.Format24bppRgb);
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, surface.Width, surface.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            for (var y = 0; y < surface.Height; y++)
            {
                var dest = (byte*)bitmapData.Scan0 + y * bitmapData.Stride;
                var src = (byte*)surface.Bits + y * surface.Pitch;

                for (var x = 0; x < surface.Width; x++)
                {
                    var b = *src++;
                    *dest++ = b;
                    *dest++ = b;
                    *dest++ = b;
                }
            }

            bitmap.UnlockBits(bitmapData);
            bitmap.Save(fileName);
            bitmap.Dispose();
            Marshal.FreeHGlobal(surface.Bits);
        }

        private static unsafe FontFace LoadTypeface(string fileName)
        {
            using var file = File.OpenRead(fileName);
            return new FontFace(file);
        }
    }
}
