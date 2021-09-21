using System.Drawing.Drawing2D;
using RestIdentity.DataAccess.Models;
using RestIdentity.Server.Models.Channels;
using RestIdentity.Shared.Wrapper;

namespace RestIdentity.Server.Services.UserAvatars;

public interface IUserAvatarService
{
    Task<UserAvatarDao> FindByUserIdAsync(string userId);
    Task<UserAvatarDao> FindByUserNameAsync(string userName);
    Task<UserAvatarDao> FindByAvatarHashAsync(string avatarHash);

    Task CreateDefaultAvatarAsync(UserDao user);
    ValueTask<(string Location, string NormalizedContentType)> GetImageFileLocationAsync(string userHash, string contentType, int? size);
    Task<Result<UserAvatarChannelModel>> UploadAvatarForSignedInUserAsync(IFormFile file, InterpolationMode interpolationMode);
    Task<Result> RemoveAvatarForSignedInUserAsync();
    Task<Result> RemoveAvatarAsync(string userId);

    Task CreateFromChannelAsync(UserAvatarChannelModel userAvatarChannelModel);
}
