using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using ImageMagick;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using RestIdentity.Server.BackgroundServices.Channels;
using RestIdentity.Server.Constants;
using RestIdentity.Server.Models;
using RestIdentity.Server.Models.Channels;
using RestIdentity.Server.Models.DAO;
using RestIdentity.Server.Models.Options;
using RestIdentity.Server.Services.Cookies;
using RestIdentity.Server.Services.User;
using RestIdentity.Shared.Wrapper;
using Serilog;

namespace RestIdentity.Server.Services.ProfileImage;

internal sealed class ProfileImageService : IProfileImageService
{
    private readonly FileStorageOptions _fileStorageOptions;
    private readonly ProfileImageChannel _profileImageChannel;
    private readonly ProfileImageDefaultOptions _profileImageOptions;

    private readonly ICookieService _cookieService;
    private readonly IServiceProvider _serviceProvider;
    private readonly DataProtectionKeys _dataProtectionKeys;

    public ProfileImageService(
        IOptions<FileStorageOptions> fileStorageOptions,
        IOptions<ProfileImageDefaultOptions> profileImageOptions,
        ProfileImageChannel profileImageChannel,
        ICookieService cookieService,
        IServiceProvider serviceProvider,
        IOptions<DataProtectionKeys> dataProtectionKeys)
    {
        _fileStorageOptions = fileStorageOptions.Value;
        _profileImageOptions = profileImageOptions.Value;
        _profileImageChannel = profileImageChannel;
        _cookieService = cookieService;
        _serviceProvider = serviceProvider;
        _dataProtectionKeys = dataProtectionKeys.Value;
    }

    public Task<string> CreateDefaultProfileImageAsync(ApplicationUser user)
    {
        return Task.FromResult(string.Empty);
    }

    public async Task<Result<ProfileImageChannelModel>> UploadProfileImageForSignedInUserAsync(IFormFile file, InterpolationMode interpolationMode)
    {
        if (GifFileIsTooLarge(file))
            return CreateFileTooLargeResult("Gif", _profileImageOptions.MaxUploadGifSizeInBytes, file.Length);
        else if (ImageFileIsTooLarge(file))
            return CreateFileTooLargeResult("Image", _profileImageOptions.MaxUploadImageSizeInBytes, file.Length);

        if (!IsContentTypeAllowed(file.ContentType.Split('/')[1]))
        {
            string allowedTypes = string.Join(", ", _profileImageOptions.AllowedFileExtensions.Select(x => "image/" + x));
            string message = $"Unsupported image type. Valid types are {allowedTypes}, but it was {file.ContentType}";

            return Result<ProfileImageChannelModel>.Fail(message).WithDescription(StatusCodeDescriptions.FileTypeIsUnsupported);
        }

        string tempFilePath = $@"{_fileStorageOptions.TempFilesPath}\{Path.GetRandomFileName()}";
        using FileStream tempFileStream = File.Create(tempFilePath);
        file.CopyTo(tempFileStream);

        string userId = GetLoggedInUserId();
        ProfileImageChannelModel data = WriteToProfileImageChannel(file, userId, tempFilePath, interpolationMode);
        return Result<ProfileImageChannelModel>.Success(data, "Image uploaded successfully, your profile image will update shortly.");
    }

    public Task RemoveProfileImageForSignedInUserAsync()
    {
        throw new NotImplementedException();
    }

    public Task RemoveProfileImageAsync(string userId)
    {
        throw new NotImplementedException();
    }

    public async Task CreateFromChannelAsync(ProfileImageChannelModel profileImage)
    {
        string savePath = @$"{_fileStorageOptions.UserProfileImagesPath}\{profileImage.UserId}";
        EnsureDirectoryCreated(savePath);

        var image = Image.FromFile(profileImage.TempFilePath);

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

    private static Result<ProfileImageChannelModel> CreateFileTooLargeResult(string type, long maxSize, long actualSize)
    {
        return Result<ProfileImageChannelModel>.Fail($"{type} is too large. Maximum is {maxSize / 1000000f:0.0}MB ({maxSize} bytes), but it was {actualSize} bytes")
            .WithDescription(StatusCodeDescriptions.FileTooLarge);
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
            using Bitmap resizedImage = ImageUtilities.ResizeImage(image, size, size, profileImage.InterpolationMode);
            resizedImage.Save($@"{savePath}\profileImage{size}.png", ImageFormat.Png);
        }
    }

