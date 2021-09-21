﻿namespace RestIdentity.Server.Services.FunctionalServices;

public interface IFunctionalService
{
    Task<bool> AnyRolesExistsAsync();
    Task<bool> AnyUsersExistsAsync();
    Task CreateDefaultRolesAsync();
    Task CreateDefaultAdminUserAsync();
    Task CreateDefaultCustomerUserAsync();

}
