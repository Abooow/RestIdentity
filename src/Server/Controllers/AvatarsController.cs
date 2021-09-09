﻿using System.Drawing.Drawing2D;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestIdentity.Server.Constants;
using RestIdentity.Server.Models.Channels;
using RestIdentity.Server.Services.ProfileImage;
using RestIdentity.Shared.Wrapper;
using Serilog;

namespace RestIdentity.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AvatarsController : ControllerBase
{
    private readonly IProfileImageService _profileImageService;

    public AvatarsController(IProfileImageService profileImageService)
    {
        _profileImageService = profileImageService;
    }

    [AllowAnonymous]
    [HttpGet("{url}")]
    public async Task<IActionResult> GetProfileImage(string url, [FromQuery] int? size)
    {
        try
        {
            string userNameHash = Path.GetFileNameWithoutExtension(url);
            string contentType = Path.GetExtension(url);
            (string filePath, string normalizedContentType) = await _profileImageService.GetPhysicalFileLocation(userNameHash, contentType, size);

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
    public async Task<IActionResult> UploadImage(IFormFile file, [FromQuery] string interpolation)
    {
        if (file is null)
            return BadRequest(Result<ProfileImageChannelModel>.Fail("No file attached.").WithDescription(StatusCodeDescriptions.FileNotFound));

        if (!Enum.TryParse(interpolation, out InterpolationMode interpolationMode) || interpolationMode == InterpolationMode.Invalid)
            interpolationMode = InterpolationMode.HighQualityBicubic;

        Result<ProfileImageChannelModel> profileImageResult = await _profileImageService.UploadProfileImageForSignedInUserAsync(file, interpolationMode);

        if (!profileImageResult.Succeeded)
        {
            Log.Information("Failed to upload image");
            return BadRequest(profileImageResult with { Data = null });
        }

        Log.Information("Profile Image Uploaded, Id: {id}", profileImageResult.Data.Id);
        return Ok(profileImageResult with { Data = null });
    }
}
