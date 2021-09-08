using RestIdentity.Server.Models;
using RestIdentity.Server.Models.Options;

namespace RestIdentity.Server.Extensions;

internal static class ServiceCollectionConfigurationExtensions
{
    public static IdentityDefaultOptions ConfigureDefaultIdentityOptions(this IServiceCollection services, IConfiguration configuration)
    {
        IConfigurationSection identityDefaultOptionsConfiguration = configuration.GetSection(nameof(IdentityDefaultOptions));
        services.Configure<IdentityDefaultOptions>(identityDefaultOptionsConfiguration);

        return identityDefaultOptionsConfiguration.Get<IdentityDefaultOptions>();
    }

    public static (AdminUserOptions, CustomerUserOptions) ConfigureDefaultAdminAndCustomerOptions(this IServiceCollection services, IConfiguration configuration)
    {
        IConfigurationSection adminOptionsConfiguration = configuration.GetSection("DefaultUserOptions:Admin");
        IConfigurationSection customerOptionsConfiguration = configuration.GetSection("DefaultUserOptions:Customer");
        services.Configure<AdminUserOptions>(configuration.GetSection("DefaultUserOptions:Admin"));
        services.Configure<CustomerUserOptions>(configuration.GetSection("DefaultUserOptions:Customer"));

        return (adminOptionsConfiguration.Get<AdminUserOptions>(), customerOptionsConfiguration.Get<CustomerUserOptions>());
    }

    public static DataProtectionKeys ConfigureDataProtectionKeys(this IServiceCollection services, IConfiguration configuration)
    {
        IConfigurationSection dataProtectionSection = configuration.GetSection(nameof(DataProtectionKeys));
        services.Configure<DataProtectionKeys>(dataProtectionSection);

        return dataProtectionSection.Get<DataProtectionKeys>();
    }

    public static JwtSettings ConfigureJwtSettings(this IServiceCollection services, IConfiguration configuration)
    {
        IConfigurationSection jwtSettingsSection = configuration.GetSection(nameof(JwtSettings));
        services.Configure<JwtSettings>(jwtSettingsSection);

        return jwtSettingsSection.Get<JwtSettings>();
    }

    public static FileStorageOptions ConfigureFileStorageOptions(this IServiceCollection services, IConfiguration configuration)
    {
        IConfigurationSection fileStorageOptionsSection = configuration.GetSection(nameof(FileStorageOptions));
        services.Configure<FileStorageOptions>(fileStorageOptionsSection);

        FileStorageOptions storageOptions = fileStorageOptionsSection.Get<FileStorageOptions>();

        // Ensure the directories exists.
        Directory.CreateDirectory(storageOptions.UserProfileImagesPath);
        Directory.CreateDirectory(storageOptions.TempFilesPath);

        return storageOptions;
    }

    public static ProfileImageDefaultOptions ConfigureProfileImageOptions(this IServiceCollection services, IConfiguration configuration)
    {
        IConfigurationSection profileImageOptionsSection = configuration.GetSection(nameof(ProfileImageDefaultOptions));
        services.Configure<ProfileImageDefaultOptions>(profileImageOptionsSection);

        return profileImageOptionsSection.Get<ProfileImageDefaultOptions>();
    }
}
