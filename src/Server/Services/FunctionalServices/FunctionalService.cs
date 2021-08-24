using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using RestIdentity.Server.Models;
using RestIdentity.Server.Models.DAO;
using Serilog;

namespace RestIdentity.Server.Services.FunctionalServices;

public sealed class FunctionalService : IFunctionalService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AdminUserOptions _adminUserOptions;
    private readonly CustomerUserOptions _customerUserOptions;

    public FunctionalService(UserManager<ApplicationUser> userManager, IOptions<AdminUserOptions> adminUserOptions, IOptions<CustomerUserOptions> customerUserOptions)
    {
        _userManager = userManager;
        _adminUserOptions = adminUserOptions.Value;
        _customerUserOptions = customerUserOptions.Value;
    }

    public async Task CreateDefaultAdminUserAsync()
    {
        try
        {
            var adminUser = new ApplicationUser()
            {
                Email = _adminUserOptions.Email,
                UserName = _adminUserOptions.Username,
                EmailConfirmed = true,
                ProfilePictureUrl = GetDefaultProfilePicUrl(),
                FirstName = _adminUserOptions.FirstName,
                LastName = _adminUserOptions.LastName,
                DateCreated = DateTime.UtcNow
            };

            IdentityResult result = await _userManager.CreateAsync(adminUser, _adminUserOptions.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(adminUser, "Admin");
                Log.Information("Admin user was successfully created. ({UserName})", adminUser.UserName);
            }
            else
            {
                string errorsString = string.Join(", ", result.Errors);
                Log.Error("An Error occurred while creating an Admin User {Errors}", errorsString, adminUser.UserName);
            }
        }
        catch (Exception e)
        {
            Log.Error("An Error occurred while creating admin user {Error} {StackTrace} {InnerException} {Source}",
                e.Message, e.StackTrace, e.InnerException, e.Source);
        }
    }

    public async Task CreateDefaultCustomerUserAsync()
    {
        try
        {
            var customerUser = new ApplicationUser()
            {
                Email = _customerUserOptions.Email,
                UserName = _customerUserOptions.Username,
                EmailConfirmed = true,
                ProfilePictureUrl = GetDefaultProfilePicUrl(),
                FirstName = _customerUserOptions.FirstName,
                LastName = _customerUserOptions.LastName,
                DateCreated = DateTime.UtcNow
            };

            IdentityResult result = await _userManager.CreateAsync(customerUser, _customerUserOptions.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(customerUser, "Customer");
                Log.Information("Customer user was successfully created. ({UserName})", customerUser.UserName);
            }
            else
            {
                string errorsString = string.Join(", ", result.Errors);
                Log.Error("An Error occurred while creating an Customer User {Errors}", errorsString, customerUser.UserName);
            }
        }
        catch (Exception e)
        {
            Log.Error("An Error occurred while creating default user {Error} {StackTrace} {InnerException} {Source}",
                e.Message, e.StackTrace, e.InnerException, e.Source);
        }
    }

    private static string GetDefaultProfilePicUrl()
    {
        return "";
    }
}
