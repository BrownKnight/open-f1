using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace OpenF1.Data;

public class JsonTimingClient(ITimingService timingService, ILogger<JsonTimingClient> logger)
    : IJsonTimingClient
{
    private string _directory = "";
    private CancellationTokenSource _cts = new();
    private Thread? _thread;

    public Task? ExecuteTask { get; private set; }

    /// <inheritdoc />
    public IEnumerable<string> GetDirectoryNames() =>
        Directory
            .GetDirectories("./SimulationData")
            .Where(x =>
                Directory
                    .GetFiles(x)
                    .All(fileName =>
                        fileName.EndsWith("live.txt") || fileName.EndsWith("subscribe.txt")
                    )
            );

    public async Task StartAsync(string directory)
    {
        _directory = directory;
        _cts.Cancel();
        _cts = new CancellationTokenSource();
        ExecuteTask = Task.Factory.StartNew(() => ExecuteAsync(_cts.Token));
        await timingService.StartAsync();
    }

    public async Task StopAsync()
    {
        _cts.Cancel();
        ExecuteTask = null;
        await timingService.StopAsync();
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // Handle the dump of data we receive at subscription time
        var subscriptionData = await File.ReadAllTextAsync(
                Path.Join(_directory, "/subscribe.txt"),
                cancellationToken
            )
            .ConfigureAwait(false);

        timingService.ProcessSubscriptionData(subscriptionData);

        var subscriptionHeartbeat = JsonNode
            .Parse(subscriptionData)
            ?["Heartbeat"]?.Deserialize<HeartbeatDataPoint>();
        if (subscriptionHeartbeat is not null)
        {
            timingService.Delay = DateTimeOffset.UtcNow - subscriptionHeartbeat.Utc;
            logger.LogInformation(
                $"Calculated a delay of {timingService.Delay} between now and {subscriptionHeartbeat.Utc:s}"
            );
        }
        else
        {
            logger.LogWarning($"Unable to calculate a delay for this simulation data");
        }

        // Handle the real-time data
        var lines = File.ReadLinesAsync(Path.Join(_directory, "/live.txt"), cancellationToken);

        await foreach (var line in lines)
        {
            try
            {
                var (type, data, timestamp) = ProcessLine(line);
                await timingService.EnqueueAsync(type, data, timestamp).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to handle data: {line}");
            }
        }
    }

    private (string type, string? data, DateTimeOffset timestamp) ProcessLine(string line)
    {
        var json = JsonNode.Parse(line);
        var parts = json?["A"]?.AsArray() ?? json!.AsArray();
        var data =
            parts[1]!.GetValueKind() == JsonValueKind.Object
                ? parts[1]!.ToJsonString()
                : parts[1]!.ToString();
        return (parts[0]!.ToString(), data, DateTimeOffset.Parse(parts[2]!.ToString()));
    }
}
