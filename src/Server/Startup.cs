using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using RestIdentity.DataAccess;
using RestIdentity.Server.Extensions;
using RestIdentity.Server.Models;
using RestIdentity.Server.Services;

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
        services.AddEfCoreDataAccessWithSqlServer(Configuration.GetConnectionString("DefaultConnection"));

        // Identity User
        IdentityDefaultOptions identityOptions = services.ConfigureDefaultIdentityOptions(Configuration, nameof(IdentityDefaultOptions));
        services.AddUserIdentity(identityOptions);
        services.ConfigureDefaultAdminAndCustomerOptions(Configuration, "DefaultUserOptions:Admin", "DefaultUserOptions:Customer");

        // Data Protection Keys.
        services.ConfigureDataProtectionKeys(Configuration, nameof(DataProtectionKeys));
        services.AddDataProtectionKeys(Configuration.GetConnectionString("DataProtectionKeysConnection"));

        // Authentication.
        services.ConfigureJwtSettings(Configuration, nameof(JwtSettings));
        services.AddCustomerAuthentication();
        services.AddAdminAuthentication();

        // Repositories.
        services.AddRepositories();

        // Services.
        services.AddTransient<IAuthService, AuthService>();
        services.AddTransient<IAuditLogService, AuditLogService>();

        services.AddHttpContextAccessor();
        services.AddTransient<ICookieService, CookieService>();

        services.AddTransient<IUserService, UserService>();
        services.AddTransient<ISignedInUserService, SignedInUserService>();
        services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

        services.AddTransient<IEmailSender, FileEmailSender>();

        services.AddTransient<IFunctionalService, FunctionalService>();

        // MVC.
        services.AddControllers().AddInvalidModelStateResponse();
        services.AddControllersWithViews();
        services.AddRazorPages();
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
