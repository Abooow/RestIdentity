using System.Drawing.Drawing2D;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestIdentity.Server.Constants;
using RestIdentity.Server.Models.Channels;
using RestIdentity.Server.Services.UserAvatars;
using RestIdentity.Shared.Wrapper;
using Serilog;

namespace RestIdentity.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AvatarsController : ControllerBase
{
    private readonly IUserAvatarService _userAvatarService;

    public AvatarsController(IUserAvatarService userAvatarService)
    {
        _userAvatarService = userAvatarService;
    }

    [AllowAnonymous]
    [HttpGet("{url}")]
    public async Task<IActionResult> GetUserAvatar(string url, [FromQuery] int? size)
    {
        try
        {
            string userHash = Path.GetFileNameWithoutExtension(url);
            string contentType = Path.GetExtension(url);
            (string filePath, string normalizedContentType) = await _userAvatarService.GetImageFileLocationAsync(userHash, contentType, size);

            // TODO: Convert the image type to normalizedContentType.
            return PhysicalFile(filePath, $"image/{normalizedContentType}");
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }

    [Authorize]
    [HttpPost("upload")]
    public async Task<IActionResult> UploadUserAvatar(IFormFile file, [FromQuery] string interpolation)
    {
        if (file is null)
            return BadRequest(Result<UserAvatarChannelModel>.Fail("No file attached.").WithDescription(StatusCodeDescriptions.FileNotFound));

        if (!Enum.TryParse(interpolation, out InterpolationMode interpolationMode) || interpolationMode == InterpolationMode.Invalid)
            interpolationMode = InterpolationMode.HighQualityBicubic;

        Result<UserAvatarChannelModel> userAvatarResult = await _userAvatarService.UploadAvatarForSignedInUserAsync(file, interpolationMode);

        if (!userAvatarResult.Succeeded)
        {
            Log.Information("Failed to upload image");
            return BadRequest(userAvatarResult with { Data = null });
        }

        Log.Information("User Avatar Uploaded, Id: {id}", userAvatarResult.Data.Id);
        return Ok(userAvatarResult with { Data = null });
    }

    [Authorize]
    [HttpDelete("remove")]
    public async Task<IActionResult> RemoveMyAvatar()
    {
        Result removeAvatarResult = await _userAvatarService.RemoveAvatarForSignedInUserAsync();

        return removeAvatarResult.Succeeded
            ? Ok(removeAvatarResult)
            : BadRequest(removeAvatarResult);
    }

    [Authorize(AuthenticationSchemes = RolesConstants.Admin)]
    [HttpDelete("remove/{userId}")]
    public async Task<IActionResult> RemoveUserAvatar(string userId)
    {
        Result removeAvatarResult = await _userAvatarService.RemoveAvatarAsync(userId);

        return removeAvatarResult.Succeeded
            ? Ok(removeAvatarResult)
            : BadRequest(removeAvatarResult);
    }
}
