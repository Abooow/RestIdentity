namespace RestIdentity.Client.Infrastructure.Routes;

public static class UserEndpoints
{
    public static readonly string GetMe = "api/auth/getMe";
    public static readonly string Register = "api/auth/register";
    public static readonly string ResendEmailConfirmation = "api/auth/resendEmailConfirmation";
    public static readonly string ChangePassword = "api/auth/changePassword";
    public static readonly string ForgotPassword = "api/auth/forgotPassword";
    public static readonly string ResetPassword = "api/auth/resetPassword";
    public static readonly string EnableTwoFactorAuth = "api/auth/enableTwoFactorAuth";
    public static readonly string DisableTwoFactorAuth = "api/auth/disableTwoFactorAuth";
    public static readonly string GenerateRecoveryCodes = "api/auth/generateRecoveryCodes";
    public static readonly string ResetAuthenticator = "api/auth/resetAuthenticator";

    public static string ConfirmEmail(string userId, string code)
    {
        return $"api/auth/confirmEmail?userId={userId}&code={code}";
    }
}
