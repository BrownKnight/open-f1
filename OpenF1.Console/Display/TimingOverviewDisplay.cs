using OpenF1.Data;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace OpenF1.Console;

public class TimingOverviewDisplay(
    State state,
    RaceControlMessageProcessor raceControlMessages,
    TimingDataProcessor timingData,
    DriverListDataProcessor driverList,
    TrackStatusProcessor trackStatusProcessor,
    LapCountProcessor lapCountProcessor
) : IDisplay
{
    public Screen Screen => Screen.TimingOverview;

    private Style _personalBest = new(background: Color.Green);
    private Style _overallBest = new(background: Color.Purple);
    private Style _normal = new();

    public Task<IRenderable> GetContentAsync()
    {
        var statusPanel = GetStatusPanel();
        var raceControlPanel = GetRaceControlPanel();
        var timingTower = GetTimingTower();

        var layout = new Layout("Root").SplitRows(
            new Layout("Timing Tower", timingTower),
            new Layout("Info").SplitColumns(
                new Layout("Status", statusPanel),
                new Layout("Race Control Messages", raceControlPanel)
            )
        );

        layout["Info"].Size = 5;
        layout["Info"]["Status"].Size = 19;

        return Task.FromResult<IRenderable>(layout);
    }

    private IRenderable GetTimingTower()
    {
        var table = new Table();
        table.AddColumns("", "Interval", "Last Lap", "S1", "S2", "S3");
        if (timingData.LatestLiveTimingDataPoint is null)
        {
            return new Text("No Timing");
        }
        foreach (
            var (driverNumber, line) in timingData.LatestLiveTimingDataPoint.Lines.OrderBy(x =>
                x.Value.Line
            )
        )
        {
            var driver = driverList.Latest?.GetValueOrDefault(driverNumber) ?? new();
            table.AddRow(
                new Text($"{line.Position, 2} {driver.RacingNumber, 2} {driver.Tla}"),
                new Text(line.IntervalToPositionAhead?.Value ?? ""),
                new Text(line.LastLapTime?.Value ?? "NULL", GetStyle(line.LastLapTime)),
                new Text(
                    line.Sectors.GetValueOrDefault("0")?.Value ?? "",
                    GetStyle(line.Sectors.GetValueOrDefault("0"))
                ),
                new Text(
                    line.Sectors.GetValueOrDefault("1")?.Value ?? "",
                    GetStyle(line.Sectors.GetValueOrDefault("1"))
                ),
                new Text(
                    line.Sectors.GetValueOrDefault("2")?.Value ?? "",
                    GetStyle(line.Sectors.GetValueOrDefault("2"))
                )
            );
        }

        table.NoBorder();

        return table;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Style",
        "IDE0046:Convert to conditional expression",
        Justification = "Harder to read"
    )]
    private Style GetStyle(TimingDataPoint.Driver.LapSectorTime? time)
    {
        if (time is null)
            return _normal;
        if (time.OverallFastest ?? false)
            return _overallBest;
        if (time.PersonalFastest ?? false)
            return _personalBest;
        return _normal;
    }

    private IRenderable GetRaceControlPanel()
    {
        var table = new Table();
        table.NoBorder();
        table.AddColumns("Timestamp", "Message");
        table.HideHeaders();

        var messages = raceControlMessages
            .RaceControlMessages.Messages.OrderByDescending(x => x.Value.Utc)
            .Skip(state.CursorOffset)
            .Take(5);

        foreach (var (key, value) in messages)
        {
            table.AddRow($"{value.Utc:T}", value.Message);
        }
        return new Panel(table)
        {
            Header = new PanelHeader("Race Control Messages"),
            Expand = true
        };
    }

    private IRenderable GetStatusPanel()
    {
        var items = new List<IRenderable>();
        if (lapCountProcessor.Latest is not null)
        {
            items.Add(
                new Text(
                    $"LAP {lapCountProcessor.Latest.CurrentLap}/{lapCountProcessor.Latest.TotalLaps}"
                )
            );
        }

        if (trackStatusProcessor.Latest is not null)
        {
            var style = trackStatusProcessor.Latest.Status switch
            {
                "1" => new Style(background: Color.Green),
                "2" => new Style(background: Color.Yellow),
                _ => Style.Plain
            };
            items.Add(
                new Text(
                    $"{trackStatusProcessor.Latest.Status} {trackStatusProcessor.Latest.Message}",
                    style
                )
            );
        }

        var rows = new Rows(items);
        return new Panel(rows) { Header = new PanelHeader("Status"), Expand = true };
    }
}
