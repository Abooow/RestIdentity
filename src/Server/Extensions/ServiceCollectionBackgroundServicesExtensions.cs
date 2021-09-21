using RestIdentity.Server.BackgroundServices;
using RestIdentity.Server.BackgroundServices.Channels;
using RestIdentity.Server.Services;

namespace RestIdentity.Server.Extensions;

internal static class ServiceCollectionBackgroundServicesExtensions
{
    public static IServiceCollection AddUserAvatarBackgroundService(this IServiceCollection services)
    {
        services.AddTransient<IUserAvatarService, UserAvatarService>();
        services.AddSingleton<UserAvatarChannel>();
        services.AddHostedService<UserAvatarDispatcher>();

        return services;
    }
}
