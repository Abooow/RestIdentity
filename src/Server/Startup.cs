using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using RestIdentity.Server.Extensions;
using RestIdentity.Server.Models;
using RestIdentity.Server.Services.Activity;
using RestIdentity.Server.Services.Authentication;
using RestIdentity.Server.Services.Cookies;
using RestIdentity.Server.Services.EmailSenders;
using RestIdentity.Server.Services.IpInfo;
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
        IdentityDefaultOptions identityOptions = services.ConfigureDefaultIdentityOptions(Configuration);
        services.AddUserIdentity(identityOptions);
        services.ConfigureDefaultAdminAndCustomerOptions(Configuration);

        // Data Protection Keys.
        services.ConfigureDataProtectionKeys(Configuration);
        services.AddDataProtectionKeys(Configuration.GetConnectionString("DataProtectionKeysConnection"));

        // JWT Authentication.
        JwtSettings jwtSettings = services.ConfigureJwtSettings(Configuration);
        services.AddJwtAuthentication(jwtSettings);
        services.AddAdminSchemeAuthentication();

        // File Storage.
        services.ConfigureFileStorageOptions(Configuration);

        // Profile Image Options.
        services.ConfigureProfileImageOptions(Configuration);

        services.AddTransient<IAuthService, AuthService>();
        services.AddTransient<IActivityService, ActivityService>();

        services.AddHttpContextAccessor();
        services.AddTransient<ICookieService, CookieService>();

        services.AddTransient<IUserService, UserService>();
        services.AddTransient<ISignedInUserService, SignedInUserService>();
        services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

        services.AddHttpClient();
        services.AddTransient<IIpInfoService, TestIpInfoService>();

        services.AddTransient<IEmailSender, FileEmailSender>();

        // MVC.
        services.AddControllers().AddInvalidModelStateResponse();
        services.AddControllersWithViews();
        services.AddRazorPages();

        // Background Services.
        services.AddProfileImageBackgroundService();
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
