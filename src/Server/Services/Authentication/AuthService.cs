using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RestIdentity.DataAccess;
using RestIdentity.DataAccess.Models;
using RestIdentity.DataAccess.Repositories;
using RestIdentity.Server.Constants;
using RestIdentity.Server.Models;
using RestIdentity.Server.Services.AuditLog;
using RestIdentity.Shared.Models.Requests;
using RestIdentity.Shared.Models.Response;
using RestIdentity.Shared.Wrapper;
using Serilog;

namespace RestIdentity.Server.Services.Authentication;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<UserDao> _userManager;
    private readonly ITokensRepository _tokensRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IdentityDefaultOptions _identityOptions;
    private readonly JwtSettings _jwtSettings;
    private readonly DataProtectionKeys _dataProtectionKeys;
    private readonly IServiceProvider _provider;

    public AuthService(
        UserManager<UserDao> userManager,
        ITokensRepository tokensRepository,
        IAuditLogService auditLogService,
        IOptions<IdentityDefaultOptions> identityOptions,
        IOptions<JwtSettings> jwtSettings,
        IOptions<DataProtectionKeys> dataProtectionKeys,
        IServiceProvider provider)
    {
        _userManager = userManager;
        _tokensRepository = tokensRepository;
        _identityOptions = identityOptions.Value;
        _jwtSettings = jwtSettings.Value;
        _auditLogService = auditLogService;
        _dataProtectionKeys = dataProtectionKeys.Value;
        _provider = provider;
    }

    public async Task<Result<TokenResponse>> AuthenticateAsync(LoginRequest loginRequest)
    {
        // Find User.
        UserDao user = await _userManager.FindByEmailAsync(loginRequest.Email);
        if (user is null)
        {
            Log.Error("Could Not find User {Email}", loginRequest.Email);
            return Result<TokenResponse>.Fail("Invalid Email/Password.").AsBadRequest().WithDescription(StatusCodeDescriptions.InvalidCredentials);
        }

        // Check Password.
        if (!await _userManager.CheckPasswordAsync(user, loginRequest.Password))
        {
            await _auditLogService.AddAuditLogAsync(user.Id, AuditLogsConstants.AuthInvalidPassword);

            Log.Error("Error: Invalid Password for {Email}", loginRequest.Email);
            return Result<TokenResponse>.Fail("Invalid Email/Password.").AsBadRequest().WithDescription(StatusCodeDescriptions.InvalidCredentials);
        }

        // Check Email Confirmed.
        if (_identityOptions.SignInRequreConfirmedEmail && !await _userManager.IsEmailConfirmedAsync(user))
        {
            await _auditLogService.AddAuditLogAsync(user.Id, AuditLogsConstants.AuthEmailNotConfirmed);

            Log.Error("Error: Email Not Confirmed {Email}", loginRequest.Email);
            return Result<TokenResponse>.Fail("Email Not Confirmed.").WithDescription(StatusCodeDescriptions.RequiresConfirmEmail);
        }

        try
        {
            TokenResponse authToken = await CreateAuthTokenAsync(user);
            Log.Information("User {Email} Signed In", user.Email);

            await _auditLogService.AddAuditLogAsync(user.Id, AuditLogsConstants.AuthSignedIn);
            return Result<TokenResponse>.Success(authToken);
        }
        catch (Exception e)
        {
            Log.Error("An error occurred while authenticating the user {Email}. {Error} {StackTrace} {InnerException} {Source}",
                loginRequest.Email, e.Message, e.StackTrace, e.InnerException, e.Source);
        }

        return Result<TokenResponse>.Fail("Request Not Supported.").AsUnauthorized();
    }

    private async Task<TokenResponse> CreateAuthTokenAsync(UserDao user)
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

        var refreshToken = new TokenDao()
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
            await _tokensRepository.AddUserTokenAsync(refreshToken);
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
