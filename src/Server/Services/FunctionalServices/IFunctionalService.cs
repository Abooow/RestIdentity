namespace RestIdentity.Server.Services.FunctionalServices;

public interface IFunctionalService
{
    Task CreateDefaultAdminUserAsync();
    Task CreateDefaultCustomerUserAsync();
}
