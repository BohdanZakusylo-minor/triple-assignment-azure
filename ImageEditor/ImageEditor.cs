using System.Reflection;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Compnay.Function.ImageEditor
{
    public class ImageHelper
    {
        /// <summary>
        /// Font family names to try in order. Arial on Windows; Liberation Sans/DejaVu Sans on Linux (e.g. Azure).
        /// </summary>
        private static readonly string[] FontFamilyFallbacks = { "Arial", "Liberation Sans", "DejaVu Sans", "Ubuntu", "sans-serif" };

        private static FontFamily? _embeddedFontFamily;

        private static Font GetFont(float size)
        {
            foreach (var familyName in FontFamilyFallbacks)
            {
                if (SystemFonts.TryFind(familyName, out var family))
                    return family.CreateFont(size);
            }
            // Azure/minimal Linux often has no system fonts; use first available or embedded.
            if (SystemFonts.Families.Any())
                return SystemFonts.Families.First().CreateFont(size);
            // Load embedded font (ImageEditor/DefaultFont.ttf as EmbeddedResource).
            return GetEmbeddedFontFamily().CreateFont(size);
        }

        private static FontFamily GetEmbeddedFontFamily()
        {
            if (_embeddedFontFamily is { } cached)
                return cached;
            var assembly = typeof(ImageHelper).Assembly;
            var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase));
            if (resourceName is null)
                throw new InvalidOperationException("No system fonts and no embedded .ttf found. On Azure/Linux add a .ttf (e.g. LiberationSans-Regular.ttf) to the project as EmbeddedResource.");
            using var stream = assembly.GetManifestResourceStream(resourceName)!;
            var collection = new FontCollection();
            _embeddedFontFamily = collection.Install(stream);
            return _embeddedFontFamily;
        }

        public static Stream AddTextToImage(Stream imageStream, params (string text, (float x, float y) position, int fontSize, string colorHex)[] texts)
        {
            var memoryStream = new MemoryStream();

            var image = Image.Load(imageStream);

            image.Clone(img =>
            {
                var textGraphicsOptions = new TextGraphicsOptions()
                {
                    TextOptions = {
                            WrapTextWidth = image.Width - 10
                        }
                };

                foreach (var (text, (x, y), fontSize, colorHex) in texts)
                {
                    var font = GetFont(fontSize);
                    var color = Rgba32.ParseHex(colorHex);

                    img.DrawText(textGraphicsOptions, text, font, color, new PointF(x, y));
                }
            })
                .SaveAsPng(memoryStream);

            memoryStream.Position = 0;

            return memoryStream;
        }
    }
}