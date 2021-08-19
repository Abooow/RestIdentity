using Serilog.Sinks.SystemConsole.Themes;

namespace RestIdentity.Logging;

public static class ConsoleThemes
{
    public static CustomConsoleTheme CustumDark => new CustomConsoleTheme(new Dictionary<ConsoleThemeStyle, string>
    {
        [ConsoleThemeStyle.Text] = "\u001b[37;1m",
        [ConsoleThemeStyle.SecondaryText] = "\u001b[36;1m",
        [ConsoleThemeStyle.TertiaryText] = "\u001b[30;1m",
        [ConsoleThemeStyle.Invalid] = "\u001b[31;1m",
        [ConsoleThemeStyle.Null] = "\u001b[31m",
        [ConsoleThemeStyle.Name] = "\u001b[35;1m",
        [ConsoleThemeStyle.String] = "\u001b[33m",
        [ConsoleThemeStyle.Number] = "\u001b[33;1m",
        [ConsoleThemeStyle.Boolean] = "\u001b[31;1m",
        [ConsoleThemeStyle.Scalar] = "\u001b[37m",
        [ConsoleThemeStyle.LevelVerbose] = "\u001b[37m",
        [ConsoleThemeStyle.LevelDebug] = "\u001b[44;1m\u001b[37;1m",
        [ConsoleThemeStyle.LevelInformation] = "\u001b[42;1m\u001b[37;1m",
        [ConsoleThemeStyle.LevelWarning] = "\u001b[43;1m\u001b[37;1m",
        [ConsoleThemeStyle.LevelError] = "\u001b[41;1m\u001b[37;1m",
        [ConsoleThemeStyle.LevelFatal] = "\u001b[46;1m\u001b[37;1m"
    });

    public static CustomConsoleTheme CustumLight => new CustomConsoleTheme(new Dictionary<ConsoleThemeStyle, string>
    {
        [ConsoleThemeStyle.Text] = "\u001b[30m",
        [ConsoleThemeStyle.SecondaryText] = "\u001b[36m",
        [ConsoleThemeStyle.TertiaryText] = "\u001b[30;1m",
        [ConsoleThemeStyle.Invalid] = "\u001b[31;1m",
        [ConsoleThemeStyle.Null] = "\u001b[31m",
        [ConsoleThemeStyle.Name] = "\u001b[35;1m",
        [ConsoleThemeStyle.String] = "\u001b[33m",
        [ConsoleThemeStyle.Number] = "\u001b[33;1m",
        [ConsoleThemeStyle.Boolean] = "\u001b[31;1m",
        [ConsoleThemeStyle.Scalar] = "\u001b[37m",
        [ConsoleThemeStyle.LevelVerbose] = "\u001b[37m",
        [ConsoleThemeStyle.LevelDebug] = "\u001b[44;1m\u001b[37;1m",
        [ConsoleThemeStyle.LevelInformation] = "\u001b[42;1m\u001b[37;1m",
        [ConsoleThemeStyle.LevelWarning] = "\u001b[43;1m\u001b[37;1m",
        [ConsoleThemeStyle.LevelError] = "\u001b[41;1m\u001b[37;1m",
        [ConsoleThemeStyle.LevelFatal] = "\u001b[46;1m\u001b[37;1m"
    });
}
