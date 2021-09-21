using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RestIdentity.DataAccess;
using RestIdentity.DataAccess.Models;
using RestIdentity.DataAccess.Repositories;
using RestIdentity.Server.Models;
using RestIdentity.Server.Services.UserAvatars;
using Serilog;

namespace RestIdentity.Server.Services.FunctionalServices;

public sealed class FunctionalService : IFunctionalService
{
    private readonly UserManager<UserDao> _userManager;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserAvatarService _userAvatarService;
    private readonly AdminUserOptions _adminUserOptions;
    private readonly CustomerUserOptions _customerUserOptions;

    public FunctionalService(
        UserManager<UserDao> userManager,
        IRoleRepository roleRepository,
        IUserAvatarService userAvatarService,
        IOptions<AdminUserOptions> adminUserOptions,
        IOptions<CustomerUserOptions> customerUserOptions)
    {
        _userManager = userManager;
        _roleRepository = roleRepository;
        _userAvatarService = userAvatarService;
        _adminUserOptions = adminUserOptions.Value;
        _customerUserOptions = customerUserOptions.Value;
    }

    public Task<bool> AnyRolesExistsAsync()
    {
        return _roleRepository.AnyRolesExistsAsync();
    }

    public Task<bool> AnyUsersExistsAsync()
    {
        return _userManager.Users.AnyAsync();
    }

    public async Task CreateDefaultRolesAsync()
    {
        var adminRole = new IdentityRole() { Id = RolesConstants.AdminId, Name = RolesConstants.Admin, NormalizedName = RolesConstants.AdminNormalized };
        var customerRole = new IdentityRole() { Id = RolesConstants.CustomerId, Name = RolesConstants.Customer, NormalizedName = RolesConstants.CustomerNormalized };

        await _roleRepository.AddRolesAsync(adminRole, customerRole);
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
