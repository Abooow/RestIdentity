using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestIdentity.DataAccess.EntityFrameworkCore.Repositories;
using RestIdentity.DataAccess.Models;
using RestIdentity.DataAccess.Repositories;
using RestIdentity.Server.Data;

namespace RestIdentity.DataAccess;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEfCoreDataAccessWithSqlServer(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

        return services;
    }

    public static IdentityBuilder AddRepositories(this IServiceCollection services, Action<IdentityOptions> userSetupAction)
    {
        services.AddTransient<IUserAvatarRepository, UserAvatarRepository>();
        services.AddTransient<IAuditLogRepository, AuditLogRepository>();
        services.AddTransient<ITokenRepository, TokenRepository>();

        return services.AddIdentity<UserDao, IdentityRole>(userSetupAction)
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
    }
}
