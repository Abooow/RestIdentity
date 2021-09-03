using RestIdentity.Server.BackgroundServices.Channels;
using RestIdentity.Server.Models.Channels;
using RestIdentity.Server.Services.ProfileImage;

namespace RestIdentity.Server.BackgroundServices;

internal sealed class ProfileImageDispatcher : BackgroundService
{
    private readonly ILogger<ProfileImageDispatcher> _logger;
    private readonly IProfileImageService _profileImageService;
    private readonly ProfileImageChannel _profileImageChannel;

    public ProfileImageDispatcher(ILogger<ProfileImageDispatcher> logger, IProfileImageService profileImageService, ProfileImageChannel profileImageChannel)
    {
        _logger = logger;
        _profileImageService = profileImageService;
        _profileImageChannel = profileImageChannel;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Waiting for new ProfileImage");
                ProfileImageChannelModel profileImageModel = await _profileImageChannel.ReadAsync(stoppingToken);

                _logger.LogInformation("ProfileImage received with Id: {id}, waiting for processing...", profileImageModel.Id);
                await _profileImageService.Upload(profileImageModel);

                _logger.LogInformation("ProfileImage with Id: {id} has completed processing successfully", profileImageModel.Id);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while processing a ProfileImage");
            }
        }
    }
}
