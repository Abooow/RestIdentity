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
}
