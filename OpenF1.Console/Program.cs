﻿using System.Text.Json;
using System.Text.Json.Serialization;
using InMemLogger;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.OpenApi.Models;
using OpenF1.Console;
using OpenF1.Data;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder
    .Configuration.AddJsonFile(
        Path.Join(LiveTimingOptions.BaseDirectory, "config.json"),
        optional: true
    )
    .AddEnvironmentVariables("OPENF1_")
    .Build();

Log.Logger = new LoggerConfiguration()
    .WriteTo.File(
        path: Path.Join(LiveTimingOptions.BaseDirectory, "logs/openf1-console.log"), 
        rollOnFileSizeLimit: true,
        rollingInterval: RollingInterval.Hour)
    .CreateLogger();

builder
    .Services.AddOptions()
    .AddLogging(configure => configure.ClearProviders().AddInMemory().AddSerilog())
    .AddSingleton<ConsoleLoop>()
    .AddSingleton<State>()
    .AddInputHandlers()
    .AddDisplays()
    .AddLiveTiming(builder.Configuration)
    .AddSingleton<INotifyHandler, NotifyHandler>()
    .AddSingleton<ConsoleLoop>()
    .AddHostedService(sp => sp.GetRequiredService<ConsoleLoop>());

builder
    .Services.AddEndpointsApiExplorer()
    .AddSwaggerGen(c =>
    {
        c.CustomSchemaIds(type => type.FullName!.Replace("OpenF1.Data.", string.Empty).Replace("+", string.Empty));
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Open F1 API", Version = "v1" });
    });

builder.Services.Configure<JsonOptions>(x =>
{
    x.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    x.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

app.UseSwagger().UseSwaggerUI();

app.MapSwagger();

app.MapTimingEndpoints();

await app.RunAsync();
