namespace RestIdentity.Server.Models.Options;

public sealed class UserAvatarDefaultOptions
{
    public IEnumerable<string> AllowedFileExtensions { get; set; }
    public IEnumerable<int> ImageSizes { get; set; }
    public IEnumerable<int> GifSizes { get; set; }
    public string DefaultImageExtension { get; set; }
    public string DefaultGifExtension { get; set; }
    public int DefaultImageSize { get; set; }
    public int DefaultGifSize { get; set; }
    public long MaxUploadImageSizeInBytes { get; set; }
    public long MaxUploadGifSizeInBytes { get; set; }
    public string DefaultAvatarDirectoryUrl { get; set; }
}
