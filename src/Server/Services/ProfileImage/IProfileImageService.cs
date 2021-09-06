using System.Drawing.Drawing2D;
using RestIdentity.Server.Models.Channels;
using RestIdentity.Server.Models.DAO;
using RestIdentity.Shared.Wrapper;

namespace RestIdentity.Server.Services.ProfileImage;

public interface IProfileImageService
{
    Task<string> CreateDefaultProfileImageAsync(ApplicationUser user);
    (string Location, string ActualContentType) GetPhysicalFileLocation(string userNameHash, string contentType, int? size);
    Task<Result<ProfileImageChannelModel>> UploadProfileImageForSignedInUserAsync(IFormFile file, InterpolationMode interpolationMode);
    Task RemoveProfileImageForSignedInUserAsync();
    Task RemoveProfileImageAsync(string userId);
    Task CreateFromChannelAsync(ProfileImageChannelModel profileImage);
}
