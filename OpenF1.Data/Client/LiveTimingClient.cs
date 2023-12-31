﻿using System.Text.Json;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace OpenF1.Data;

public sealed class LiveTimingClient : ILiveTimingClient, IDisposable
{
    private readonly string[] _topics = new[]
    {
        "Heartbeat",
        // "CarData.z",
        // "Position.z",
        "ExtrapolatedClock",
        "TopThree",
        "TimingStats",
        "TimingAppData",
        "WeatherData",
        "TrackStatus",
        "DriverList",
        "RaceControlMessages",
        "SessionInfo",
        "SessionData",
        "LapCount",
        "TimingData"
    };

    private readonly ILogger<LiveTimingClient> _logger;

    private HubConnection? _connection;
    private bool _disposedValue;

    public LiveTimingClient(ILogger<LiveTimingClient> logger) => _logger = logger;

    public async Task StartAsync(Action<string> eventHandler)
    {
        _logger.LogInformation("Starting Live Timing client");
        
        if (_connection is not null) throw new InvalidOperationException("Timing client already has an open connection.");

        // Fetch the cookie value that is hardcoded below
        // Leaving this here to show how I did it. I don't think I need to change the value yet.
        //var httpClient = new HttpClient();
        //var res = await httpClient.GetAsync("https://livetiming.formula1.com/signalr/negotiate");
        //Console.WriteLine(res.Headers.First(x => x.Key == "Set-Cookie"));

        DisposeConnection();
        _connection = new HubConnection("https://livetiming.formula1.com");
        // Headers taken from the Fast F1 implementation
        _connection.Headers.Add("User-agent", "BestHTTP");
        _connection.Headers.Add("Accept-Encoding", "gzip, identity");
        _connection.Headers.Add("Connection", "keep-alive, Upgrade");
        _connection.Headers.Add("Cookie", "GCLB=CJulhoyzt5qwpgE;");

        _connection.EnsureReconnecting();

        _connection.Error += (ex) => _logger.LogError(ex, "Error in live timing client: {}", ex.ToString());
        _connection.Reconnecting += () => _logger.LogWarning("Live timing client is reconnecting");
        _connection.Received += eventHandler;

        var proxy = _connection.CreateHubProxy("Streaming");

        await _connection.Start();

        _logger.LogInformation("Subscribing to lots of topics");

        await proxy.Invoke("Subscribe", (string a) => Console.WriteLine("SessionInfo " + a), new[] { new[] { "SessionInfo" } });

        var res = await proxy.Invoke<JObject>("Subscribe", new[] { _topics });
        var enumerable = res.AsJEnumerable().Select(x => x.Value<JObject>().ToString());
        foreach (var e in enumerable) 
        {
            Console.WriteLine(e);
        }
        eventHandler(res.ToString());
        _logger.LogInformation("Started Live Timing client");
    }

    private void DisposeConnection()
    {
        _connection?.Dispose();
        _connection = null;
    }

    public void Dispose()
    {
        if (!_disposedValue)
        {
            DisposeConnection();
            _disposedValue = true;
        }
        GC.SuppressFinalize(this);
    }
}

