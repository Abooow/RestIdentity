using Serilog.Events;
using Serilog.Formatting;

namespace RestIdentity.Logging;

public sealed class TextFormatter : ITextFormatter
{
    public void Format(LogEvent logEvent, TextWriter output)
    {
        if (logEvent.Level == LogEventLevel.Information)
            return;

        output.WriteLine(".......................................................");
        output.WriteLine($"[{logEvent.Timestamp:dd/MMM/yyyy HH:mm:ss.FFF}] - {logEvent.Level}");

        foreach (var property in logEvent.Properties)
        {
            output.WriteLine($"    {property.Key} : {property.Value}");
        }

        if (logEvent.Exception is not null)
        {
            output.WriteLine();
            output.WriteLine("~~~~~~~~~~~~~~~~ EXCEPTION DETAILS ~~~~~~~~~~~~~~~~");
            output.WriteLine($"    EXCEPTION - {logEvent.Exception}");
            output.WriteLine($"    STACK TRACE - {logEvent.Exception.StackTrace}");
            output.WriteLine($"    MESSAGE - {logEvent.Exception.Message}");
            output.WriteLine($"    SOURCE - {logEvent.Exception.Source}");
            output.WriteLine($"    INNER EXCEPTION - {logEvent.Exception.InnerException}");
        }

        output.WriteLine(".......................................................");
        output.WriteLine();
    }
}
