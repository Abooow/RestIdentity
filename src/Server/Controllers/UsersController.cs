using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RestIdentity.Server.Constants;
using RestIdentity.Server.Models;
using RestIdentity.Server.Models.DAO;
using RestIdentity.Server.Services.Activity;
using RestIdentity.Server.Services.Cookies;
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
    private readonly IActivityService _activityService;
    private readonly ICookieService _cookieService;
    private readonly IServiceProvider _serviceProvider;
    private readonly DataProtectionKeys _dataProtectionKeys;
    private readonly JwtSettings _jwtSettings;

    public UsersController(IUserService userService,
        IActivityService activityService,
        ICookieService cookieService,
        IServiceProvider serviceProvider,
        IOptions<DataProtectionKeys> dataProtectionKeys,
        IOptions<JwtSettings> jwtSettings)
    {
        _userService = userService;
        _activityService = activityService;
        _cookieService = cookieService;
        _serviceProvider = serviceProvider;
        _dataProtectionKeys = dataProtectionKeys.Value;
        _jwtSettings = jwtSettings.Value;
    }

    [Authorize]
    [HttpGet("getMe")]
    public async Task<IActionResult> GetMe()
    {
        PersonalUserProfile userProfile = await _userService.GetLoggedInUserProfileAsync();

        return userProfile is null
            ? Unauthorized(Result<PersonalUserProfile>.Fail("Could not get User.").AsUnauthorized())
            : Ok(Result<PersonalUserProfile>.Success(userProfile));
    }

    [AllowAnonymous]
    [HttpGet("getUserProfile/{userName}")]
    public async Task<IActionResult> GetUserProfile(string userName)
    {
        UserProfile userProfile = await _userService.GetUserProfileByNameAsync(userName);

        return userProfile is null
            ? NotFound(Result<UserProfile>.Fail("Could not find User.").AsNotFound())
            : Ok(Result<UserProfile>.Success(userProfile));
    }

    [Authorize(AuthenticationSchemes = RolesConstants.Admin)]
    [HttpGet("getUser/{id}")]
    public async Task<IActionResult> GetUser(string id)
    {
        PersonalUserProfile userProfile = await _userService.GetUserProfileByIdAsync(id);

        return userProfile is null
            ? NotFound(Result<PersonalUserProfile>.Fail("Could not find User.").AsNotFound())
            : Ok(Result<PersonalUserProfile>.Success(userProfile));
    }

    [Authorize]
    [HttpGet("getMyActivity")]
    public async Task<IActionResult> GetMyActivity()
    {
        string userId = _userService.GetLoggedInUserId();
        if (userId is null)
            return BadRequest(Result<IEnumerable<UserActivity>>.Fail(""));

        IEnumerable<ActivityModel> activities = await _activityService.GetPartialUserActivityAsync(userId);

        return Ok(Result<IEnumerable<UserActivity>>.Success(MapActivities(activities)));
    }

    [Authorize(AuthenticationSchemes = RolesConstants.Admin)]
    [HttpGet("getUserActivity/{id}")]
    public async Task<IActionResult> GetUserActivity(string id)
    {
        IEnumerable<ActivityModel> activities = await _activityService.GetFullUserActivityAsync(id);

        return Ok(Result<IEnumerable<UserActivity>>.Success(MapActivities(activities)));
    }

    [Authorize(AuthenticationSchemes = RolesConstants.Admin)]
    [HttpPost("update-myProfile")]
    public async Task<IActionResult> UpdateMyProfile(UpdateProfileRequest updateProfileRequest)
    {
        IdentityUserResult updateProfileResult = await _userService.UpdateSignedInUserProfileAsync(updateProfileRequest);

        return updateProfileResult.Succeeded
            ? Ok(Result.Success())
            : BadRequest(Result.Fail(updateProfileResult.Errors.Select(x => x.Description)));
    }

    private static IEnumerable<UserActivity> MapActivities(IEnumerable<ActivityModel> activities)
    {
        foreach (ActivityModel activity in activities)
        {
            yield return new UserActivity(activity.Type, activity.IpAddress, activity.Location, activity.Date);
        }
    }
}
