using RestIdentity.Server.BackgroundServices.Channels;
using RestIdentity.Server.Models.Channels;
using RestIdentity.Server.Services.UserAvatars;

namespace RestIdentity.Server.BackgroundServices;

internal sealed class UserAvatarDispatcher : BackgroundService
{
    private readonly ILogger<UserAvatarDispatcher> _logger;
    private readonly IServiceProvider _provider;
    private readonly UserAvatarChannel _userAvatarChannel;

    public UserAvatarDispatcher(ILogger<UserAvatarDispatcher> logger, IServiceProvider provider, UserAvatarChannel userAvatarChannel)
    {
        _logger = logger;
        _provider = provider;
        _userAvatarChannel = userAvatarChannel;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Waiting for new User Avatar");
                UserAvatarChannelModel userAvatarChannelModel = await _userAvatarChannel.ReadAsync(stoppingToken);

                _logger.LogInformation("User Avatar received with Id: {id}, waiting for processing...", userAvatarChannelModel.Id);
                using (IServiceScope scope = _provider.CreateScope())
                {
                    var userAvatarService = scope.ServiceProvider.GetRequiredService<IUserAvatarService>();
                    await userAvatarService.CreateFromChannelAsync(userAvatarChannelModel);
                }

                _logger.LogInformation("User Avatar with Id: {id} has completed processing successfully", userAvatarChannelModel.Id);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while processing a User Avatar");
            }
        }
    }
}
