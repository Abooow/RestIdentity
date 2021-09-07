using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text;
using ImageMagick;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RestIdentity.Server.BackgroundServices.Channels;
using RestIdentity.Server.Data;
using RestIdentity.Server.Models.Channels;
using RestIdentity.Server.Models.DAO;
using RestIdentity.Server.Models.Options;
using RestIdentity.Server.Services.SignedInUser;
using RestIdentity.Shared.Wrapper;
using Serilog;

namespace RestIdentity.Server.Services.ProfileImage;

internal sealed class ProfileImageService : IProfileImageService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ISignedInUserService _signedInUserService;
    private readonly FileStorageOptions _fileStorageOptions;
    private readonly ProfileImageChannel _profileImageChannel;
    private readonly ProfileImageDefaultOptions _profileImageOptions;

    public ProfileImageService(
        ApplicationDbContext dbContext,
        ISignedInUserService signedInUserService,
        IOptions<FileStorageOptions> fileStorageOptions,
        IOptions<ProfileImageDefaultOptions> profileImageOptions,
        ProfileImageChannel profileImageChannel)
    {
        _dbContext = dbContext;
        _signedInUserService = signedInUserService;
        _fileStorageOptions = fileStorageOptions.Value;
        _profileImageOptions = profileImageOptions.Value;
        _profileImageChannel = profileImageChannel;
        _dbContext = dbContext;
    }

    public async Task CreateDefaultProfileImageAsync(ApplicationUser user)
    {
        string userIdHash = HashUserId(user.Id);
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            user.ProfilePicHash = userIdHash;
            _dbContext.Update(user);

            CreateDefaultProfileImages(user, userIdHash);

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            Log.Error("An error occurred while creating default user profile for user {Id}. {Error} {StackTrace} {InnerException} {Source}",
                user.Id, e.Message, e.StackTrace, e.InnerException, e.Source);

            await transaction.RollbackAsync();

            string userDirectory = $@"{_fileStorageOptions.UserProfileImagesPath}\{userIdHash}";
            if (Directory.Exists(userDirectory))
                Directory.Delete(userDirectory, true);
        }
    }

    public (string Location, string ActualContentType) GetPhysicalFileLocation(string userIdHash, string contentType, int? size)
    {
        string userFolderPath = @$"{_fileStorageOptions.UserProfileImagesPath}\{userIdHash}";

        if (!Directory.Exists(userFolderPath))
            throw new FileNotFoundException();

        string acualFileType = GetFileExtensionInDirectory(userFolderPath);
        contentType = contentType == string.Empty
            ? acualFileType == "png" ? _profileImageOptions.DefaultImageExtension : _profileImageOptions.DefaultGifExtension
            : contentType[1..];

        EnsureContentTypeIsAllowed(contentType);
        int validSize = GetValidSizeForFileType(size, acualFileType);

        return ($@"{userFolderPath}\profileImage{validSize}.{acualFileType}", contentType);
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

        string userId = _signedInUserService.GetUserId();
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

    private static Image CreateTextProfileImage(string userInitials, int size, Color backgroundColor)
    {
        var tempImage = new Bitmap(1, 1);
        var drawing = Graphics.FromImage(tempImage);

        tempImage.Dispose();
        drawing.Dispose();

        var img = new Bitmap(size, size);
        var imgCanvas = Graphics.FromImage(img);
        imgCanvas.Clear(backgroundColor);

        using var textBrush = new SolidBrush(Color.White);
        using var centerStringFormat = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        using var arialFont = new Font("Arial", 24 * size / 64);
        imgCanvas.DrawString(userInitials, arialFont, textBrush, new PointF(size / 2f, size / 2f), centerStringFormat);

        imgCanvas.Save();
        imgCanvas.Dispose();

        return img;
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

    private static string HashUserId(string userId)
    {
        using var sha1 = SHA1.Create();
        byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(userId));
        string userIdHash = string.Concat(hash.Select(x => x.ToString("x2")));

        return userIdHash;
    }

    private void CreateDefaultProfileImages(ApplicationUser user, string userIdHash)
    {
        int userNameHashCode = Math.Abs(user.UserName.GetHashCode());
        Color backgroundColor = ImageUtilities.ColorFromHSV(userNameHashCode % 360, 75 / 100f, 75 / 100f);
        string userInitials = $"{user.FirstName[0]}{user.LastName[0]}";

        string userDirectory = $@"{_fileStorageOptions.UserProfileImagesPath}\{userIdHash}";
        Directory.CreateDirectory(userDirectory);

        foreach (int size in _profileImageOptions.ImageSizes)
        {
            string filePath = $@"{userDirectory}\profileImage{size}.{_profileImageOptions.DefaultImageExtension}";
            using Image profileImage = CreateTextProfileImage(userInitials, size, backgroundColor);
            profileImage.Save(filePath);
        }
    }

    private bool IsContentTypeAllowed(string contentType)
    {
        return _profileImageOptions.AllowedFileExtensions.Contains(contentType, StringComparer.OrdinalIgnoreCase);
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
