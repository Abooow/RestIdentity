using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RestIdentity.Server.Constants;
using RestIdentity.Server.Data;
using RestIdentity.Server.Models;
using RestIdentity.Server.Services.Activity;
using RestIdentity.Server.Services.Authentication;
using RestIdentity.Server.Services.Cookies;
using RestIdentity.Server.Services.EmailSenders;
using RestIdentity.Server.Services.FunctionalServices;
using RestIdentity.Server.Services.Handlers;
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

        var identityDefaultOptionsConfiguration = Configuration.GetSection(nameof(IdentityDefaultOptions));
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

        var dataProtectionSection = Configuration.GetSection(nameof(DataProtectionKeys));
        services.Configure<DataProtectionKeys>(dataProtectionSection);
        services.AddDataProtection().PersistKeysToDbContext<DataProtectionKeysContext>();

        var jwtSettingsSection = Configuration.GetSection(nameof(JwtSettings));
        services.Configure<JwtSettings>(jwtSettingsSection);

        var jwtSettings = jwtSettingsSection.Get<JwtSettings>();
        var key = Encoding.ASCII.GetBytes(jwtSettings.Secret);
        services.AddAuthentication(x =>
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

        services.AddTransient<IAuthService, AuthService>();
        services.AddTransient<IActivityService, ActivityService>();

        services.AddHttpContextAccessor();
        services.AddTransient<ICookieService, CookieService>();

        services.AddAuthentication(RolesConstants.Admin).AddScheme<AdminAuthenticationOptions, AdminAuthenticationHandler>(RolesConstants.Admin, null);

        services.AddControllers().ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = context =>
            {
                var problemDetails = new ValidationProblemDetails(context.ModelState)
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "One or more model validation errors occurred.",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "See the errors property for details",
                    Instance = context.HttpContext.Request.Path
                };

                return new OkObjectResult(Result<ValidationProblemDetails>.Fail(problemDetails, "One or more model validation errors occurred.").AsBadRequest());
            });
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
