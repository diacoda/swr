// See https://aka.ms/new-console-template for more information
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
        })
        .Build();

    // Resolve Scenario using DI
    using var scope = host.Services.CreateScope(); // Create a scope for resolving services
    var serviceProvider = scope.ServiceProvider;
    Scenario scenario = scope.ServiceProvider.GetRequiredService<Scenario>();

    Portfolio portfolio = new Portfolio("us_stocks:50;us_stocks_orig:50");
    scenario.Portfolio = portfolio;
    scenario.Values = DataLoader.LoadValues(scenario.Portfolio.Allocations);
    foreach (DataVector dv in scenario.Values)
    {
        string fileName = $"{dv.Name}.txt"; // Create a file name based on the DataVector's Name
        using (StreamWriter writer = new StreamWriter(fileName))
        {
            foreach (Item item in dv.Data)
            {
                // Assuming `Item` has properties like `Month`, `Year`, and `Value`
                writer.WriteLine($"{item.Month},{item.Year},{item.Value}");
            }
        }
    }

}
catch (Exception ex)
{
    Log.Fatal(ex, "The application failed to start");
}
finally
{
    Log.CloseAndFlush(); // Ensure all logs are flushed before exit
}
