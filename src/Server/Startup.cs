using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using RestIdentity.Server.Extensions;
using RestIdentity.Server.Models;
using RestIdentity.Server.Models.Options;
using RestIdentity.Server.Services.AuditLog;
using RestIdentity.Server.Services.Authentication;
using RestIdentity.Server.Services.Cookies;
using RestIdentity.Server.Services.EmailSenders;
using RestIdentity.Server.Services.RemoteConnectionInfo;
using RestIdentity.Server.Services.SignedInUser;
using RestIdentity.Server.Services.User;

namespace RestIdentity.Server;

public sealed class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Database Connection.
        services.AddSqlServerDatabase(Configuration.GetConnectionString("DefaultConnection"));

        // Identity User
        IdentityDefaultOptions identityOptions = services.ConfigureDefaultIdentityOptions(Configuration, nameof(IdentityDefaultOptions));
        services.AddUserIdentity(identityOptions);
        services.ConfigureDefaultAdminAndCustomerOptions(Configuration, "DefaultUserOptions:Admin", "DefaultUserOptions:Customer");

        // Data Protection Keys.
        services.ConfigureDataProtectionKeys(Configuration, nameof(DataProtectionKeys));
        services.AddDataProtectionKeys(Configuration.GetConnectionString("DataProtectionKeysConnection"));

        // JWT Authentication.
        JwtSettings jwtSettings = services.ConfigureJwtSettings(Configuration, nameof(JwtSettings));
        services.AddJwtAuthentication(jwtSettings);
        services.AddAdminSchemeAuthentication();

        // File Storage.
        services.ConfigureFileStorageOptions(Configuration, nameof(FileStorageOptions));

        // User Avatar Options.
        services.ConfigureUserAvatarOptions(Configuration, nameof(UserAvatarDefaultOptions));

        services.AddTransient<IAuthService, AuthService>();
        services.AddTransient<IAuditLogService, AuditLogService>();

        services.AddHttpContextAccessor();
        services.AddTransient<ICookieService, CookieService>();

        services.AddTransient<IUserService, UserService>();
        services.AddTransient<ISignedInUserService, SignedInUserService>();
        services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

        services.AddTransient<IRemoteConnectionInfoService, RemoteConnectionInfoService>();

        services.AddTransient<IEmailSender, FileEmailSender>();

        // MVC.
        services.AddControllers().AddInvalidModelStateResponse();
        services.AddControllersWithViews();
        services.AddRazorPages();

        // Background Services.
        services.AddUserAvatarBackgroundService();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapRazorPages();
            endpoints.MapControllers();
            endpoints.MapFallbackToFile("index.html");
        });
    }
}
