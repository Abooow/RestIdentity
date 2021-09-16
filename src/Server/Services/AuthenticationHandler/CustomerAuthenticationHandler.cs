using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RestIdentity.Server.Constants;
using Serilog;

namespace RestIdentity.Server.Services.AuthenticationHandler;

public sealed class CustomerAuthenticationOptions : AuthenticationSchemeOptions { }

public sealed class CustomerAuthenticationHandler : AuthenticationHandler<CustomerAuthenticationOptions>
{
    private readonly ICustomAuthenticationHandler _authenticationHandler;

    public CustomerAuthenticationHandler(
        IOptionsMonitor<CustomerAuthenticationOptions> options, ILoggerFactory loggerFactory, UrlEncoder urlEncoder, ISystemClock systemClock,
        ICustomAuthenticationHandler authenticationHandler)
        : base(options, loggerFactory, urlEncoder, systemClock)
    {
        _authenticationHandler = authenticationHandler;
    }

    protected async override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            return await _authenticationHandler.HandleAuthenticateAsync(Request, Scheme.Name, RolesConstants.Customer, user =>
            {
                Log.Warning("User {UserId} does not have User role but tried to access {Endpoint}", user.Id, OriginalPath);
                return Task.CompletedTask;
            });
        }
        catch (SecurityTokenExpiredException ex)
        {
            // TODO: Try to use the refresh-cookie to refresh the token.
            Log.Warning("Token was Expired");

            return AuthenticateResult.Fail("You are not Authorized");
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
        return _authenticationHandler.HandleChallangeResponseMessageAsync(Response);
    }
}
