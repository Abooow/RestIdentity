using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RestIdentity.Server.Constants;
using RestIdentity.Server.Data;
using RestIdentity.Server.Models;
using RestIdentity.Server.Models.DAO;
using RestIdentity.Server.Services.FunctionalServices;
using RestIdentity.Server.Services.Handlers;

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

    public static AuthenticationBuilder AddJwtAuthentication(this IServiceCollection services, JwtSettings jwtSettings)
    {
        byte[] key = Encoding.ASCII.GetBytes(jwtSettings.Secret);

        return services.AddAuthentication(x =>
        {
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters()
            {
                ValidateIssuerSigningKey = jwtSettings.ValidateIssuerSigningKey,
                ValidateIssuer = jwtSettings.ValidateIssuer,
                ValidateAudience = jwtSettings.ValidateAudience,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });
    }

    public static AuthenticationBuilder AddAdminSchemeAuthentication(this IServiceCollection services)
    {
        services.AddTransient<IFunctionalService, FunctionalService>();

        return services.AddAuthentication(RolesConstants.Admin)
            .AddScheme<AdminAuthenticationOptions, AdminAuthenticationHandler>(RolesConstants.Admin, null);
    }
}
