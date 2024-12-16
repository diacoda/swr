using System;
using System.Globalization;
using System.IO;
using Swr.Data;
using Swr.Investment;
using Swr.Simulation;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Swr.Integration;
using Integration;

var logFilePath = "logs/swrplay.log";

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information() // Set minimum log level
    .WriteTo.Console()          // Log to the console
    .WriteTo.File(logFilePath,  // Log to a file
                  rollingInterval: RollingInterval.Day, // Roll logs daily
                  retainedFileCountLimit: 7)           // Retain the last 7 log files
    .CreateLogger();

try
{
    Log.Information("started");
    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog() // Use Serilog as the logging provider
        .ConfigureServices((context, services) =>
        {
            services.AddTransient<MarketData>();
            services.AddScoped<MarketData>();
        })
        .Build();

    var service = host.Services.GetRequiredService<MarketData>();
    //service.TransformCanadaCPI();
    //string? filePath = await service.GetData("GC=F", TickerFrequency.Daily);
    service.TransformExchangeRates();
    Console.WriteLine("loaded");
}
catch (Exception ex)
{
    Log.Fatal(ex, "The application failed to start");
}
finally
{
    Log.CloseAndFlush(); // Ensure all logs are flushed before exit
}
