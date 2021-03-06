namespace RestIdentity.Server.Constants;

internal static class AuditLogsConstants
{
    public const string UnAuthorizedUserAccessedProtectedEndpoint = "AUTH.UNAUTHORIZED_USER.ACCESSED_PROTECTED_ENDPOINT";

    public const string AuthInvalidPassword = "AUTH.INVALID.PASSWORD";
    public const string AuthEmailNotConfirmed = "AUTH.EMAIL_NOT_CONFIRMED";
    public const string AuthSignedIn = "AUTH.SIGN_IN";
    public const string AuthSignedOut = "AUTH.SIGN_OUT";
    public const string AuthRegistered = "AUTH.REGISTERED";
    public const string AuthChangePassword = "AUTH.CHANGE.PASSWORD";
    public const string AuthChangeOtherPassword = "AUTH.CHANGE.OTHER_PASSWORD";

    public const string CreateAdminUser = "CREATE.USER.ADMIN";

    public const string UserProfileUpdated = "USER.UPDATE.PROFILE";
    public const string UserUpdatedDefaultAvatar = "USER.UPDATE.DEFAULT_AVATAR";
    public const string UserUpdatedAvatar = "USER.UPDATE.AVATAR";
    public const string UserRemovedAvatar = "USER.REMOVE.AVATAR";

    public static readonly string[] PartialAuditLogTypes =
    {
        AuthRegistered,
        AuthSignedIn,
        AuthChangePassword,
        UserProfileUpdated
    };
}
