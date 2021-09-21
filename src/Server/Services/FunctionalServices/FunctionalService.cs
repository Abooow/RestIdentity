using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RestIdentity.DataAccess;
using RestIdentity.DataAccess.Models;
using RestIdentity.Server.Models;
using RestIdentity.Server.Services.UserAvatars;
using Serilog;

namespace RestIdentity.Server.Services.FunctionalServices;

public sealed class FunctionalService : IFunctionalService
{
    private readonly UserManager<UserDao> _userManager;
    private readonly IUserAvatarService _userAvatarService;
    private readonly AdminUserOptions _adminUserOptions;
    private readonly CustomerUserOptions _customerUserOptions;

    public FunctionalService(
        UserManager<UserDao> userManager,
        IUserAvatarService userAvatarService,
        IOptions<AdminUserOptions> adminUserOptions,
        IOptions<CustomerUserOptions> customerUserOptions)
    {
        _userManager = userManager;
        _userAvatarService = userAvatarService;
        _adminUserOptions = adminUserOptions.Value;
        _customerUserOptions = customerUserOptions.Value;
    }

    public Task<bool> AnyUsersExistsAsync()
    {
        return _userManager.Users.AnyAsync();
    }

    public Task CreateDefaultAdminUserAsync()
    {
        var adminUser = new UserDao()
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
        var customerUser = new UserDao()
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

    private async Task CreateUserAsync(UserDao user, string password, params string[] roles)
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

            await _userAvatarService.CreateDefaultAvatarAsync(user);
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
