using RestIdentity.Server.Models.DAO;

namespace RestIdentity.Server.Services.ProfileImage;

internal sealed class ProfileImageService : IProfileImageService
{
    public Task<string> CreateDefaultProfileImageAsync(ApplicationUser user)
    {
        return Task.FromResult(string.Empty);
    }
}
