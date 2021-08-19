using System.Diagnostics;
using RestIdentity.Logging;
using Serilog;

namespace RestIdentity.Server;

public static class Program
{
    public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
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
