using StbImageSharp;

namespace NINA.PINS.AllSky.Services;

internal static class ImageMeanAnalyzer
{
    // AllSky uses a circular mask centered in the frame with a radius of one third
    // of the image height. This keeps the auto-exposure controller focused on the
    // sky instead of the horizon and matches the upstream behavior closely.
    public static double CalculateNormalizedMean(string imagePath)
    {
        using var stream = File.OpenRead(imagePath);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        if (image.Width <= 0 || image.Height <= 0 || image.Data.Length == 0)
        {
            throw new InvalidOperationException($"Unable to analyze image mean for '{imagePath}'.");
        }

        var centerX = image.Width / 2.0;
        var centerY = image.Height / 2.0;
        var radius = image.Height / 3.0;
        var radiusSquared = radius * radius;

        double sum = 0;
        long countedPixels = 0;

        for (var y = 0; y < image.Height; y++)
        {
            var dy = y - centerY;
            for (var x = 0; x < image.Width; x++)
            {
                var dx = x - centerX;
                if ((dx * dx) + (dy * dy) > radiusSquared)
                {
                    continue;
                }

                var offset = (y * image.Width + x) * 4;
                var red = image.Data[offset];
                var green = image.Data[offset + 1];
                var blue = image.Data[offset + 2];

                sum += (red + green + blue) / 3.0;
                countedPixels++;
            }
        }

        if (countedPixels == 0)
        {
            throw new InvalidOperationException($"Image mean mask produced no samples for '{imagePath}'.");
        }

        return sum / (countedPixels * 255.0);
    }
}
