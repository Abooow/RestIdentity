using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestIdentity.DataAccess;
using RestIdentity.DataAccess.Models;
using RestIdentity.Server.Models;
using RestIdentity.Server.Services.AuditLog;
using RestIdentity.Server.Services.User;
using RestIdentity.Shared.Models;
using RestIdentity.Shared.Models.Requests;
using RestIdentity.Shared.Models.Response;
using RestIdentity.Shared.Wrapper;

namespace RestIdentity.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuditLogService _auditLogService;

    public UsersController(IUserService userService, IAuditLogService auditLogService)
    {
        _userService = userService;
        _auditLogService = auditLogService;
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        PersonalUserProfile userProfile = await _userService.GetLoggedInUserProfileAsync();

        return userProfile is null
            ? Unauthorized(Result<PersonalUserProfile>.Fail("Could not get User.").AsUnauthorized())
            : Ok(Result<PersonalUserProfile>.Success(userProfile));
    }

    [AllowAnonymous]
    [HttpGet("profiles/{userName}")]
    public async Task<IActionResult> GetUserProfile(string userName)
    {
        UserProfile userProfile = await _userService.GetUserProfileByNameAsync(userName);

        return userProfile is null
            ? NotFound(Result<UserProfile>.Fail("Could not find User.").AsNotFound())
            : Ok(Result<UserProfile>.Success(userProfile));
    }

    [Authorize(AuthenticationSchemes = RolesConstants.Admin)]
    [HttpGet("full-profiles/{id}")]
    public async Task<IActionResult> GetUser(string id)
    {
        PersonalUserProfile userProfile = await _userService.GetUserProfileByIdAsync(id);

        return userProfile is null
            ? NotFound(Result<PersonalUserProfile>.Fail("Could not find User.").AsNotFound())
            : Ok(Result<PersonalUserProfile>.Success(userProfile));
    }

    [Authorize]
    [HttpGet("my-auditLogs")]
    public async Task<IActionResult> GetMyAuditLog()
    {
        string userId = _userService.GetSignedInUserId();
        if (userId is null)
            return BadRequest(Result<IEnumerable<UserAuditLog>>.Fail(""));

        (_, IEnumerable<AuditLogRecord> auditLogs) = await _auditLogService.GetPartialAuditLogsAsync(userId);

        return Ok(Result<IEnumerable<UserAuditLog>>.Success(MapAuditLogs(auditLogs)));
    }

    [Authorize(AuthenticationSchemes = RolesConstants.Admin)]
    [HttpGet("auditLogs/{id}")]
    public async Task<IActionResult> GetUserAuditLog(string id)
    {
        (bool userFound, IEnumerable<AuditLogRecord> auditLogs) = await _auditLogService.GetFullAuditLogsAsync(id);

        return userFound
            ? Ok(Result<IEnumerable<UserAuditLog>>.Success(MapAuditLogs(auditLogs)))
            : BadRequest(Result<IEnumerable<UserAuditLog>>.Fail("User not found."));
    }

    [Authorize]
    [HttpPost("update-myProfile")]
    public async Task<IActionResult> UpdateMyProfile(UpdateProfileRequest updateProfileRequest)
    {
        IdentityUserResult updateProfileResult = await _userService.UpdateSignedInUserProfileAsync(updateProfileRequest);

        return updateProfileResult.Succeeded
            ? Ok(Result.Success())
            : BadRequest(Result.Fail(updateProfileResult.Errors.Select(x => x.Description)));
    }

    private static IEnumerable<UserAuditLog> MapAuditLogs(IEnumerable<AuditLogRecord> auditLogs)
    {
        foreach (AuditLogRecord auditLog in auditLogs)
        {
            yield return new UserAuditLog(auditLog.Type, auditLog.IpAddress, auditLog.Date);
        }
    }
}
