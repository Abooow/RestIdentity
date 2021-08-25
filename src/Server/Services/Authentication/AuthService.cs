using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RestIdentity.Server.Constants;
using RestIdentity.Server.Data;
using RestIdentity.Server.Models;
using RestIdentity.Server.Models.DAO;
using RestIdentity.Server.Services.Activity;
using RestIdentity.Server.Services.IpInfo;
using RestIdentity.Shared.Models.Requests;
using RestIdentity.Shared.Models.Response;
using RestIdentity.Shared.Wrapper;
using Serilog;

namespace RestIdentity.Server.Services.Authentication;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IdentityDefaultOptions _identityOptions;
    private readonly JwtSettings _jwtSettings;
    private readonly DataProtectionKeys _dataProtectionKeys;
    private readonly IIpInfoService _ipInfoService;
    private readonly IActivityService _activityService;
    private readonly IServiceProvider _provider;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IOptions<IdentityDefaultOptions> identityOptions,
        IOptions<JwtSettings> jwtSettings,
        IOptions<DataProtectionKeys> dataProtectionKeys,
        IIpInfoService ipInfoService,
        IActivityService activityService,
        IServiceProvider provider)
    {
        _userManager = userManager;
        _context = context;
        _identityOptions = identityOptions.Value;
        _jwtSettings = jwtSettings.Value;
        _activityService = activityService;
        _dataProtectionKeys = dataProtectionKeys.Value;
        _ipInfoService = ipInfoService;
        _provider = provider;
    }

    public async Task<Result<TokenResponse>> AuthenticateAsync(LoginRequest loginRequest)
    {
        var ipInfo = await _ipInfoService.GetIpInfo();
        var activity = new ActivityModel()
        {
            IpAddress = _ipInfoService.GetRemoteIpAddress(),
            Location = ipInfo.Country is null ? "unknown" : $"{ipInfo.Country}, {ipInfo.City}",
            OperationgSystem = _ipInfoService.GetRemoteOperatingSystem(),
            Date = DateTime.UtcNow
        };

        // Find User.
        ApplicationUser user = await _userManager.FindByEmailAsync(loginRequest.Email);
        if (user is null)
        {
            Log.Error("Could Not find User {Email}", loginRequest.Email);
            return Result<TokenResponse>.Fail("Invalid Email/Password.").AsBadRequest().WithDescription(StatusCodeDescriptions.InvalidCredentials);
        }
        activity.UserId = user.Id;

        // Check Password.
        if (!await _userManager.CheckPasswordAsync(user, loginRequest.Password))
        {

            activity.Type = ActivityConstants.AuthInvalidPassword;
            await _activityService.AddUserActivity(activity);

            Log.Error("Error: Invalid Password for {Email}", loginRequest.Email);
            return Result<TokenResponse>.Fail("Invalid Email/Password.").AsBadRequest().WithDescription(StatusCodeDescriptions.InvalidCredentials);
        }

        // Check Email Confirmed.
        if (_identityOptions.SignInRequreConfirmedEmail && !await _userManager.IsEmailConfirmedAsync(user))
        {
            activity.Type = ActivityConstants.AuthEmailNotConfirmed;
            await _activityService.AddUserActivity(activity);

            Log.Error("Error: Email Not Confirmed {Email}", loginRequest.Email);
            return Result<TokenResponse>.Fail("Email Not Confirmed.").WithDescription(StatusCodeDescriptions.RequiresConfirmEmail);
        }

        try
        {
            activity.Type = ActivityConstants.AuthSignedIn;
            await _activityService.AddUserActivity(activity);

            TokenResponse authToken = await CreateAuthTokenAsync(user);
            Log.Information("User {Email} Signed In", user.Email);
            return Result<TokenResponse>.Success(authToken);
        }
        catch (Exception e)
        {
            Log.Error("An error occurred while authenticating the user {Email}. {Error} {StackTrace} {InnerException} {Source}",
                loginRequest.Email, e.Message, e.StackTrace, e.InnerException, e.Source);
        }

        return Result<TokenResponse>.Fail("Request Not Supported.").AsUnauthorized();
    }

    private async Task<TokenResponse> CreateAuthTokenAsync(ApplicationUser user)
    {
        SymmetricSecurityKey key = new(Encoding.ASCII.GetBytes(_jwtSettings.Secret));
        IList<string> userRoles = await _userManager.GetRolesAsync(user);

        var tokenDescriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim("LoggedOn", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)),
            }.Concat(userRoles.Select(x => new Claim(ClaimTypes.Role, x)))),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            Expires = userRoles.Any(x => x == RolesConstants.Admin)
                ? DateTime.UtcNow.AddMinutes(60)
                : DateTime.UtcNow.AddMinutes(double.Parse(_jwtSettings.AccessTokenExpirationInMinutes))
        };

        var encryptionKeyJwt = Guid.NewGuid();
        var encryptionKeyRefreshToken = Guid.NewGuid();

        var protectorProvider = _provider.GetService<IDataProtectionProvider>();
        var protectorJwt = protectorProvider.CreateProtector(encryptionKeyJwt.ToString());

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        string encryptedToken = protectorJwt.Protect(tokenHandler.WriteToken(token));

        var refreshToken = new TokenModel()
        {
            ClientId = _jwtSettings.ClientId,
            UserId = user.Id,
            Value = Guid.NewGuid().ToString(),
            DateCreated = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddMinutes(double.Parse(_jwtSettings.RefreshTokenExpirationInMinutes)),
            EncryptionKeyJwt = encryptionKeyJwt,
            EncryptionKeyRefreshToken = encryptionKeyRefreshToken
        };

        try
        {
            var refreshTokens = await _context.Tokens.Where(x => x.UserId == user.Id).ToArrayAsync();

            if (refreshTokens.Length > 0)
                _context.RemoveRange(refreshTokens);

            _context.Add(refreshToken);

            await _context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Log.Error("An error occurred while removing and adding refresh tokens for {Email}. {Error} {StackTrace} {InnerException} {Source}",
                user.Email, e.Message, e.StackTrace, e.InnerException, e.Source);
        }

        var layerOneProtector = protectorProvider.CreateProtector(_dataProtectionKeys.ApplicationUserKey);
        var protectorRefreshToken = protectorProvider.CreateProtector(encryptionKeyRefreshToken.ToString());

        var encryptedAuthToken = new TokenResponse
        (
            layerOneProtector.Protect(encryptedToken),
            protectorRefreshToken.Protect(refreshToken.Value),
            token.ValidTo,
            refreshToken.ExpiryDate,
            userRoles,
            user.UserName,
            layerOneProtector.Protect(user.Id),
            false,
            null
        );

        return encryptedAuthToken;
    }
}
