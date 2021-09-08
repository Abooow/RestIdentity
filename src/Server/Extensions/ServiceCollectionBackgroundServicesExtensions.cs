using RestIdentity.Server.BackgroundServices;
using RestIdentity.Server.BackgroundServices.Channels;
using RestIdentity.Server.Services.ProfileImage;

namespace RestIdentity.Server.Extensions;

internal static class ServiceCollectionBackgroundServicesExtensions
{
    public static IServiceCollection AddProfileImageBackgroundService(this IServiceCollection services)
    {
        services.AddTransient<IProfileImageService, ProfileImageService>();
        services.AddSingleton<ProfileImageChannel>();
        services.AddHostedService<ProfileImageDispatcher>();

        return services;
    }
}
