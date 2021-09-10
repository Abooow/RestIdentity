using System.Drawing.Drawing2D;
using System.Threading.Channels;
using RestIdentity.Server.Models.Channels;

namespace RestIdentity.Server.BackgroundServices.Channels;

public sealed class UserAvatarChannel
{
    private readonly Channel<UserAvatarChannelModel> _channel;

    public UserAvatarChannel()
    {
        _channel = Channel.CreateUnbounded<UserAvatarChannelModel>();
    }

    public UserAvatarChannelModel Write(string userId, string tempFilePath, string originalFileName, string originalFileType, long originalFileLength, IEnumerable<int> desiredImageSizes, InterpolationMode interpolationMode)
    {
        var userAvatarChannelModel = UserAvatarChannelModel.CreateNew(userId, tempFilePath, originalFileName, originalFileType, originalFileLength, desiredImageSizes, interpolationMode);
        _channel.Writer.TryWrite(userAvatarChannelModel);

        return userAvatarChannelModel;
    }

    public ValueTask<UserAvatarChannelModel> ReadAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}
