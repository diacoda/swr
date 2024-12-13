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

    // data comes from a file
    string inputFilePath = "us_nominal_stocks.txt";
    string outputFilePath = "us_stocks_n.csv";

    // Read all lines from the input file
    string[] lines = File.ReadAllLines(inputFilePath);

    // Create the output file and write the header
    using (StreamWriter writer = new StreamWriter(outputFilePath))
    {
        double initialPrice = 0.0;
        double nominalCumulativeReturn = 0.0;
        double totalReturn = 100.0;
        bool first = true;

        foreach (string line in lines)
        {
            // Skip empty lines
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            // Split the line into date fraction and value
            string[] parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 3)
            {
                continue; // Skip malformed lines
            }

            // Parse date fraction and value
            string[] dates = parts[0].Split('.', StringSplitOptions.RemoveEmptyEntries);
            int year = int.Parse(dates[0]);
            int month = int.Parse(dates[1]);
            double price = double.Parse(parts[1], CultureInfo.InvariantCulture);
            double dividend = double.Parse(parts[2], CultureInfo.InvariantCulture);

            if (first)
            {
                initialPrice = price;
                first = false;
                writer.WriteLine($"{month},{year},{totalReturn:F2}");
                continue;
            }
            // Calculate dividend yield and update total return index
            double dividendYield = dividend / 12 / price;
            totalReturn *= (1 + dividendYield);

            // Calculate cumulative return
            nominalCumulativeReturn = totalReturn * (price / initialPrice);

            // Write the transformed data to the output file
            writer.WriteLine($"{month},{year},{nominalCumulativeReturn:F2}");
        }
    }

    Console.WriteLine($"Transformed data saved to {outputFilePath}");

    // Input Data: Dates, Nominal Prices, and CPI Values
    //List<string> dates = new List<string> { "1871-01", "1871-02", "1871-03", "1871-04", "1871-05", "1871-06", "1871-07", "1871-08", "1871-09", "1871-10", "1871-11", "1871-12" };
    //List<double> nominalPrices = new List<double> { 4.44, 4.50, 4.61, 4.74, 4.86, 4.82, 4.73, 4.79, 4.84, 4.59, 4.64, 4.74 }; // Nominal S&P 500 prices
    //List<double> cpiValues = new List<double> { 12.464, 12.845, 13.035, 12.559, 12.274, 12.083, 12.083, 11.893, 12.179, 12.369, 12.369, 12.654 };        // CPI values
    //List<double> dividends = new List<double> { 0.26, 0.26, 0.26, 0.26, 0.26, 0.26, 0.26, 0.26, 0.26, 0.26, 0.26, 0.26 };      // Monthly dividends

    /*
        // Input file path (update this as per your file location)
        string inputFilePath = "us_stocks.txt";
        string outputFilePath = "us_stocks.csv";

        // Read all lines from the input file
        string[] lines = File.ReadAllLines(inputFilePath);

        // Create the output file and write the header
        using (StreamWriter writer = new StreamWriter(outputFilePath))
        {
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
                float value = float.Parse(parts[1], CultureInfo.InvariantCulture);
                // Write the transformed data to the output file
                writer.WriteLine($"{month},{year},{value:F2}");
            }
        }

        Console.WriteLine($"Transformed data saved to {outputFilePath}");
        */
}
catch (Exception ex)
{
    Log.Fatal(ex, "The application failed to start");
}
finally
{
    Log.CloseAndFlush(); // Ensure all logs are flushed before exit
}
