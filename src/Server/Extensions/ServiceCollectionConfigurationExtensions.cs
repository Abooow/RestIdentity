using RestIdentity.Server.Models;
using RestIdentity.Server.Models.Options;

namespace RestIdentity.Server.Extensions;

internal static class ServiceCollectionConfigurationExtensions
{
    public static IdentityDefaultOptions ConfigureDefaultIdentityOptions(this IServiceCollection services, IConfiguration configuration, string identityOptionsSectionKey)
    {
        IConfigurationSection identityDefaultOptionsConfiguration = configuration.GetSection(identityOptionsSectionKey);
        services.Configure<IdentityDefaultOptions>(identityDefaultOptionsConfiguration);

        return identityDefaultOptionsConfiguration.Get<IdentityDefaultOptions>();
    }

    public static (AdminUserOptions, CustomerUserOptions) ConfigureDefaultAdminAndCustomerOptions(
        this IServiceCollection services, IConfiguration configuration, string adminOptionsSectionKey, string customerOptionsSectionKey)
    {
        IConfigurationSection adminOptionsConfiguration = configuration.GetSection(adminOptionsSectionKey);
        IConfigurationSection customerOptionsConfiguration = configuration.GetSection(customerOptionsSectionKey);
        services.Configure<AdminUserOptions>(adminOptionsConfiguration);
        services.Configure<CustomerUserOptions>(customerOptionsConfiguration);

        return (adminOptionsConfiguration.Get<AdminUserOptions>(), customerOptionsConfiguration.Get<CustomerUserOptions>());
    }

    public static DataProtectionKeys ConfigureDataProtectionKeys(this IServiceCollection services, IConfiguration configuration, string dataProtectionKeysSectionKey)
    {
        IConfigurationSection dataProtectionSection = configuration.GetSection(dataProtectionKeysSectionKey);
        services.Configure<DataProtectionKeys>(dataProtectionSection);

        return dataProtectionSection.Get<DataProtectionKeys>();
    }

    public static JwtSettings ConfigureJwtSettings(this IServiceCollection services, IConfiguration configuration, string jwtSettingsSectionKey)
    {
        IConfigurationSection jwtSettingsSection = configuration.GetSection(jwtSettingsSectionKey);
        services.Configure<JwtSettings>(jwtSettingsSection);

        return jwtSettingsSection.Get<JwtSettings>();
    }

    public static FileStorageOptions ConfigureFileStorageOptions(this IServiceCollection services, IConfiguration configuration, string fileStorageSectionKey)
    {
        IConfigurationSection fileStorageOptionsSection = configuration.GetSection(fileStorageSectionKey);
        services.Configure<FileStorageOptions>(fileStorageOptionsSection);

        FileStorageOptions storageOptions = fileStorageOptionsSection.Get<FileStorageOptions>();

        // Ensure the directories exists.
        Directory.CreateDirectory(storageOptions.UserAvatarsPath);
        Directory.CreateDirectory(storageOptions.TempFilesPath);

        return storageOptions;
    }

    public static UserAvatarDefaultOptions ConfigureUserAvatarOptions(this IServiceCollection services, IConfiguration configuration, string userAvatarOptionsSectionKey)
    {
        IConfigurationSection userAvatarOptionsSection = configuration.GetSection(userAvatarOptionsSectionKey);
        services.Configure<UserAvatarDefaultOptions>(userAvatarOptionsSection);

        return userAvatarOptionsSection.Get<UserAvatarDefaultOptions>();
    }
}
