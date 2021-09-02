﻿namespace RestIdentity.Shared.Models;

public sealed record PersonalUserProfile
(
    string ProfileImage,
    string Email,
    string UserName,
    string FirstName,
    string LastName,
    string? Phone,
    bool IsTwoFactorOn,
    bool IsPhoneVerified,
    bool IsEmailVerified,
    bool IsAccountLocked,
    DateTime CreatedOn,
    IEnumerable<string> Roles
);
