namespace RestIdentity.Client.Infrastructure.Routes
{
    public static class AuthenticationEndpoints
    {
        public static readonly string Login = "api/auth/login";
        public static readonly string LoginWith2faAsync = "api/auth/loginWithTwoFactor";
        public static readonly string LoginWithRecoveryCodeAsync = "api/auth/loginWithRecoveryCode";
        public static readonly string LogoutAsync = "api/auth/logout";
    }
}
