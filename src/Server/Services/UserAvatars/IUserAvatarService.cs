using System.Drawing.Drawing2D;
using RestIdentity.Server.Models.Channels;
using RestIdentity.Server.Models.DAO;
using RestIdentity.Shared.Wrapper;

namespace RestIdentity.Server.Services.UserAvatars;

public interface IUserAvatarService
{
    Task<UserAvatarModel> FindByIdAsync(string userId);
    Task<UserAvatarModel> FindByUserNameAsync(string userName);

    Task CreateDefaultAvatarAsync(ApplicationUser user);
    ValueTask<(string Location, string NormalizedContentType)> GetImageFileLocationAsync(string userHash, string contentType, int? size);
    Task<Result<UserAvatarChannelModel>> UploadAvatarForSignedInUserAsync(IFormFile file, InterpolationMode interpolationMode);
    Task RemoveAvatarForSignedInUserAsync();
    Task RemoveAvatarAsync(string userId);

    Task CreateFromChannelAsync(UserAvatarChannelModel userAvatarChannelModel);
}
