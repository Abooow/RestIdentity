using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using ImageMagick;

namespace RestIdentity.Server;

internal static class ImageUtilities
{
    private static readonly Dictionary<InterpolationMode, PixelInterpolateMethod> MagixInterpolateMethodMap = new()
    {
        [InterpolationMode.Default] = PixelInterpolateMethod.Undefined,
        [InterpolationMode.Low] = PixelInterpolateMethod.Average,
        [InterpolationMode.High] = PixelInterpolateMethod.Average,
        [InterpolationMode.Bicubic] = PixelInterpolateMethod.Bilinear,
        [InterpolationMode.Bilinear] = PixelInterpolateMethod.Bilinear,
        [InterpolationMode.HighQualityBicubic] = PixelInterpolateMethod.Bilinear,
        [InterpolationMode.HighQualityBilinear] = PixelInterpolateMethod.Bilinear,
        [InterpolationMode.NearestNeighbor] = PixelInterpolateMethod.Nearest
    };

    public static Bitmap ResizeImage(Image image, int newWidth, int newHeight, InterpolationMode interpolationMode)
    {
        var destRect = new Rectangle(0, 0, newWidth, newHeight);
        var destImage = new Bitmap(newWidth, newHeight);

        destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

        var sourceRect = new Rectangle();
        if (image.Height > image.Width)
        {
            sourceRect.X = 0;
            sourceRect.Y = (image.Height - image.Width) / 2;
            sourceRect.Width = image.Width;
            sourceRect.Height = image.Width;
        }
        else
        {
            sourceRect.X = (image.Width - image.Height) / 2;
            sourceRect.Y = 0;
            sourceRect.Width = image.Height;
            sourceRect.Height = image.Height;
        }

        using (var graphics = Graphics.FromImage(destImage))
        {
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = interpolationMode;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            using var wrapMode = new ImageAttributes();
            wrapMode.SetWrapMode(WrapMode.TileFlipXY);
            graphics.DrawImage(image, destRect, sourceRect.X, sourceRect.Y, sourceRect.Width, sourceRect.Height, GraphicsUnit.Pixel, wrapMode);
        }

        return destImage;
    }

    public static MagickImageCollection ResizeGif(Stream gifStream, int originalWidth, int originalHeight, int newWidth, int newHeight, InterpolationMode interpolationMode)
    {
        var newGif = new MagickImageCollection(gifStream);
        int size = originalHeight > originalWidth ? originalWidth : originalHeight;

        newGif.Coalesce();
        foreach (IMagickImage<ushort> image in newGif)
        {
            image.Crop(size, size, Gravity.Center);
            image.RePage();

            image.Interpolate = MagixInterpolateMethodMap[interpolationMode];
            image.Resize(newWidth, newHeight);
        }

        return newGif;
    }

    /// <param name="hue">From 0 to 360</param>
    /// <param name="saturation">From 0 to 1</param>
    /// <param name="value">From 0 to 1</param>
    public static Color ColorFromHSV(float hue, float saturation, float value)
    {
        int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
        double f = hue / 60 - Math.Floor(hue / 60);
        value *= 255;

        int v = Convert.ToInt32(value);
        int p = Convert.ToInt32(value * (1 - saturation));
        int q = Convert.ToInt32(value * (1 - f * saturation));
        int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

        if (hi == 0)
            return Color.FromArgb(255, v, t, p);
        else if (hi == 1)
            return Color.FromArgb(255, q, v, p);
        else if (hi == 2)
            return Color.FromArgb(255, p, v, t);
        else if (hi == 3)
            return Color.FromArgb(255, p, q, v);
        else if (hi == 4)
            return Color.FromArgb(255, t, p, v);
        else
            return Color.FromArgb(255, v, p, q);
    }
}
