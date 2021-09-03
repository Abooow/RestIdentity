namespace RestIdentity.Server.Models.Channels;

public abstract class ChannelModel
{
    public Guid Id { get; init; }
    public DateTime DateRequested { get; init; }
};
