using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestIdentity.Server.Data;
using RestIdentity.Server.Models;
using RestIdentity.Server.Services.EmailSenders;
using RestIdentity.Server.Services.FunctionalServices;
using RestIdentity.Shared.Wrapper;

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
        services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

        services.AddDbContext<DataProtectionKeysContext>(options =>
        options.UseSqlServer(Configuration.GetConnectionString("DataProtectionKeysConnection")));

        var identityDefaultOptionsConfiguration = Configuration.GetSection("identityDefaultOptions");
        services.Configure<IdentityDefaultOptions>(identityDefaultOptionsConfiguration);
        var identityDefaultOptions = identityDefaultOptionsConfiguration.Get<IdentityDefaultOptions>();

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
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

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = false;
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Response.ContentType = "application/json";
                using StreamWriter writer = new StreamWriter(context.Response.BodyWriter.AsStream());
                writer.Write(JsonSerializer.Serialize(Result.Fail("You are not Authorized dud.").AsUnauthorized()));
                context.Response.Body = writer.BaseStream;

                return Task.CompletedTask;
            };
        });

        services.AddControllers();
        services.AddControllersWithViews();
        services.AddRazorPages();

        services.AddTransient<IEmailSender, FileEmailSender>();
        services.AddTransient<IFunctionalService, FunctionalService>();
        services.Configure<AdminUserOptions>(Configuration.GetSection("DefaultUserOptions:Admin"));
        services.Configure<CustomerUserOptions>(Configuration.GetSection("DefaultUserOptions:Customer"));
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
