namespace RestIdentity.Shared.Wrapper;

public static class StatusCodeDescriptions
{
    public const string None = "NONE";

    public const string RequiresConfirmEmail = "AUTH.REQUIRE_EMAIL_CONFIRM";
    public const string RequiresTwoFactor = "AUTH.REQUIRE_TWO_FACTOR";
    public const string InvalidCredentials = "AUTH.INVALID_CREDENTIALS";

    public const string FileNotFound = "FILE.NOT_FOUND";
    public const string FileTooLarge = "FILE.TOO_LARGE";
    public const string FileTypeIsUnsupported = "FILE.UNSUPPORTED_TYPE";
}
