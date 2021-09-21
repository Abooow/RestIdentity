using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RestIdentity.DataAccess.Models;
using RestIdentity.DataAccess.Repositories;
using RestIdentity.Server.Constants;
using RestIdentity.Server.Models;
using RestIdentity.Shared.Wrapper;
using Serilog;

namespace RestIdentity.Server.Services;

public class CustomAuthenticationHandler : ICustomAuthenticationHandler
{
    private readonly UserManager<UserRecord> _userManager;
    private readonly ITokensRepository _tokensRepository;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly DataProtectionKeys _dataProtectionKeys;
    private readonly JwtSettings _jwtSettings;

    public CustomAuthenticationHandler(
        UserManager<UserRecord> userManager,
        ITokensRepository tokensRepository,
        IDataProtectionProvider dataProtectionProvider,
        IOptions<DataProtectionKeys> dataProtectionKeys,
        IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _tokensRepository = tokensRepository;
        _dataProtectionProvider = dataProtectionProvider;
        _dataProtectionKeys = dataProtectionKeys.Value;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthenticateResult> HandleAuthenticateAsync(HttpRequest request, string schemeName, string role, Func<UserRecord, Task> notInRoleCallback)
    {
        if (!request.Cookies.ContainsKey(CookieConstants.AccessToken) || !request.Cookies.ContainsKey(CookieConstants.UserId))
        {
            Log.Error("No Access Token or User Id was found.");
            return AuthenticateResult.NoResult();
        }

        if (!AuthenticationHeaderValue.TryParse($"Bearer {request.Cookies[CookieConstants.AccessToken]}", out AuthenticationHeaderValue authHeaderValue))
        {
            Log.Error("Could not Parse Token from Authentication Header.");
            return AuthenticateResult.NoResult();
        }

        if (!AuthenticationHeaderValue.TryParse($"Bearer {request.Cookies[CookieConstants.UserId]}", out AuthenticationHeaderValue userIdHeaderValue))
        {
            Log.Error("Could not Parse User Id from Authentication Header.");
            return AuthenticateResult.NoResult();
        }

        byte[] secret = Encoding.ASCII.GetBytes(_jwtSettings.Secret);
        var tokenHandler = new JwtSecurityTokenHandler();

        var validationParameters = new TokenValidationParameters
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

        IDataProtector protector = _dataProtectionProvider.CreateProtector(_dataProtectionKeys.ApplicationUserKey);

        string decryptedUserId = protector.Unprotect(userIdHeaderValue.Parameter);
        string decryptedToken = protector.Unprotect(authHeaderValue.Parameter);

        TokenRecord tokenModel = await _tokensRepository.GetUserTokenAsync(decryptedUserId, request.Cookies[CookieConstants.UserName]);

        if (tokenModel is null)
            return AuthenticateResult.Fail("You are not Authorized");

        IDataProtector layerTwoProtector = _dataProtectionProvider.CreateProtector(tokenModel.EncryptionKeyJwt.ToString());
        string decryptedLayerTwoToken = layerTwoProtector.Unprotect(decryptedToken);

        ClaimsPrincipal validateToken = tokenHandler.ValidateToken(decryptedLayerTwoToken, validationParameters, out SecurityToken securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken
            || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            return AuthenticateResult.Fail("You are not Authorized");
        }

        string username = validateToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
        if (request.Cookies[CookieConstants.UserName] != username)
            return AuthenticateResult.Fail("You are not Authorized");

        UserRecord user = await _userManager.FindByNameAsync(username);
        if (user is null)
            return AuthenticateResult.Fail("You are not Authorized");

        if (!await _userManager.IsInRoleAsync(user, role))
        {
            await notInRoleCallback?.Invoke(user);
            return AuthenticateResult.Fail("You are not Authorized");
        }

        var identity = new ClaimsIdentity(validateToken.Claims, schemeName);
        var principal = new ClaimsPrincipal(identity);
        var authTicket = new AuthenticationTicket(principal, schemeName);

        return AuthenticateResult.Success(authTicket);
    }

    public Task HandleChallangeResponseMessageAsync(HttpResponse response)
    {
        //response.Cookies.Delete(CookieConstants.AccessToken);
        //response.Cookies.Delete(CookieConstants.UserId);
        response.Headers["WWW-Authentication"] = "Not Authorized";

        response.StatusCode = (int)HttpStatusCode.Unauthorized;
        response.ContentType = "application/json";
        using var writer = new StreamWriter(response.BodyWriter.AsStream());
        writer.Write(JsonSerializer.Serialize(Result.Fail("You are not Authorized.").AsUnauthorized()));
        response.Body = writer.BaseStream;

        return Task.CompletedTask;
    }
}
