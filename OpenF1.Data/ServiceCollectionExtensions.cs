﻿using AutoMapper.EquivalencyExpression;
using Microsoft.Extensions.DependencyInjection;
using OpenF1.Data.AutoMapper;

namespace OpenF1.Data;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddLiveTiming(this IServiceCollection collection)
    {
        collection
            .AddAutoMapper(cfg => cfg.AddCollectionMappers(), typeof(TimingDataPointConfiguration).Assembly)
            .AddLiveTimingClient()
            .AddLiveTimingProcessors();

        return collection;
    }
}
