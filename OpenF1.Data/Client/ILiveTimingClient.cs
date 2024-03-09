using Microsoft.AspNet.SignalR.Client;

namespace OpenF1.Data;

/// <summary>
/// A client which interacts with the SignalR data stream provided by F1. 
/// </summary>
public interface ILiveTimingClient
{
    public HubConnection? Connection { get; }

    public Queue<string> RecentDataPoints { get; }

    /// <summary>
    /// Starts the timing client, which establishes a connection to the real F1 live timing data source.
    /// </summary>
    Task StartAsync();
}
