using RestIdentity.Server.Models;
using RestIdentity.Shared.Models;
using RestIdentity.Shared.Wrapper;
using RestIdentity.Shared.Models.Requests;
using RestIdentity.Shared.Models.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Identity = Microsoft.AspNetCore.Identity;

namespace RestIdentity.Server.Controllers
{
    public partial class AuthController
    {
        [AllowAnonymous]
        [HttpPost("loginWithTwoFactor")]
        public async Task<IActionResult> LoginWithTwoFactor(
            [FromHeader(Name = "Identity.TwoFactorUserId")][Required(ErrorMessage = "The header Identity.TwoFactorUserId is requred")] string _,
            [FromBody] LoginWith2faRequest request)
        {
            ApplicationUser user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user is null)
                return NotFound(Result.Fail("Unable to load two-factor authentication user.").AsNotFound());

            string authenticatorCode = request.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);
            Identity::SignInResult result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, request.RememberMe, request.RememberMachine);

            return result.Succeeded ? Ok(Result.Success()) 
                : BadRequest(Result.Fail("Invalid authenticator code.").AsBadRequest());
        }

        [Authorize]
        [HttpPost("enableTwoFactorAuth")]
        public async Task<IActionResult> EnableTwoFactorAuth()
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            if (user is null)
                return NotFound(Result.Fail($"Unable to load user.").AsNotFound());

            await _userManager.SetTwoFactorEnabledAsync(user, true);

            string unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }
            
            string email = await _userManager.GetEmailAsync(user);
            string formattedKey = FormatKey(unformattedKey);
            string authenticatorUri = GenerateQrCodeUri(email, unformattedKey);

            return Ok(Result<TwoFactorQRCode>.Success(
                new TwoFactorQRCode() { SharedKey = formattedKey, AuthenticatorUri = authenticatorUri },
                "2fa has been enabled."));
        }

        [Authorize]
        [HttpPost("disableTwoFactorAuth")]
        public async Task<IActionResult> DisableTwoFactorAuth()
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            if (user is null)
                return NotFound(Result.Fail($"Unable to load user.").AsNotFound());

            if (!await _userManager.GetTwoFactorEnabledAsync(user))
                return BadRequest(Result.Fail($"Can not disable 2fa on this user as it's not currently enabled.").AsBadRequest());

            Identity::IdentityResult result = await _userManager.SetTwoFactorEnabledAsync(user, false);

            return result.Succeeded ? Ok(Result.Success("2fa has been disabled.")) 
                : BadRequest(Result.Fail(result.Errors.Select(x => x.Description)).AsBadRequest());
        }

        [Authorize]
        [HttpPost("resetAuthenticator")]
        public async Task<IActionResult> ResetAuthenticator()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
                return NotFound(Result.Fail($"Unable to load user.").AsNotFound());

            await _userManager.SetTwoFactorEnabledAsync(user, false);
            await _userManager.ResetAuthenticatorKeyAsync(user);

            await _signInManager.RefreshSignInAsync(user);

            return Ok(Result.Success("Authenticator has been reset."));
        }

        [Authorize]
        [HttpGet("generateRecoveryCodes")]
        public async Task<IActionResult> GenerateRecoveryCodes()
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            if (user is null)
                return NotFound(Result.Fail("Unable to load user.").AsNotFound());

            if (!await _userManager.GetTwoFactorEnabledAsync(user))
                return BadRequest($"Cannot generate recovery codes for user as they do not have 2FA enabled.");

            IEnumerable<string> recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

            return Ok(Result<RecoveryCodes>.Success(
                new RecoveryCodes() { Codes = recoveryCodes },
                "Recovery codes has been generated."));
        }

        [AllowAnonymous]
        [HttpPost("loginWithRecoveryCode")]
        public async Task<IActionResult> LoginWithRecoveryCode(
            [FromHeader(Name = "Identity.TwoFactorUserId")][Required(ErrorMessage = "The header Identity.TwoFactorUserId is requred")] string _,
            LoginWithRecoveryCodeRequest loginWithRecoveryRequest)
        {
            ApplicationUser user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user is null)
                return BadRequest(Result.Fail("Unable to load two-factor authentication user.").AsBadRequest());

            string recoveryCode = loginWithRecoveryRequest.RecoveryCode.Replace(" ", string.Empty);
            Identity::SignInResult result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

            return result.Succeeded ? Ok() 
                : BadRequest(Result.Fail("Invalid recovery code entered.").AsBadRequest());
        }

        private string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            int currentPosition = 0;
            while (currentPosition + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition, 4)).Append(" ");
                currentPosition += 4;
            }
            if (currentPosition < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition));
            }

            return result.ToString().ToLowerInvariant();
        }

        private string GenerateQrCodeUri(string email, string unformattedKey)
        {
            const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

            return string.Format(
                AuthenticatorUriFormat,
                _urlEncoder.Encode("IdentityTest"),
                _urlEncoder.Encode(email),
                unformattedKey);
        }
    }
}
