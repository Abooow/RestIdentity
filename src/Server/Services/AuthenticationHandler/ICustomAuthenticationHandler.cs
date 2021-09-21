using Microsoft.AspNetCore.Authentication;
using RestIdentity.DataAccess.Models;

namespace RestIdentity.Server.Services;

public interface ICustomAuthenticationHandler
{
    Task<AuthenticateResult> HandleAuthenticateAsync(HttpRequest request, string schemeName, string role, Func<UserRecord, Task> notInRoleCallback);
    Task HandleChallangeResponseMessageAsync(HttpResponse response);
}