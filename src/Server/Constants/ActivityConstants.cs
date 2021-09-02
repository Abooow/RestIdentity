namespace RestIdentity.Server.Constants;

internal static class ActivityConstants
{
    public const string UnAuthorized = "AUTH.UNAUTHORIZED";
    public const string AuthInvalidPassword = "AUTH.INVALID.PASSWORD";
    public const string AuthEmailNotConfirmed = "AUTH.EMAIL_NOT_CONFIRMED";
    public const string AuthSignedIn = "AUTH.SIGN_IN";
    public const string AuthRegistered = "AUTH.REGISTERED";
    public const string AuthChangePassword = "AUTH.CHANGE.PASSWORD";
    public const string AuthChangeOtherPassword = "AUTH.CHANGE.OTHER_PASSWORD";

    public const string CreateAdminUser = "CREATE.USER.ADMIN";

    public const string UserProfileUpdated = "USER.UPDATE.PROFILE";

    public static readonly string[] PartialActivityTypes =
    {
        AuthRegistered,
        AuthSignedIn,
        AuthChangePassword,
        UserProfileUpdated
    };
}
