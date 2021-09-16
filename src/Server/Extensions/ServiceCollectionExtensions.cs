using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestIdentity.Server.Constants;
using RestIdentity.Server.Data;
using RestIdentity.Server.Models;
using RestIdentity.Server.Models.DAO;
using RestIdentity.Server.Services.AuthenticationHandler;

namespace RestIdentity.Server.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlServerDatabase(this IServiceCollection services, string connectionString)
    {
        return services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));
    }

    public static IDataProtectionBuilder AddDataProtectionKeys(this IServiceCollection services, string dataProtectionKeysConnection)
    {
        services.AddDbContext<DataProtectionKeysContext>(options =>
            options.UseSqlServer(dataProtectionKeysConnection));

        return services.AddDataProtection().PersistKeysToDbContext<DataProtectionKeysContext>();
    }

    public static IdentityBuilder AddUserIdentity(this IServiceCollection services, IdentityDefaultOptions identityDefaultOptions)
    {
        return services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = identityDefaultOptions.PasswordRequireDigit;
            options.Password.RequiredLength = identityDefaultOptions.PasswordRequiredLength;
            options.Password.RequireNonAlphanumeric = identityDefaultOptions.PasswordRequireNonAlphanumeric;
            options.Password.RequireUppercase = identityDefaultOptions.PasswordRequireUppercase;
            options.Password.RequireLowercase = identityDefaultOptions.PasswordRequireLowercase;
            options.Password.RequiredUniqueChars = identityDefaultOptions.PasswordRequiredUniqueChars;

            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(identityDefaultOptions.LockoutDefaultLockoutTimeSpanInMinutes);
            options.Lockout.MaxFailedAccessAttempts = identityDefaultOptions.LockoutMaxFailedAccessAttempts;
            options.Lockout.AllowedForNewUsers = identityDefaultOptions.LockoutAllowedForNewUsers;

            options.User.RequireUniqueEmail = identityDefaultOptions.UserRequreUniqueEmail;
            options.SignIn.RequireConfirmedEmail = identityDefaultOptions.SignInRequreConfirmedEmail;
        })
           .AddEntityFrameworkStores<ApplicationDbContext>()
           .AddDefaultTokenProviders();
    }

    public static AuthenticationBuilder AddCustomerAuthentication(this IServiceCollection services)
    {
        services.AddTransient<ICustomAuthenticationHandler, CustomAuthenticationHandler>();

        return services.AddAuthentication(x =>
        {
            x.DefaultChallengeScheme = RolesConstants.Customer;
            x.DefaultSignInScheme = RolesConstants.Customer;
            x.DefaultAuthenticateScheme = RolesConstants.Customer;
            x.DefaultScheme = RolesConstants.Customer;
        })
            .AddScheme<CustomerAuthenticationOptions, CustomerAuthenticationHandler>(RolesConstants.Customer, null);
    }

    public static AuthenticationBuilder AddAdminAuthentication(this IServiceCollection services)
    {
        services.AddTransient<ICustomAuthenticationHandler, CustomAuthenticationHandler>();

        return services.AddAuthentication(RolesConstants.Admin)
            .AddScheme<AdminAuthenticationOptions, AdminAuthenticationHandler>(RolesConstants.Admin, null);
    }
}
