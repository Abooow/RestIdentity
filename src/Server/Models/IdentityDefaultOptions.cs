namespace RestIdentity.Server.Models;

public sealed class IdentityDefaultOptions
{
    public bool PasswordRequireDigit { get; set; }
    public bool PasswordRequireNonAlphanumeric { get; set; }
    public bool PasswordRequireUppercase { get; set; }
    public bool PasswordRequireLowercase { get; set; }
    public int PasswordRequiredLength { get; set; }
    public int PasswordRequiredUniqueChars { get; set; }

    public double LockoutDefaultLockoutTimeSpanInMinutes { get; set; }
    public int LockoutMaxFailedAccessAttempts { get; set; }
    public bool LockoutAllowedForNewUsers { get; set; }

    public bool UserRequreUniqueEmail { get; set; }
    public bool SignInRequreConfirmedEmail { get; set; }
    public string AccessDeniedPath { get; set; }
}
