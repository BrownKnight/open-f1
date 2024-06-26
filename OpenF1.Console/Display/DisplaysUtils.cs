using OpenF1.Data;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace OpenF1.Console;

public static class DisplayUtils
{
    public static readonly Style STYLE_NORMAL = new(foreground: Color.White);
    public static readonly Style STYLE_INVERT =
        new(foreground: Color.Black, background: Color.White);
    public static readonly Style STYLE_PB =
        new(foreground: Color.White, background: new Color(0, 118, 0));
    public static readonly Style STYLE_BEST =
        new(foreground: Color.White, background: new Color(118, 0, 118));

    public static IRenderable DriverTag(
        DriverListDataPoint.Driver driver,
        TimingDataPoint.Driver line,
        bool selected
    )
    {
        var lineStyle = selected ? STYLE_INVERT : STYLE_NORMAL;
        return new Markup($"{line.Line, 2} {MarkedUpDriverNumber(driver)}", lineStyle);
    }

    public static string MarkedUpDriverNumber(DriverListDataPoint.Driver driver) =>
        $"[#{driver.TeamColour ?? "000000"} bold]{driver.RacingNumber, 2} {driver.Tla ?? "UNK"}[/]";
}
