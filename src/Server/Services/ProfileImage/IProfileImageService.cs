using RestIdentity.Server.Models.DAO;

namespace RestIdentity.Server.Services.ProfileImage;

internal interface IProfileImageService
{
    Task<string> CreateDefaultProfileImage(ApplicationUser user);
}
