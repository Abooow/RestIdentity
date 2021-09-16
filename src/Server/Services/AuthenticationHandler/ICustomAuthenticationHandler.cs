using Microsoft.AspNetCore.Authentication;
using RestIdentity.Server.Models.DAO;

namespace RestIdentity.Server.Services.AuthenticationHandler;

public interface ICustomAuthenticationHandler
{
    Task<AuthenticateResult> HandleAuthenticateAsync(HttpRequest request, string schemeName, string role, Func<ApplicationUser, Task> notInRoleCallback);
    Task HandleChallangeResponseMessageAsync(HttpResponse response);
}