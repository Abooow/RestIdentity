using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RestIdentity.DataAccess;
using RestIdentity.DataAccess.Models;
using RestIdentity.DataAccess.Repositories;
using RestIdentity.Server.Models;
using Serilog;

namespace RestIdentity.Server.Services;

public sealed class FunctionalService : IFunctionalService
{
    private readonly UserManager<UserRecord> _userManager;
    private readonly IRolesRepository _rolesRepository;
    private readonly AdminUserOptions _adminUserOptions;
    private readonly CustomerUserOptions _customerUserOptions;

    public FunctionalService(
        UserManager<UserRecord> userManager,
        IRolesRepository rolesRepository,
        IOptions<AdminUserOptions> adminUserOptions,
        IOptions<CustomerUserOptions> customerUserOptions)
    {
        _userManager = userManager;
        _rolesRepository = rolesRepository;
        _adminUserOptions = adminUserOptions.Value;
        _customerUserOptions = customerUserOptions.Value;
    }

    public Task<bool> AnyRolesExistsAsync()
    {
        return _rolesRepository.AnyRolesExistsAsync();
    }

    public Task<bool> AnyUsersExistsAsync()
    {
        return _userManager.Users.AnyAsync();
    }

    public async Task CreateDefaultRolesAsync()
    {
        var adminRole = new IdentityRole() { Id = RolesConstants.AdminId, Name = RolesConstants.Admin, NormalizedName = RolesConstants.AdminNormalized };
        var customerRole = new IdentityRole() { Id = RolesConstants.CustomerId, Name = RolesConstants.Customer, NormalizedName = RolesConstants.CustomerNormalized };

        await _rolesRepository.AddRolesAsync(adminRole, customerRole);
    }

    public Task CreateDefaultAdminUserAsync()
    {
        var adminUser = new UserRecord()
        {
            Email = _adminUserOptions.Email,
            UserName = _adminUserOptions.Username,
            EmailConfirmed = true,
            FirstName = _adminUserOptions.FirstName,
            LastName = _adminUserOptions.LastName,
            DateCreated = DateTime.UtcNow
        };

        return CreateUserAsync(adminUser, _adminUserOptions.Password, RolesConstants.Admin, RolesConstants.Customer);
    }

    public Task CreateDefaultCustomerUserAsync()
    {
        var customerUser = new UserRecord()
        {
            Email = _customerUserOptions.Email,
            UserName = _customerUserOptions.Username,
            EmailConfirmed = true,
            FirstName = _customerUserOptions.FirstName,
            LastName = _customerUserOptions.LastName,
            DateCreated = DateTime.UtcNow
        };

        return CreateUserAsync(customerUser, _customerUserOptions.Password, RolesConstants.Customer);
    }

    private async Task CreateUserAsync(UserRecord user, string password, params string[] roles)
    {
        try
        {
            IdentityResult result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                string errorsString = string.Join(", ", result.Errors);
                Log.Error("An Error occurred while creating an {role} User {Errors}", errorsString, user.UserName);

                return;
            }

            foreach (string role in roles)
            {
                await _userManager.AddToRoleAsync(user, role);
            }

            Log.Information($"User with roles: ({string.Join(", ", roles)}) was successfully created. ({{UserName}})", user.UserName);
        }
        catch (Exception e)
        {
            Log.Error($"An Error occurred while creating a User with roles: ({string.Join(", ", roles)}) {{Error}} {{StackTrace}} {{InnerException}} {{Source}}",
                e.Message, e.StackTrace, e.InnerException, e.Source);
        }
    }
}
