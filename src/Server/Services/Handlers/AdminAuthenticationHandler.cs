using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RestIdentity.Server.Constants;
using RestIdentity.Server.Data;
using RestIdentity.Server.Models;
using Serilog;

namespace RestIdentity.Server.Services.Handlers;

public sealed class AdminAuthenticationHandler : AuthenticationHandler<AdminAuthenticationOptions>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly DataProtectionKeys _dataProtectionKeys;
    private readonly JwtSettings _jwtSettings;

    public AdminAuthenticationHandler(
        IOptionsMonitor<AdminAuthenticationOptions> options, ILoggerFactory loggerFactory, UrlEncoder urlEncoder, ISystemClock systemClock,
        UserManager<ApplicationUser> userManager,
        IServiceProvider serviceProvider,
        IOptions<DataProtectionKeys> dataProtectionKeys,
        IOptions<JwtSettings> jwtSettings)
        : base(options, loggerFactory, urlEncoder, systemClock)
    {
        _userManager = userManager;
        _serviceProvider = serviceProvider;
        _dataProtectionKeys = dataProtectionKeys.Value;
        _jwtSettings = jwtSettings.Value;
    }

    protected async override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.ContainsKey(CookieConstants.AccessToken) || !Request.Cookies.ContainsKey(CookieConstants.UserId))
        {
            Log.Error("No Access Token or User Id was found.");
            return AuthenticateResult.NoResult();
        }

        if (!AuthenticationHeaderValue.TryParse($"Bearer {Request.Cookies[CookieConstants.AccessToken]}", out AuthenticationHeaderValue authHeaderValue))
        {
            Log.Error("Could not Parse Token from Authentication Header.");
            return AuthenticateResult.NoResult();
        }

        if (!AuthenticationHeaderValue.TryParse($"Bearer {Request.Cookies[CookieConstants.UserId]}", out AuthenticationHeaderValue userIdHeaderValue))
        {
            Log.Error("Could not Parse User Id from Authentication Header.");
            return AuthenticateResult.NoResult();
        }

        try
        {
            byte[] secret = Encoding.ASCII.GetBytes(_jwtSettings.Secret);
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = _jwtSettings.ValidateIssuerSigningKey,
                    ValidateIssuer = _jwtSettings.ValidateIssuer,
                    ValidateAudience = _jwtSettings.ValidateAudience,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidAudience = _jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(secret),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

            var protectionProvider = _serviceProvider.GetService<IDataProtectionProvider>();
            var protector = protectionProvider.CreateProtector(_dataProtectionKeys.ApplicationUserKey);

            string decryptedUserId = protector.Unprotect(userIdHeaderValue.Parameter);
            string decryptedToken = protector.Unprotect(authHeaderValue.Parameter);

            var tokenModel = new TokenModel();
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var tokenQuery = from u in dbContext.Tokens.Include(x => x.User)
                                 from ur in dbContext.UserRoles
                                 where u.UserId == ur.UserId && ur.RoleId == RolesConstants.AdminId
                                    && u.UserId == decryptedUserId
                                    && u.User.UserName == Request.Cookies[CookieConstants.UserName]
                                 select u;

                tokenModel = await tokenQuery.FirstOrDefaultAsync();
            }

            if (tokenModel is null)
                return AuthenticateResult.Fail("You are not Authorized");

            IDataProtector layerTwoProtector = protectionProvider.CreateProtector(tokenModel.EncryptionKeyJwt.ToString());
            string decryptedLayerTwoToken = layerTwoProtector.Unprotect(decryptedToken);

            var validateToken = tokenHandler.ValidateToken(decryptedLayerTwoToken, validationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken
                || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return AuthenticateResult.Fail("You are not Authorized");
            }

            string username = validateToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            if (Request.Cookies[CookieConstants.UserName] != username)
                return AuthenticateResult.Fail("You are not Authorized");

            ApplicationUser user = await _userManager.FindByNameAsync(username);
            if (user is null || !await _userManager.IsInRoleAsync(user, RolesConstants.AdminId))
                return AuthenticateResult.Fail("You are not Authorized");

            var identity = new ClaimsIdentity(validateToken.Claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var authTicket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(authTicket);
        }
        catch (Exception e)
        {
            Log.Error("An error occurred while decrypting user Tokens. {Error} {StackTrace} {InnerException} {Source}",
                e.Message, e.StackTrace, e.InnerException, e.Source);
            return AuthenticateResult.Fail("You are not Authorized");
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Cookies.Delete(CookieConstants.AccessToken);
        Response.Cookies.Delete(CookieConstants.UserId);
        Response.Headers["WWW-Authentication"] = "Not Authorized";

        return Task.CompletedTask;
    }
}
