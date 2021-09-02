using Serilog.Sinks.SystemConsole.Themes;

namespace RestIdentity.Logging;

public sealed class CustomConsoleTheme : ConsoleTheme
{
    public override bool CanBuffer => true;
    protected override int ResetCharCount => "\x001B[0m".Length;

    private readonly IReadOnlyDictionary<ConsoleThemeStyle, string> _colorStyles;

    public CustomConsoleTheme(IReadOnlyDictionary<ConsoleThemeStyle, string> colorsStyles)
    {
        _colorStyles = colorsStyles;
    }

    public override int Set(TextWriter output, ConsoleThemeStyle style)
    {
        if (!_colorStyles.TryGetValue(style, out string str))
            return 0;

        output.Write(str);
        return str.Length;
    }

    public override void Reset(TextWriter output)
    {
        output.Write("\x001B[0m");
    }
}
