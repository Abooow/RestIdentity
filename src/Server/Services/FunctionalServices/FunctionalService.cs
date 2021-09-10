using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using RestIdentity.Server.Constants;
using RestIdentity.Server.Models;
using RestIdentity.Server.Models.DAO;
using RestIdentity.Server.Services.UserAvatars;
using Serilog;

namespace RestIdentity.Server.Services.FunctionalServices;

public sealed class FunctionalService : IFunctionalService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserAvatarService _userAvatarService;
    private readonly AdminUserOptions _adminUserOptions;
    private readonly CustomerUserOptions _customerUserOptions;

    public FunctionalService(
        UserManager<ApplicationUser> userManager,
        IUserAvatarService userAvatarService,
        IOptions<AdminUserOptions> adminUserOptions,
        IOptions<CustomerUserOptions> customerUserOptions)
    {
        _userManager = userManager;
        _userAvatarService = userAvatarService;
        _adminUserOptions = adminUserOptions.Value;
        _customerUserOptions = customerUserOptions.Value;
    }

    public Task CreateDefaultAdminUserAsync()
    {
        var adminUser = new ApplicationUser()
        {
            Email = _adminUserOptions.Email,
            UserName = _adminUserOptions.Username,
            EmailConfirmed = true,
            FirstName = _adminUserOptions.FirstName,
            LastName = _adminUserOptions.LastName,
            DateCreated = DateTime.UtcNow
        };

        return CreateUserAsync(adminUser, _adminUserOptions.Password, RolesConstants.Admin);
    }

    public Task CreateDefaultCustomerUserAsync()
    {
        var customerUser = new ApplicationUser()
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

    private async Task CreateUserAsync(ApplicationUser user, string password, string role)
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
            await _userManager.AddToRoleAsync(user, role);

            Log.Information($"{role} user was successfully created. ({{UserName}})", user.UserName);
        }
        catch (Exception e)
        {
            Log.Error($"An Error occurred while creating {role} user {{Error}} {{StackTrace}} {{InnerException}} {{Source}}",
                e.Message, e.StackTrace, e.InnerException, e.Source);
        }
    }
}
