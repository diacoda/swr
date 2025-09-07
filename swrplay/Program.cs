using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Swr.Integration;
using Swr.Play;
using Swr.Simulation;
using Swr.Investment;
using Swr.Model;
using Swr.Data;
using Swr.Processing;
using OoplesFinance.YahooFinanceAPI.Models;


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
    var solutionFolder = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..");
    Environment.CurrentDirectory = Path.GetFullPath(solutionFolder);

    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog() // Use Serilog as the logging provider
        .ConfigureServices((context, services) =>
        {
            services.AddSingleton<Scenario>();
            services.AddSingleton<Simulator>();
            services.AddSingleton<MarketData>();
        })
        .Build();

    Scenario scenario = host.Services.GetRequiredService<Scenario>();
    scenario.TimeHorizon = 30;
    scenario.StartYear = 1871;
    scenario.EndYear = 2023;
    //string allocation = "us_stocks:75;us_bonds:5;cash:10;gold2023:10";
    string allocation = "us_stocks:80;;cash:10;gold2023:10";
    scenario.Portfolio = new Portfolio(allocation);
    scenario.Inflation = "us_inflation";
    scenario.WithdrawalFrequency = WithdrawalFrequency.MONTHLY;
    scenario.WithdrawalMethod = WithdrawalMethod.STANDARD;
    scenario.ExpenseRatio = 0.003f;
    scenario.MinimumWithdrawalRate = 3.0f;
    scenario.InflationAdjustedFinalTarget = true;
    scenario.FinalTargetPercentage = 0.1f;
    scenario.UseCashWithdrawal = false;
    scenario.InitialCash = 0;
    //scenario.UseCashWithdrawal = true;
    //scenario.InitialCash = 80;
    scenario.Values = DataLoader.LoadValues(scenario.Portfolio.Allocations);
    scenario.InflationData = DataLoader.LoadInflation(scenario.Values, scenario.Inflation);
    bool r3 = scenario.PrepareExchangeRates("usd");
    //3.4 (0) 3.5(1), 3.65 (2), 3.7 (3)
    scenario.WithdrawalRate = 3.4f;
    Fixed.Simulate(scenario);

    var service = host.Services.GetRequiredService<Simulator>();
    WithdrawalRates withdrawalRates = service.CalculateWithdrawalRates(scenario);

    //var service = host.Services.GetRequiredService<MarketData>();
    //service.TransformCanadaCPI();
    //string? filePath = await service.GetData("GC=F", TickerFrequency.Daily);
    //service.TransformExchangeRates();

    scenario.WithdrawalRate = withdrawalRates.Fail0Percent;
    Fixed.Simulate(scenario);

    scenario.WithdrawalRate = withdrawalRates.Fail1Percent;
    Fixed.Simulate(scenario);

    scenario.WithdrawalRate = withdrawalRates.Fail2Percent;
    Fixed.Simulate(scenario);

    scenario.WithdrawalRate = withdrawalRates.Fail5Percent;
    Fixed.Simulate(scenario);
}
catch (Exception ex)
{
    Log.Fatal(ex, "The application failed to start");
}
finally
{
    Log.CloseAndFlush(); // Ensure all logs are flushed before exit
}