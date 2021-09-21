using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using RestIdentity.DataAccess;
using RestIdentity.Server.Constants;
using RestIdentity.Server.Services.AuditLog;
using Serilog;

namespace RestIdentity.Server.Services.AuthenticationHandler;

public sealed class AdminAuthenticationOptions : AuthenticationSchemeOptions { }

public sealed class AdminAuthenticationHandler : AuthenticationHandler<AdminAuthenticationOptions>
{
    private readonly ICustomAuthenticationHandler _authenticationHandler;
    private readonly IAuditLogService _auditLogService;

    public AdminAuthenticationHandler(
        IOptionsMonitor<AdminAuthenticationOptions> options, ILoggerFactory loggerFactory, UrlEncoder urlEncoder, ISystemClock systemClock,
        ICustomAuthenticationHandler authenticationHandler,
        IAuditLogService auditLogService)
        : base(options, loggerFactory, urlEncoder, systemClock)
    {
        _authenticationHandler = authenticationHandler;

        _auditLogService = auditLogService;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            return _authenticationHandler.HandleAuthenticateAsync(Request, Scheme.Name, RolesConstants.Admin, async user =>
            {
                Log.Warning("An unauthorized user with Id {UserId} tried to access an protected endpoint {Endpoint}", user.Id, OriginalPath);
                await _auditLogService.AddAuditLogAsync(user.Id, AuditLogsConstants.UnAuthorizedUserAccessedProtectedEndpoint, $"ENDPOINT: {OriginalPath}");
            });
        }
        catch (Exception e)
        {
            Log.Error("An error occurred while decrypting user Tokens. {Error} {StackTrace} {InnerException} {Source}",
                e.Message, e.StackTrace, e.InnerException, e.Source);

            return Task.FromResult(AuthenticateResult.Fail("You are not Authorized"));
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        return _authenticationHandler.HandleChallangeResponseMessageAsync(Response);
    }
}
