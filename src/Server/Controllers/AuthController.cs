using RestIdentity.Server.Models;
using RestIdentity.Shared.Models;
using RestIdentity.Shared.Models.Requests;
using RestIdentity.Shared.Wrapper;
using RestIdentity.Server.Services.EmailSenders;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Text.Encodings.Web;
using Identity = Microsoft.AspNetCore.Identity;
using Wrapper = RestIdentity.Shared.Wrapper;

namespace RestIdentity.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public partial class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly UrlEncoder _urlEncoder;

        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IEmailSender emailSender, UrlEncoder urlEncoder)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _urlEncoder = urlEncoder;
        }

        [Authorize]
        [HttpGet("getMe")]
        public async Task<IActionResult> GetMe()
        {
            Result<CurrentUser> resultUser = await Result<CurrentUser>.SuccessAsync(new CurrentUser
            {
                IsAuthenticated = User.Identity.IsAuthenticated,
                Email = User.Identity.Name,
                Claims = User.Claims
                .ToDictionary(c => c.Type, c => c.Value)
            });
            return Ok(resultUser);
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest parameters)
        {
            ApplicationUser user = new ApplicationUser
            {
                UserName = parameters.Email,
                Email = parameters.Email
            };
            IdentityResult result = await _userManager.CreateAsync(user, parameters.Password);
            
            if (!result.Succeeded)
                return BadRequest(Result.Fail(result.Errors.Select(x => x.Description)).AsBadRequest());

            await GenerateAndSendConfirmationEmail(user);

            return Ok(Result.Success($"User {user.Email} Registered. Please check your Mailbox to verify!"));
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            Identity::SignInResult singInResult = await _signInManager.PasswordSignInAsync(request.Email, request.Password, request.RememberMe, false);
            
            if (singInResult.IsNotAllowed)
                return BadRequest(Result.Fail("Please confirm your email before continuing.").AsBadRequest());

            if (singInResult.RequiresTwoFactor)
                return Ok(Wrapper::RedirectResult.RedirectTo(Url.RouteUrl("loginWithTwoFactor")));

            if (!singInResult.Succeeded)
                return BadRequest(Result.Fail("Invalid Credentials.").AsBadRequest());

            return Ok(Result.Success());
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
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
}