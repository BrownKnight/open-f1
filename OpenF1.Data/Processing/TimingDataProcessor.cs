using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace OpenF1.Data;

public class TimingDataProcessor : IProcessor
{
    private readonly ILiveTimingProvider _liveTimingProvider;
    private readonly IDbContextFactory<LiveTimingDbContext> _dbContextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<TimingDataProcessor> _logger;

    public LiveTimingDataType LiveTimingDataType => LiveTimingDataType.TimingData;

    public TimingDataPoint? LatestLiveTimingDataPoint { get; private set; }

    /// <summary>
    /// Dictionary of { DriverNumber, { Lap, Data } }.
    /// </summary>
    public ConcurrentDictionary<string, ConcurrentDictionary<int, TimingDataPoint.TimingData.Driver>> DriverLapData { get; } = new();

    public TimingDataProcessor(
        ILiveTimingProvider liveTimingProvider,
        IDbContextFactory<LiveTimingDbContext> dbContextFactory,
        IMapper mapper,
        ILogger<TimingDataProcessor> logger)
    {
        _liveTimingProvider = liveTimingProvider;
        _dbContextFactory = dbContextFactory;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task StartAsync()
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var latestDriverLaps = await dbContext.DriverLaps
            .GroupBy(x => x.Line)
            .Select(x => x.OrderByDescending(x => x.NumberOfLaps).First())
            .ToListAsync();

        foreach (var driverLap in latestDriverLaps.Where(x => x != null))
        {
            var laps = new ConcurrentDictionary<int, TimingDataPoint.TimingData.Driver>()
            {
                [driverLap.NumberOfLaps.GetValueOrDefault()] = new()
                {
                    Line = driverLap.Line,
                    NumberOfLaps = driverLap.NumberOfLaps
                }
            };
            DriverLapData.TryAdd(driverLap.DriverNumber, laps);
        }

        _liveTimingProvider.TimingDataReceived += Process;
        _logger.LogInformation("Subscribed to Timing Data stream for processing");
    }

    private async void Process(object? _, TimingDataPoint dataPoint)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        foreach (var (driverNumber, data) in dataPoint.Data.Lines)
        {
            UpdateDriverData(dbContext, driverNumber, data, dataPoint.SessionName);
        }

        if (LatestLiveTimingDataPoint is null)
        {
            LatestLiveTimingDataPoint = _mapper.Map<TimingDataPoint>(dataPoint);
        }
        else
        {
            _mapper.Map(dataPoint, LatestLiveTimingDataPoint);
        }

        _ = await dbContext.SaveChangesAsync();
    }

    private void UpdateDriverData(LiveTimingDbContext dbContext, string driverNumber, TimingDataPoint.TimingData.Driver update, string sessionName)
    {
        var alreadyInDb = true;
        var current = DriverLapData.GetOrAdd(driverNumber, (_) =>
        {
            ConcurrentDictionary<int, TimingDataPoint.TimingData.Driver> newData = new();
            newData.TryAdd(0, update);
            alreadyInDb = false;
            return newData;
        });

        // Check if this is a lap change. If so, we need to start a new lap
        if (update.NumberOfLaps.HasValue && !current.ContainsKey(update.NumberOfLaps.Value))
        {
            if (current.TryAdd(update.NumberOfLaps.Value, update))
            {
                AddUpdateDriverLap(dbContext, driverNumber, sessionName, update, isUpdate: false);
            }
            return;
        }

        // Existing lap, so fetch it and update it
        var latestLap = current.OrderByDescending(x => x.Key).First();
        _mapper.Map(update, latestLap.Value);
        AddUpdateDriverLap(dbContext, driverNumber, sessionName, latestLap.Value, isUpdate: alreadyInDb);
    }

    private void AddUpdateDriverLap(
        LiveTimingDbContext dbContext,
        string driverNumber,
        string sessionName,
        TimingDataPoint.TimingData.Driver toAdd,
        bool isUpdate)
    {
        var newDriverLap = _mapper.Map<DriverLap>(toAdd);
        newDriverLap.DriverNumber = driverNumber;
        newDriverLap.SessionName = sessionName;
        newDriverLap.NumberOfLaps ??= -1;

        if (isUpdate)
        {
            isUpdate = dbContext.DriverLaps.Any(x => x.DriverNumber == driverNumber && x.SessionName == sessionName && x.NumberOfLaps == x.NumberOfLaps);
        }

        if (isUpdate)
        {
            dbContext.DriverLaps.Update(newDriverLap);
        }
        else
        {
            dbContext.DriverLaps.Add(newDriverLap);
        }
    }
}

