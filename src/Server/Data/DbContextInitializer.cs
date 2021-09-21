using RestIdentity.DataAccess;
using RestIdentity.Server.Services.FunctionalServices;

namespace RestIdentity.Server.Data;

public static class DbContextInitializer
{
    public static async Task InitializeAsync(DataProtectionKeysContext dataProtectionKeysContext, IDatabaseInitializer databaseInitializer, IFunctionalService functionalService)
    {
        await dataProtectionKeysContext.Database.EnsureCreatedAsync();
        await databaseInitializer.EnsureCreatedAsync();

        if (await functionalService.AnyUsersExistsAsync())
            return;

        await functionalService.CreateDefaultAdminUserAsync();
        await functionalService.CreateDefaultCustomerUserAsync();
    }
}
