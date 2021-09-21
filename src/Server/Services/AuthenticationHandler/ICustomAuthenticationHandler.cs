using Microsoft.AspNetCore.Authentication;
using RestIdentity.DataAccess.Models;

namespace RestIdentity.Server.Services.AuthenticationHandler;

public interface ICustomAuthenticationHandler
{
    Task<AuthenticateResult> HandleAuthenticateAsync(HttpRequest request, string schemeName, string role, Func<UserDao, Task> notInRoleCallback);
    Task HandleChallangeResponseMessageAsync(HttpResponse response);
}