    private async static Task CreateGifsAsync(ProfileImageChannelModel profileImage, Size originalSize, string savePath)
    {
        using FileStream gifStream = File.OpenRead(profileImage.TempFilePath);

        foreach (int size in profileImage.DesiredImageSizes)
        {
            gifStream.Seek(0, SeekOrigin.Begin);
            using MagickImageCollection resizedGif = ImageUtilities.ResizeGif(gifStream, originalSize.Width, originalSize.Height, size, size, profileImage.InterpolationMode);
            await resizedGif.WriteAsync($@"{savePath}\profileImage{size}.gif", MagickFormat.Gif);
        }

        File.Create($@"{savePath}\gif_type.dbl").Dispose();
    }

    private bool IsContentTypeAllowed(string contentType)
    {
        return _profileImageOptions.AllowedFileExtensions.Contains(contentType, StringComparer.OrdinalIgnoreCase);
    }

    private string GetLoggedInUserId()
    {
        try
        {
            var protectorProvider = _serviceProvider.GetService<IDataProtectionProvider>();
            IDataProtector protector = protectorProvider.CreateProtector(_dataProtectionKeys.ApplicationUserKey);

            return protector.Unprotect(_cookieService.GetCookie(CookieConstants.UserId));
        }
        catch (Exception e)
        {
            Log.Error("An error occurred while trying to get user_id from cookies {Error} {StackTrace} {InnerExeption} {Source}",
                e.Message, e.StackTrace, e.InnerException, e.Source);
        }

        return null;
    }

    private ProfileImageChannelModel WriteToProfileImageChannel(IFormFile file, string userId, string tempFilePath, InterpolationMode interpolationMode)
    {
        return file.ContentType switch
        {
            "image/png" or "image/jpeg" => _profileImageChannel.Write(userId, tempFilePath, file.FileName, file.ContentType, file.Length, _profileImageOptions.ImageSizes, interpolationMode),
            "image/gif" => _profileImageChannel.Write(userId, tempFilePath, file.FileName, file.ContentType, file.Length, _profileImageOptions.GifSizes, interpolationMode),
            _ => throw new Exception()
        };
    }

    private bool GifFileIsTooLarge(IFormFile file)
    {
        return file.ContentType == "image/gif" && file.Length > _profileImageOptions.MaxUploadGifSizeInBytes;
    }

    private bool ImageFileIsTooLarge(IFormFile file)
    {
        return file.Length > _profileImageOptions.MaxUploadImageSizeInBytes;
    }

    private int GetValidSizeForFileType(int? size, string fileType)
    {
        return (size, fileType) switch
        {
            // Make sure it's a valid size for png's.
            ({ }, "png") => _profileImageOptions.ImageSizes.Contains(size.Value) ? size.Value : _profileImageOptions.DefaultImageSize,
            (null, "png") => _profileImageOptions.DefaultImageSize,

            // Make sure it's a valid size for gif's.
            ({ }, "gif") => _profileImageOptions.GifSizes.Contains(size.Value) ? size.Value : _profileImageOptions.DefaultGifSize,
            (null, "gif") => _profileImageOptions.DefaultGifSize,

            _ => throw new Exception("Unexpected File Extension. Expected png or gif, but was " + fileType)
        };
    }

    private void EnsureContentTypeIsAllowed(string contentType)
    {
        if (!IsContentTypeAllowed(contentType))
            throw new Exception($"Invalid extension requested. Allowed extensions are ({string.Join(", ", _profileImageOptions.AllowedFileExtensions)}), but it was ({contentType})");
    }

    private string GetFileExtensionInDirectory(string directory)
    {
        return File.Exists($@"{directory}\gif_type.dbl") ? "gif" : "png";
    }
}
