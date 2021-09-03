using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using ImageMagick;
using Microsoft.Extensions.Options;
using RestIdentity.Server.Models.Channels;
using RestIdentity.Server.Models.DAO;
using RestIdentity.Server.Models.Options;

namespace RestIdentity.Server.Services.ProfileImage;

internal sealed class ProfileImageService : IProfileImageService
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

    private readonly FileStorageOptions _fileStorageOptions;

    public ProfileImageService(IOptions<FileStorageOptions> fileStorageOptions)
    {
        _fileStorageOptions = fileStorageOptions.Value;
    }

    public Task<string> CreateDefaultProfileImageAsync(ApplicationUser user)
    {
        return Task.FromResult(string.Empty);
    }

    public async Task Upload(ProfileImageChannelModel profileImage)
    {
        string savePath = @$"{_fileStorageOptions.UserProfileImagesPath}\{profileImage.UserId}";
        EnsureDirectoryCreated(savePath);

        Image image = Image.FromFile(profileImage.TempFilePath);

        switch (profileImage.OriginalFileType)
        {
            case "image/png":
            case "image/jpeg":
                CreateImages(image, profileImage, savePath);
                image.Dispose();
                break;

            case "image/gif":
                Size size = image.Size;
                image.Dispose();
                await CreateGifsAsync(profileImage, size, savePath);
                break;

            default:
                image.Dispose();
                File.Delete(profileImage.TempFilePath);
                throw new Exception($"File type is not supported. Supported types are (image/png, image/jpeg, image/gif), but is was {profileImage.OriginalFileType}");
        }

        File.Delete(profileImage.TempFilePath);
    }

    private static void EnsureDirectoryCreated(string directory)
    {
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
    }

    private static void CreateImages(Image image, ProfileImageChannelModel profileImage, string savePath)
    {
        foreach (int size in profileImage.DesiredImageSizes)
        {
            using Bitmap resizedImage = ResizeImage(image, size, size, profileImage.InterpolationMode);
            resizedImage.Save($@"{savePath}\profileImage{size}.png", ImageFormat.Png);
        }
    }

    private async static Task CreateGifsAsync(ProfileImageChannelModel profileImage, Size originalSize, string savePath)
    {
        using FileStream gifStream = File.OpenRead(profileImage.TempFilePath);

        foreach (int size in profileImage.DesiredImageSizes)
        {
            gifStream.Seek(0, SeekOrigin.Begin);
            using MagickImageCollection resizedGif = ResizeGif(gifStream, originalSize.Width, originalSize.Height, size, size, profileImage.InterpolationMode);
            await resizedGif.WriteAsync($@"{savePath}\profileImage{size}.gif", MagickFormat.Gif);
        }

        File.Create($@"{savePath}\gif_type.dbl").Dispose();
    }

    private static Bitmap ResizeImage(Image image, int newWidth, int newHeight, InterpolationMode interpolationMode)
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

    private static MagickImageCollection ResizeGif(Stream gifStream, int originalWidth, int originalHeight, int newWidth, int newHeight, InterpolationMode interpolationMode)
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
