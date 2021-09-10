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

namespace RestIdentity.Server.Services.UserAvatars;

internal sealed class UserAvatarService : IUserAvatarService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ISignedInUserService _signedInUserService;
    private readonly FileStorageOptions _fileStorageOptions;
    private readonly UserAvatarDefaultOptions _userAvatarOptions;
    private readonly UserAvatarChannel _userAvatarChannel;

    public UserAvatarService(
        ApplicationDbContext dbContext,
        ISignedInUserService signedInUserService,
        IOptions<FileStorageOptions> fileStorageOptions,
        IOptions<UserAvatarDefaultOptions> userAvatarOptions,
        UserAvatarChannel userAvatarChannel)
    {
        _dbContext = dbContext;
        _signedInUserService = signedInUserService;
        _fileStorageOptions = fileStorageOptions.Value;
        _userAvatarOptions = userAvatarOptions.Value;
        _userAvatarChannel = userAvatarChannel;
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

    public async Task CreateDefaultAvatarAsync(ApplicationUser user)
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

        CreateDefaultUserAvatar(user, userIdHash);
    }

    public async ValueTask<(string Location, string NormalizedContentType)> GetImageFileLocationAsync(string userHash, string contentType, int? size)
    {
        UserAvatarModel userAvatar = await FindByAvatarHashAsync(userHash);
        if (userAvatar is null)
            throw new Exception(); // TODO: Change to a good Exception type with a good message.

        string acualFileType = userAvatar.UsesDefaultAvatar ? _userAvatarOptions.DefaultImageExtension : userAvatar.ImageExtension;

        string validContentType = contentType == string.Empty
            ? acualFileType == "png" ? _userAvatarOptions.DefaultImageExtension : _userAvatarOptions.DefaultGifExtension // Use a default extension.
            : contentType[1..]; // Use the requested extension.
        EnsureContentTypeIsAllowed(validContentType);

        int validSize = GetValidSizeForFileType(size, acualFileType);

        string userAvatarPath = userAvatar.UsesDefaultAvatar
            ? @$"{_fileStorageOptions.UserAvatarsPath}\{userHash}\{_userAvatarOptions.DefaultAvatarDirectoryUrl}\avatar.{acualFileType}"
            : @$"{_fileStorageOptions.UserAvatarsPath}\{userHash}\avatar{validSize}.{acualFileType}";

        return (Path.GetFullPath(userAvatarPath), validContentType);
    }

    public async Task<Result<UserAvatarChannelModel>> UploadAvatarForSignedInUserAsync(IFormFile file, InterpolationMode interpolationMode)
    {
        if (GifFileIsTooLarge(file))
            return CreateFileTooLargeResult("Gif", _userAvatarOptions.MaxUploadGifSizeInBytes, file.Length);
        else if (ImageFileIsTooLarge(file))
            return CreateFileTooLargeResult("Image", _userAvatarOptions.MaxUploadImageSizeInBytes, file.Length);

        if (!IsContentTypeAllowed(file.ContentType.Split('/')[1]))
        {
            string allowedTypes = string.Join(", ", _userAvatarOptions.AllowedFileExtensions.Select(x => "image/" + x));
            string message = $"Unsupported image type. Valid types are {allowedTypes}, but it was {file.ContentType}";

            return Result<UserAvatarChannelModel>.Fail(message).WithDescription(StatusCodeDescriptions.FileTypeIsUnsupported);
        }

        string tempFilePath = $@"{_fileStorageOptions.TempFilesPath}\{Path.GetRandomFileName()}";
        using FileStream tempFileStream = File.Create(tempFilePath);
        file.CopyTo(tempFileStream);

        string userId = _signedInUserService.GetUserId();
        UserAvatarChannelModel data = WriteToUserAvatarChannel(file, userId, tempFilePath, interpolationMode);
        return Result<UserAvatarChannelModel>.Success(data, "Image uploaded successfully, your profile image will update shortly.");
    }

    public Task RemoveAvatarForSignedInUserAsync()
    {
        throw new NotImplementedException();
    }

    public Task RemoveAvatarAsync(string userId)
    {
        throw new NotImplementedException();
    }

    public async Task CreateFromChannelAsync(UserAvatarChannelModel userAvatarChannelModel)
    {
        string savePath = @$"{_fileStorageOptions.UserAvatarsPath}\{HashUserId(userAvatarChannelModel.UserId)}";
        EnsureDirectoryCreated(savePath);

        var image = Image.FromFile(userAvatarChannelModel.TempFilePath);

        switch (userAvatarChannelModel.OriginalFileType)
        {
            case "image/png":
            case "image/jpeg":
                await CreateImages(userAvatarChannelModel, image, savePath);
                image.Dispose();
                break;

            case "image/gif":
                Size size = image.Size;
                image.Dispose();
                await CreateGifsAsync(userAvatarChannelModel, size, savePath);
                break;

            default:
                image.Dispose();
                File.Delete(userAvatarChannelModel.TempFilePath);
                throw new Exception($"File type is not supported. Supported types are (image/png, image/jpeg, image/gif), but is was {userAvatarChannelModel.OriginalFileType}");
        }

        File.Delete(userAvatarChannelModel.TempFilePath);
    }

    private static Image CreateAvatarImageWithText(string userInitials, int size, Color backgroundColor)
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

    private static Result<UserAvatarChannelModel> CreateFileTooLargeResult(string type, long maxSize, long actualSize)
    {
        return Result<UserAvatarChannelModel>.Fail($"{type} is too large. Maximum is {maxSize / 1000000f:0.0}MB ({maxSize} bytes), but it was {actualSize} bytes")
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

    private async Task CreateImages(UserAvatarChannelModel userAvatarChannelModel, Image image, string savePath)
    {
        foreach (int size in userAvatarChannelModel.DesiredImageSizes)
        {
            using Bitmap resizedImage = ImageUtilities.ResizeImage(image, size, size, userAvatarChannelModel.InterpolationMode);
            resizedImage.Save($@"{savePath}\avatar{size}.png", ImageFormat.Png);
        }

        await UpdateUserAvatar(userAvatarChannelModel.UserId, "png");
    }

    private async Task CreateGifsAsync(UserAvatarChannelModel userAvatarChannelModel, Size originalSize, string savePath)
    {
        using FileStream gifStream = File.OpenRead(userAvatarChannelModel.TempFilePath);

        foreach (int size in userAvatarChannelModel.DesiredImageSizes)
        {
            gifStream.Seek(0, SeekOrigin.Begin);
            using MagickImageCollection resizedGif = ImageUtilities.ResizeGif(gifStream, originalSize.Width, originalSize.Height, size, size, userAvatarChannelModel.InterpolationMode);
            await resizedGif.WriteAsync($@"{savePath}\avatar{size}.gif", MagickFormat.Gif);
        }

        await UpdateUserAvatar(userAvatarChannelModel.UserId, "gif");
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

    private void CreateDefaultUserAvatar(ApplicationUser user, string userIdHash)
    {
        Color backgroundColor = GetRandomColor();
        string userInitials = $"{user.FirstName[0]}{user.LastName[0]}".ToUpperInvariant();

        string defaultAvatarDirectory = $@"{_fileStorageOptions.UserAvatarsPath}\{userIdHash}\{_userAvatarOptions.DefaultAvatarDirectoryUrl}";
        Directory.CreateDirectory(defaultAvatarDirectory);

        string filePath = $@"{defaultAvatarDirectory}\avatar.{_userAvatarOptions.DefaultImageExtension}";
        using Image userAvatar = CreateAvatarImageWithText(userInitials, _userAvatarOptions.DefaultImageSize, backgroundColor);
        userAvatar.Save(filePath);
    }

    private bool IsContentTypeAllowed(string contentType)
    {
        return _userAvatarOptions.AllowedFileExtensions.Contains(contentType, StringComparer.OrdinalIgnoreCase);
    }

    private UserAvatarChannelModel WriteToUserAvatarChannel(IFormFile file, string userId, string tempFilePath, InterpolationMode interpolationMode)
    {
        return file.ContentType switch
        {
            "image/png" or "image/jpeg" => _userAvatarChannel.Write(userId, tempFilePath, file.FileName, file.ContentType, file.Length, _userAvatarOptions.ImageSizes, interpolationMode),
            "image/gif" => _userAvatarChannel.Write(userId, tempFilePath, file.FileName, file.ContentType, file.Length, _userAvatarOptions.GifSizes, interpolationMode),
            _ => throw new Exception()
        };
    }

    private bool GifFileIsTooLarge(IFormFile file)
    {
        return file.ContentType == "image/gif" && file.Length > _userAvatarOptions.MaxUploadGifSizeInBytes;
    }

    private bool ImageFileIsTooLarge(IFormFile file)
    {
        return file.Length > _userAvatarOptions.MaxUploadImageSizeInBytes;
    }

    private int GetValidSizeForFileType(int? size, string fileType)
    {
        return (size, fileType) switch
        {
            // Make sure it's a valid size for png's.
            ({ }, "png") => _userAvatarOptions.ImageSizes.Contains(size.Value) ? size.Value : _userAvatarOptions.DefaultImageSize,
            (null, "png") => _userAvatarOptions.DefaultImageSize,

            // Make sure it's a valid size for gif's.
            ({ }, "gif") => _userAvatarOptions.GifSizes.Contains(size.Value) ? size.Value : _userAvatarOptions.DefaultGifSize,
            (null, "gif") => _userAvatarOptions.DefaultGifSize,

            _ => throw new Exception("Unexpected File Extension. Expected png or gif, but was " + fileType)
        };
    }

    private void EnsureContentTypeIsAllowed(string contentType)
    {
        if (!IsContentTypeAllowed(contentType))
            throw new Exception($"Invalid extension requested. Allowed extensions are ({string.Join(", ", _userAvatarOptions.AllowedFileExtensions)}), but it was ({contentType})");
    }
}
