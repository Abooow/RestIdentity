using System.Drawing.Drawing2D;
using System.Threading.Channels;
using RestIdentity.Server.Models.Channels;

namespace RestIdentity.Server.BackgroundServices.Channels;

public sealed class ProfileImageChannel
{
    private readonly Channel<ProfileImageChannelModel> _channel;

    public ProfileImageChannel()
    {
        _channel = Channel.CreateUnbounded<ProfileImageChannelModel>();
    }

    public ProfileImageChannelModel Write(string userId, string tempFilePath, string originalFileName, string originalFileType, long originalFileLength, IEnumerable<int> desiredImageSizes, InterpolationMode interpolationMode)
    {
        var profileImageChannelModel = ProfileImageChannelModel.CreateNew(userId, tempFilePath, originalFileName, originalFileType, originalFileLength, desiredImageSizes, interpolationMode);
        _channel.Writer.TryWrite(profileImageChannelModel);

        return profileImageChannelModel;
    }

    public ValueTask<ProfileImageChannelModel> ReadAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}
