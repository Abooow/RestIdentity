﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestIdentity.Server.Constants;
using RestIdentity.Server.Models;
using RestIdentity.Server.Models.DAO;
using RestIdentity.Server.Services.Activity;
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

    public UsersController(
        IUserService userService,
        IActivityService activityService)
    {
        _userService = userService;
        _activityService = activityService;
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
    [HttpGet("my-activity")]
    public async Task<IActionResult> GetMyActivity()
    {
        string userId = _userService.GetSignedInUserId();
        if (userId is null)
            return BadRequest(Result<IEnumerable<UserActivity>>.Fail(""));

        (_, IEnumerable<ActivityModel> activities) = await _activityService.GetPartialUserActivitiesAsync(userId);

        return Ok(Result<IEnumerable<UserActivity>>.Success(MapActivities(activities)));
    }

    [Authorize(AuthenticationSchemes = RolesConstants.Admin)]
    [HttpGet("activities/{id}")]
    public async Task<IActionResult> GetUserActivity(string id)
    {
        (bool userFound, IEnumerable<ActivityModel> activities) = await _activityService.GetFullUserActivitiesAsync(id);

        return userFound
            ? Ok(Result<IEnumerable<UserActivity>>.Success(MapActivities(activities)))
            : BadRequest(Result<IEnumerable<UserActivity>>.Fail("User not found."));
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

    private static IEnumerable<UserActivity> MapActivities(IEnumerable<ActivityModel> activities)
    {
        foreach (ActivityModel activity in activities)
        {
            yield return new UserActivity(activity.Type, activity.IpAddress, activity.Location, activity.Date);
        }
    }
}
