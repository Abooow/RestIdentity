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
        UserProfile userProfile = await _userService.GetLoggedInUserProfile();

        return userProfile is null
            ? Unauthorized(Result<UserProfile>.Fail("Could not get User.").AsUnauthorized())
            : Ok(Result<UserProfile>.Success(userProfile));
    }

    [Authorize(AuthenticationSchemes = RolesConstants.Admin)]
    [HttpGet("getUser/{id}")]
    public async Task<IActionResult> GetUser(string id)
    {
        UserProfile userProfile = await _userService.GetUserProfileById(id);

        return userProfile is null
            ? NotFound(Result<UserProfile>.Fail("Could not find User.").AsNotFound())
            : Ok(Result<UserProfile>.Success(userProfile));
    }

    [Authorize]
    [HttpGet("getMyActivity")]
    public async Task<IActionResult> GetMyActivity()
    {
        string userId = _userService.GetLoggedInUserId();
        IEnumerable<ActivityModel> activities = await _activityService.GetPartialUserActivity(userId);

        return Ok(MapActivities(activities));
    }

    [Authorize(AuthenticationSchemes = RolesConstants.Admin)]
    [HttpGet("getUserActivity/{id}")]
    public async Task<IActionResult> GetUserActivity(string id)
    {
        IEnumerable<ActivityModel> activities = await _activityService.GetFullUserActivity(id);

        return Ok(MapActivities(activities));
    }

    private static IEnumerable<UserActivity> MapActivities(IEnumerable<ActivityModel> activities)
    {
        foreach (ActivityModel activity in activities)
        {
            yield return new UserActivity(activity.Type, activity.IpAddress, activity.Location, activity.Date);
        }
    }
}
