using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestIdentity.DataAccess.Data;
using RestIdentity.DataAccess.EntityFrameworkCore;
using RestIdentity.DataAccess.EntityFrameworkCore.Repositories;
using RestIdentity.DataAccess.Models;
using RestIdentity.DataAccess.Repositories;

namespace RestIdentity.DataAccess;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEfCoreDataAccessWithSqlServer(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
        services.AddTransient<IDatabaseInitializer, DatabaseInitializer>();

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddTransient<IUserAvatarRepository, UserAvatarRepository>();
        services.AddTransient<IAuditLogRepository, AuditLogRepository>();
        services.AddTransient<ITokenRepository, TokenRepository>();
        services.AddTransient<IRoleRepository, RoleRepository>();

        return services;
    }

    public static IdentityBuilder AddIdentityUserRepository(this IServiceCollection services, Action<IdentityOptions> userSetupAction)
    {
        return services.AddIdentity<UserDao, IdentityRole>(userSetupAction)
            .AddEntityFrameworkStores<ApplicationDbContext>();
    }
}
