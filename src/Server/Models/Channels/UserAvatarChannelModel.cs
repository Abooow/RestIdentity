using System.Drawing.Drawing2D;

namespace RestIdentity.Server.Models.Channels;

public sealed class UserAvatarChannelModel : ChannelModel
{
    public string UserId { get; init; }
    public string TempFilePath { get; init; }
    public string OriginalFileName { get; init; }
    public string OriginalFileType { get; init; }
    public long OriginalFileLength { get; init; }
    public IEnumerable<int> DesiredImageSizes { get; init; }
    public InterpolationMode InterpolationMode { get; init; }

    private UserAvatarChannelModel(string userId, string tempFilePath, string originalFileName, string originalFileType, long originalFileLength, IEnumerable<int> desiredImageSizes, InterpolationMode interpolationMode)
    {
        UserId = userId;
        TempFilePath = tempFilePath;
        OriginalFileName = originalFileName;
        OriginalFileType = originalFileType;
        OriginalFileLength = originalFileLength;
        DesiredImageSizes = desiredImageSizes;
        InterpolationMode = interpolationMode;
    }

    public static UserAvatarChannelModel CreateNew(string userId, string tempFilePath, string originalFileName, string originalFileType, long originalFileLength, IEnumerable<int> desiredImageSizes, InterpolationMode interpolationMode)
    {
        return new UserAvatarChannelModel(userId, tempFilePath, originalFileName, originalFileType, originalFileLength, desiredImageSizes, interpolationMode)
        {
            Id = Guid.NewGuid(),
            DateRequested = DateTime.UtcNow,
        };
    }
}
