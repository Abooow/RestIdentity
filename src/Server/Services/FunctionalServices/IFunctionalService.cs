namespace RestIdentity.Server.Services.FunctionalServices;

public interface IFunctionalService
{
    Task<bool> AnyUsersExistsAsync();
    Task CreateDefaultAdminUserAsync();
    Task CreateDefaultCustomerUserAsync();
}
