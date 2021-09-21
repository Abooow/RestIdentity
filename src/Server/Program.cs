using System.Diagnostics;
using RestIdentity.DataAccess;
using RestIdentity.Logging;
using RestIdentity.Server.Data;
using RestIdentity.Server.Services;
using Serilog;

namespace RestIdentity.Server;

public static class Program
{
    public static async Task Main(string[] args)
    {
        IHost host = CreateHostBuilder(args).Build();

        await InitializeDatabaseAsync(host);

        host.Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseSerilog((hotstingContext, loggongConfiguration) => loggongConfiguration
                .Enrich.FromLogContext()
                .Enrich.WithProperty("    Application", "RestIdentity")
                .Enrich.WithProperty("    CurrentManagedThreadId", Environment.CurrentManagedThreadId)
                .Enrich.WithProperty("    ProcessId", Environment.ProcessId)
                .Enrich.WithProperty("    ProcessName", Process.GetCurrentProcess().ProcessName)
                .WriteTo.Console(theme: ConsoleThemes.CustumDark)
                .WriteTo.File(new TextFormatter(), Path.Combine($@"{hotstingContext.HostingEnvironment.ContentRootPath}\Logs\", $"{DateTime.Now:yyyyMMdd_HHmmss}.txt"))
                .ReadFrom.Configuration(hotstingContext.Configuration));

                webBuilder.UseStartup<Startup>();
            });
    }

    private static async Task InitializeDatabaseAsync(IHost host)
    {
        var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var dataProtectionKeysContext = services.GetService<DataProtectionKeysContext>();
            var dbInitializer = services.GetService<IDatabaseInitializer>();
            var functionalService = services.GetService<IFunctionalService>();

            await DbContextInitializer.InitializeAsync(dataProtectionKeysContext, dbInitializer, functionalService);
        }
        catch (Exception e)
        {
            Log.Error("An Error occurred while seeding the database {Error} {StackTrace} {InnerException} {Source}",
                e.Message, e.StackTrace, e.InnerException, e.Source);
        }
    }
}
