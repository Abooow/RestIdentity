﻿using System.Data;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RestIdentity.Server.Constants;
using RestIdentity.Server.Data;
using RestIdentity.Server.Models;
using RestIdentity.Server.Models.DAO;
using RestIdentity.Server.Services.Activity;
using RestIdentity.Server.Services.Authentication;
using RestIdentity.Server.Services.Cookies;
using RestIdentity.Server.Services.EmailSenders;
using RestIdentity.Shared.Models;
using RestIdentity.Shared.Models.Requests;
using RestIdentity.Shared.Models.Response;
using RestIdentity.Shared.Wrapper;
using Serilog;
using Identity = Microsoft.AspNetCore.Identity;

namespace RestIdentity.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed partial class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UrlEncoder _urlEncoder;
    private readonly JwtSettings _jwtSettings;
    private readonly DataProtectionKeys _dataProtectionKeys;
    private readonly IServiceProvider _serviceProvider;
    private readonly IEmailSender _emailSender;
    private readonly IActivityService _activityService;
    private readonly IAuthService _authService;
    private readonly ICookieService _cookieService;

    private readonly string[] _cookiesToDelete = new string[] { CookieConstants.AccessToken, CookieConstants.UserId, CookieConstants.UserName };

    public AuthController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        UrlEncoder urlEncoder,
        IOptions<JwtSettings> jwtSettings,
        IOptions<DataProtectionKeys> dataProtectionKeys,
        IServiceProvider serviceProvider,
        IEmailSender emailSender,
         IActivityService activityService,
        IAuthService authService,
        ICookieService cookieService)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _urlEncoder = urlEncoder;
        _jwtSettings = jwtSettings.Value;
        _dataProtectionKeys = dataProtectionKeys.Value;
        _serviceProvider = serviceProvider;
        _emailSender = emailSender;
        _activityService = activityService;
        _authService = authService;
        _cookieService = cookieService;
    }

    [Authorize]
    [HttpGet("getMe")]
    public IActionResult GetMe()
    {
        Result<CurrentUser> resultUser = Result<CurrentUser>.Success(new CurrentUser(
            User.Identity.Name,
            User.Claims.ToDictionary(c => c.Type, c => c.Value)));

        return Ok(resultUser);
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest registerRequest)
    {
        ApplicationUser user = new ApplicationUser
        {
            UserName = registerRequest.Email,
            Email = registerRequest.Email
        };
        IdentityResult result = await _userManager.CreateAsync(user, registerRequest.Password);

        if (!result.Succeeded)
            return BadRequest(Result.Fail(result.Errors.Select(x => x.Description)).AsBadRequest());

        await GenerateAndSendConfirmationEmail(user);

        return Ok(Result.Success($"User {user.Email} Registered. Please check your Mailbox to verify!"));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest loginRequest)
    {
        Result<TokenResponse> jwtTokenResult = await _authService.AuthenticateAsync(loginRequest);
        if (jwtTokenResult.StatusCodeDescription == StatusCodeDescriptions.RequiresConfirmEmail)
            return Ok(jwtTokenResult);
        else if (!jwtTokenResult.Succeeded)
            return BadRequest(jwtTokenResult);

        TokenResponse jwtToken = jwtTokenResult.Data;
        _cookieService.SetCookie(CookieConstants.AccessToken, jwtToken.Token, jwtToken.ExpirationDate);
        _cookieService.SetCookie(CookieConstants.UserId, jwtToken.UserId, jwtToken.ExpirationDate);
        _cookieService.SetCookie(CookieConstants.UserName, jwtToken.Username, jwtToken.ExpirationDate);

        return Ok(jwtTokenResult);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        string protectedUserId = _cookieService.GetCookie(CookieConstants.UserId);
        _cookieService.DeleteCookies(_cookiesToDelete);

        if (protectedUserId is null)
        {
            Log.Warning("No user_id Cookie found when logging out.");
            return Ok(Result.Success());
        }

        IDataProtectionProvider protectorProvider = _serviceProvider.GetService<IDataProtectionProvider>();
        IDataProtector protector = protectorProvider.CreateProtector(_dataProtectionKeys.ApplicationUserKey);
        string userId = protector.Unprotect(protectedUserId);

        TokenModel token = _context.Tokens.FirstOrDefault(x => x.UserId == userId);
        if (token is not null)
        {
            _context.Remove(token);
            await _context.SaveChangesAsync();
        }
        else
            Log.Warning("Invalid userId was detected from user_id Cookie");

        Log.Information("User logged out.");
        return Ok(Result.Success());
    }

    [AllowAnonymous]
    [HttpPost("confirmEmail")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string code)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
            return BadRequest(Result.Fail("User Id and Code are required.").AsBadRequest());

        ApplicationUser user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound(Result.Fail("User was not found.").AsNotFound());

        if (user.EmailConfirmed)
            return Ok(Result.Success("Account has been confirmed."));

        IdentityResult result = await _userManager.ConfirmEmailAsync(user, code);

        return result.Succeeded ? Ok(Result.Success("Account has been confirmed."))
            : BadRequest(Result.Fail(result.Errors.Select(x => x.Description)).AsBadRequest());
    }

    [AllowAnonymous]
    [HttpPost("resendEmailConfirmation")]
    public async Task<IActionResult> ResendEmailConfirmation(EmailAddress emailAddress)
    {
        ApplicationUser user = await _userManager.FindByEmailAsync(emailAddress.Email);
        if (user is null || user.EmailConfirmed)
            return Ok(Result.Success("Reset Password email sent. Please check your Mailbox.")); // Fake it.

        await GenerateAndSendConfirmationEmail(user);

        return Ok(Result.Success("Reset Password email sent. Please check your Mailbox."));
    }

    [Authorize]
    [HttpPost("changePassword")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest changePasswordRequest)
    {
        ApplicationUser user = await _userManager.GetUserAsync(User);
        if (user is null)
            return NotFound(Result.Fail("User was not found.").AsNotFound());

        IdentityResult result = await _userManager.ChangePasswordAsync(user, changePasswordRequest.OldPassword, changePasswordRequest.NewPassword);
        if (!result.Succeeded)
            return BadRequest(Result.Fail(result.Errors.Select(x => x.Description)).AsBadRequest());

        await _signInManager.RefreshSignInAsync(user);
        return Ok(Result.Success("Password was successfully changed."));
    }

    [AllowAnonymous]
    [HttpPost("forgotPassword")]
    public async Task<IActionResult> ForgotPassword(EmailAddress emailAddress)
    {
        ApplicationUser user = await _userManager.FindByEmailAsync(emailAddress.Email);
        if (user is null || !await _userManager.IsEmailConfirmedAsync(user))
            return Ok(Result.Success("Verification email sent. Please check your Mailbox.")); // Fake it.

        await GenerateAndSendPasswordResetEmail(user);

        return Ok(Result.Success("Verification email sent. Please check your Mailbox."));
    }

    [AllowAnonymous]
    [HttpPost("resetPassword")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest resetPasswordRequest)
    {
        ApplicationUser user = await _userManager.FindByEmailAsync(resetPasswordRequest.Email);
        if (user is null)
            return Ok(Result.Success("Password has been reset.")); // Fake it.

        IdentityResult result = await _userManager.ResetPasswordAsync(user, resetPasswordRequest.Code, resetPasswordRequest.Password);

        return result.Succeeded ? Ok(Result.Success("Password has been reset."))
            : BadRequest(Result.Fail(result.Errors.Select(x => x.Description)).AsBadRequest());
    }

    private async Task GenerateAndSendConfirmationEmail(ApplicationUser user)
    {
        string code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        string callbackUrl = Url.ActionLink("confirmEmail", "Auth", new { UserId = user.Id, Code = code });

        await _emailSender.SendAsync(user.Email, "Confirm Your account", $"Please confirm your account by <a href='{callbackUrl}'>Clicking Here</a>");
    }

    private async Task GenerateAndSendPasswordResetEmail(ApplicationUser user)
    {
        string code = await _userManager.GeneratePasswordResetTokenAsync(user);
        string callbackUrl = Url.ActionLink("resetPassword", "Auth", new { Area = "Identity", Code = code });

        await _emailSender.SendAsync(user.Email, "Reset Password", $"Reset your password by <a href='{callbackUrl}'>Clicking Here</a>");
    }
}
