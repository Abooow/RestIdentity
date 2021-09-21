using System.Drawing.Drawing2D;
using RestIdentity.DataAccess.Models;
using RestIdentity.Server.Models.Channels;
using RestIdentity.Shared.Wrapper;

namespace RestIdentity.Server.Services;

public interface IUserAvatarService
{
    Task<UserAvatarRecord> FindByUserIdAsync(string userId);
    Task<UserAvatarRecord> FindByUserNameAsync(string userName);
    Task<UserAvatarRecord> FindByAvatarHashAsync(string avatarHash);

    Task CreateDefaultAvatarAsync(UserRecord user);
    ValueTask<(string Location, string NormalizedContentType)> GetImageFileLocationAsync(string userHash, string contentType, int? size);
    Task<Result<UserAvatarChannelModel>> UploadAvatarForSignedInUserAsync(IFormFile file, InterpolationMode interpolationMode);
    Task<Result> RemoveAvatarForSignedInUserAsync();
    Task<Result> RemoveAvatarAsync(string userId);

    Task CreateFromChannelAsync(UserAvatarChannelModel userAvatarChannelModel);
}
