namespace RestIdentity.Client.Infrastructure.Routes;

public static class AuthenticationEndpoints
{
    public static readonly string Login = "api/auth/login";
    public static readonly string LoginWith2fa = "api/auth/loginWithTwoFactor";
    public static readonly string LoginWithRecoveryCode = "api/auth/loginWithRecoveryCode";
    public static readonly string Logout = "api/auth/logout";
}
