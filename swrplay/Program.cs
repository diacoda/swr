using System;
using System.Globalization;
using System.IO;
using Swr.Data;
using Swr.Investment;
using Swr.Simulation;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
            services.AddTransient<Scenario>(); // Register Scenario for DI
            services.AddTransient<Testing>();
        })
        .Build();

    // Input file path (update this as per your file location)
    string inputFilePath = "../stock-data/cpi.csv";
    string outputFilePath = "../stock-data/transformed_cpi.csv";

    // Read all lines from the input file
    string[] lines = File.ReadAllLines(inputFilePath);

    // Create the output file and write the header
    using (StreamWriter writer = new StreamWriter(outputFilePath))
    {
        bool first = true;
        double oldValue = 0;
        foreach (string line in lines)
        {
            // Skip empty lines
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Split the line into date fraction and value
            string[] parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2) continue; // Skip malformed lines

            // Parse date fraction and value
            string[] dates = parts[0].Split('.', StringSplitOptions.RemoveEmptyEntries);
            int year = int.Parse(dates[0]);
            int month = int.Parse(dates[1]);
            double value = double.Parse(parts[1], CultureInfo.InvariantCulture);
            double inflation;
            if (first)
            {
                first = false;
                inflation = 1;
            }
            else
            {
                inflation = (value - oldValue) / oldValue * 100;
            }
            oldValue = value;
            // Write the transformed data to the output file
            writer.WriteLine($"{month},{year},{inflation:F2}");
        }
    }

    Console.WriteLine($"Transformed data saved to {outputFilePath}");
}
catch (Exception ex)
{
    Log.Fatal(ex, "The application failed to start");
}
finally
{
    Log.CloseAndFlush(); // Ensure all logs are flushed before exit
}
