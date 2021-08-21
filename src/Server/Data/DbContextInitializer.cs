using System;
using RestIdentity.Server.Services.FunctionalServices;

namespace RestIdentity.Server.Data;

public static class DbContextInitializer
{
    public static async Task InitializeAsync(DataProtectionKeysContext dataProtectionKeysContext, ApplicationDbContext applicationDbContext, IFunctionalService functionalService)
    {
        await dataProtectionKeysContext.Database.EnsureCreatedAsync();
        await applicationDbContext.Database.EnsureCreatedAsync();

        if (applicationDbContext.Users.Any())
            return;

        await functionalService.CreateDefaultAdminUserAsync();
        await functionalService.CreateDefaultCustomerUserAsync();
    }
}
