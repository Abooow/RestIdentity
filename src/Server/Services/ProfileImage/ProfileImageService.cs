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

    public Task<UserAvatarModel> FindByIdAsync(string userId)
    {
        return _dbContext.UserAvatars
            .Where(x => x.UserId == userId)
            .Include(x => x.User)
            .FirstOrDefaultAsync();
    }

    public Task<UserAvatarModel> FindByUserNameAsync(string userName)
    {
        userName = userName.ToUpperInvariant();
        var query = from u in _dbContext.Users
                    from ua in _dbContext.UserAvatars.Include(x => x.User)
                    where u.Id == ua.UserId && u.NormalizedUserName == userName
                    select ua;

        return query.FirstOrDefaultAsync();
    }

    private Task<UserAvatarModel> FindByAvatarHashAsync(string avatarHash)
    {
        return _dbContext.UserAvatars
            .Where(x => x.AvatarHash == avatarHash)
            .Include(x => x.User)
            .FirstOrDefaultAsync();
    }

    public async Task CreateDefaultProfileImageAsync(ApplicationUser user)
    {
        UserAvatarModel existingUserAvatar = await FindByIdAsync(user.Id);
        string userIdHash = HashUserId(user.Id);

        if (existingUserAvatar is null)
        {
            // Create new.
            var userAvatar = new UserAvatarModel(user)
            {
                AvatarHash = userIdHash
            };

            await _dbContext.UserAvatars.AddAsync(userAvatar);
            await _dbContext.SaveChangesAsync();
        }
        else
        {
            // Update Existing.
            existingUserAvatar.LastModifiedDate = DateTime.UtcNow;

            _dbContext.Update(existingUserAvatar);
            await _dbContext.SaveChangesAsync();
        }

        CreateDefaultProfileImage(user, userIdHash);
    }

    public async ValueTask<(string Location, string NormalizedContentType)> GetPhysicalFileLocation(string userIdHash, string contentType, int? size)
    {
        UserAvatarModel userAvatar = await FindByAvatarHashAsync(userIdHash);
        if (userAvatar is null)
            throw new Exception(); // TODO: Change to a good Exception type with a good message.

        string acualFileType = userAvatar.UsesDefaultAvatar ? _profileImageOptions.DefaultImageExtension : userAvatar.ImageExtension;

        string validContentType = contentType == string.Empty
            ? acualFileType == "png" ? _profileImageOptions.DefaultImageExtension : _profileImageOptions.DefaultGifExtension // Use a default extension.
            : contentType[1..]; // Use the requested extension.
        EnsureContentTypeIsAllowed(validContentType);

        int validSize = GetValidSizeForFileType(size, acualFileType);

        string profileImagePath = userAvatar.UsesDefaultAvatar
            ? @$"{_fileStorageOptions.UserProfileImagesPath}\{userIdHash}\{_profileImageOptions.DefaultAvatarDirectoryUrl}\profileImage.{acualFileType}"
            : @$"{_fileStorageOptions.UserProfileImagesPath}\{userIdHash}\profileImage{validSize}.{acualFileType}";

        return (Path.GetFullPath(profileImagePath), validContentType);
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

    public async Task CreateFromChannelAsync(ProfileImageChannelModel profileImageChannelModel)
    {
        string savePath = @$"{_fileStorageOptions.UserProfileImagesPath}\{HashUserId(profileImageChannelModel.UserId)}";
        EnsureDirectoryCreated(savePath);

        var image = Image.FromFile(profileImageChannelModel.TempFilePath);

        switch (profileImageChannelModel.OriginalFileType)
        {
            case "image/png":
            case "image/jpeg":
                await CreateImages(profileImageChannelModel, image, savePath);
                image.Dispose();
                break;

            case "image/gif":
                Size size = image.Size;
                image.Dispose();
                await CreateGifsAsync(profileImageChannelModel, size, savePath);
                break;

            default:
                image.Dispose();
                File.Delete(profileImageChannelModel.TempFilePath);
                throw new Exception($"File type is not supported. Supported types are (image/png, image/jpeg, image/gif), but is was {profileImageChannelModel.OriginalFileType}");
        }

        File.Delete(profileImageChannelModel.TempFilePath);
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

    private static string HashUserId(string userId)
    {
        using var sha1 = SHA1.Create();
        byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(userId));
        string userIdHash = string.Concat(hash.Select(x => x.ToString("x2")));

        return userIdHash;
    }

    private static Color GetRandomColor()
    {
        var random = new Random();
        return ImageUtilities.ColorFromHSV(random.Next(360), 75 / 100f, 75 / 100f);
    }

    private async Task CreateImages(ProfileImageChannelModel profileImageChannelModel, Image image, string savePath)
    {
        foreach (int size in profileImageChannelModel.DesiredImageSizes)
        {
            using Bitmap resizedImage = ImageUtilities.ResizeImage(image, size, size, profileImageChannelModel.InterpolationMode);
            resizedImage.Save($@"{savePath}\profileImage{size}.png", ImageFormat.Png);
        }

        await UpdateUserAvatar(profileImageChannelModel.UserId, "png");
    }

    private async Task CreateGifsAsync(ProfileImageChannelModel profileImageChannelModel, Size originalSize, string savePath)
    {
        using FileStream gifStream = File.OpenRead(profileImageChannelModel.TempFilePath);

        foreach (int size in profileImageChannelModel.DesiredImageSizes)
        {
            gifStream.Seek(0, SeekOrigin.Begin);
            using MagickImageCollection resizedGif = ImageUtilities.ResizeGif(gifStream, originalSize.Width, originalSize.Height, size, size, profileImageChannelModel.InterpolationMode);
            await resizedGif.WriteAsync($@"{savePath}\profileImage{size}.gif", MagickFormat.Gif);
        }

        await UpdateUserAvatar(profileImageChannelModel.UserId, "gif");
    }

    private async Task UpdateUserAvatar(string userId, string imageExtension)
    {
        UserAvatarModel userAvatar = await FindByIdAsync(userId);
        userAvatar.UsesDefaultAvatar = false;
        userAvatar.ImageExtension = imageExtension;
        userAvatar.LastModifiedDate = DateTime.UtcNow;

        _dbContext.Update(userAvatar);
        await _dbContext.SaveChangesAsync();
    }

    private void CreateDefaultProfileImage(ApplicationUser user, string userIdHash)
    {
        Color backgroundColor = GetRandomColor();
        string userInitials = $"{user.FirstName[0]}{user.LastName[0]}".ToUpperInvariant();

        string defaultAvatarDirectory = $@"{_fileStorageOptions.UserProfileImagesPath}\{userIdHash}\{_profileImageOptions.DefaultAvatarDirectoryUrl}";
        Directory.CreateDirectory(defaultAvatarDirectory);

        string filePath = $@"{defaultAvatarDirectory}\profileImage.{_profileImageOptions.DefaultImageExtension}";
        using Image profileImage = CreateTextProfileImage(userInitials, _profileImageOptions.DefaultImageSize, backgroundColor);
        profileImage.Save(filePath);
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
}
