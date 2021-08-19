using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;
using RestIdentity.Client.Infrastructure.Facades.Identity;
using RestIdentity.Client.Services;
using RestIdentity.Client.Services.Storage;

namespace RestIdentity.Client;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        // Storage
        builder.Services.AddSingleton(serviceProvider => (IJSInProcessRuntime)serviceProvider.GetRequiredService<IJSRuntime>());
        builder.Services.AddSingleton(serviceProvider => (IJSUnmarshalledRuntime)serviceProvider.GetRequiredService<IJSRuntime>());
        builder.Services.AddScoped<ILocalStorage, LocalStorage>();
        //builder.Services.AddScoped<ICookieStorage, CookieStorage>();

        // Auth
        builder.Services.AddOptions();
        builder.Services.AddAuthorizationCore();
        builder.Services.AddScoped<AuthStateProvider>();
        builder.Services.AddScoped<AuthenticationStateProvider>(services => services.GetRequiredService<AuthStateProvider>());

        // HttpFacades
        builder.Services.AddTransient<IAuthenticationFacade, AuthenticationFacade>();
        builder.Services.AddTransient<IUserFacade, UserFacade>();

        // MudSnackbar
        builder.Services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;

            config.SnackbarConfiguration.PreventDuplicates = false;
            config.SnackbarConfiguration.NewestOnTop = false;
            config.SnackbarConfiguration.ShowCloseIcon = true;
            config.SnackbarConfiguration.VisibleStateDuration = 4000;
            config.SnackbarConfiguration.HideTransitionDuration = 500;
            config.SnackbarConfiguration.ShowTransitionDuration = 500;
            config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
        });

        await builder.Build().RunAsync();
    }
}
