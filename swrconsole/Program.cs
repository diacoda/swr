using Microsoft.Extensions.Hosting;
using Serilog;
using System.Globalization;
using Swr.Data;
using Swr.Investment;
using Swr.Processing;
using Swr.Simulation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Swr.Model;

var logFilePath = "logs/swrconsole.log";

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information() // Set minimum log level
    .WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}{Exception}")          // Log to the console
    .WriteTo.File(logFilePath,  // Log to a file
                  rollingInterval: RollingInterval.Day, // Roll logs daily
                  retainedFileCountLimit: 7,
                  outputTemplate: "{Message:lj}{NewLine}{Exception}")           // Retain the last 7 log files
    .CreateLogger();

try
{
    Log.Information("started");
    //await host.RunAsync();
    // Build a config object, using env vars and JSON providers.
    IConfigurationRoot config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddEnvironmentVariables()
        .Build();

    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog() // Use Serilog as the logging provider
        .ConfigureServices((context, services) =>
        {
            services.AddTransient<Scenario>();
            services.AddScoped<Scenario>();
        })
        .Build();
    var s = config["key"];

    // Resolve Scenario using DI
    Scenario scenario = host.Services.GetRequiredService<Scenario>();

    if (args.Length == 0)
    {
        return;
    }

    scenario.TimeHorizon = 30;
    scenario.StartYear = 1871;
    scenario.EndYear = 2023;
    string allocation = "us_stocks:100";
    scenario.Portfolio = new Portfolio(allocation);
    scenario.Inflation = "us_inflation";
    scenario.WithdrawalFrequency = WithdrawalFrequency.MONTHLY;
    scenario.WithdrawalMethod = WithdrawalMethod.STANDARD;
    scenario.WithdrawalRate = 4.0f;
    scenario.Fees = 0.003f;
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
    Fixed.Simulate(scenario);

    Vanguard.Simulate(scenario, host.Services);

    scenario.WithdrawalMethod = WithdrawalMethod.CURRENT;
    Vanguard.Simulate(scenario, host.Services);

    scenario.WithdrawalMethod = WithdrawalMethod.VANGUARD;
    Vanguard.Simulate(scenario, host.Services);

    Dictionary<string, object> arguments = ParseArguments(args);

    string? command = arguments["command"].ToString();
    if (String.IsNullOrEmpty(command))
    {

    }

    scenario.TimeHorizon = (int)arguments["TimeHorizon"];
    scenario.StartYear = (int)arguments["start"];
    scenario.EndYear = (int)arguments["end"];
    if (arguments.TryGetValue("portfolio", out var portfolio) &&
        !string.IsNullOrEmpty(portfolio?.ToString()))
    {
        scenario.Portfolio = new Portfolio(portfolio.ToString()!);
    }
    else
    {
        // default to 100% US Stocks
        scenario.Portfolio = new Portfolio("us_stocks:100;");
    }
    if (arguments.TryGetValue("inflation", out var inflationValue) &&
        inflationValue is string inflationStr && !string.IsNullOrEmpty(inflationStr))
    {
        scenario.Inflation = inflationStr;
    }
    if (arguments.TryGetValue("withdrawalRate", out var wr))
    {
        scenario.WithdrawalRate = (float)wr;
    }
    if (arguments["fees"] != null)
    {
        // there is a default, 0.01
        scenario.Fees = (float)arguments["fees"] / 100.0f;
    }
    if (arguments.TryGetValue("minimumWithdrawalRate", out var minimumWithdrawalRate))
    {
        scenario.MinimumWithdrawalRate = (float)minimumWithdrawalRate;
    }
    if (arguments.TryGetValue("limit", out var limit))
    {
        // there is a default, 95
        scenario.SuccessRateLimit = (float)limit;
    }
    if (arguments.TryGetValue("rebalancing", out var rebalancingValue) &&
        rebalancingValue is string rebalancingStr &&
        Enum.TryParse(rebalancingStr, true, out Rebalancing rebalancing))
    {
        scenario.Rebalance = rebalancing;
    }
    if (arguments.TryGetValue("withdrawalFrequency", out var withdrawalFrequencyValue) &&
        withdrawalFrequencyValue is string withdrawalFrequencyStr &&
        Enum.TryParse(withdrawalFrequencyStr, true, out WithdrawalFrequency withdrawalFrequency))
    {
        scenario.WithdrawalFrequency = withdrawalFrequency;
    }

    scenario.Values = DataLoader.LoadValues(scenario.Portfolio.Allocations);
    scenario.InflationData = DataLoader.LoadInflation(scenario.Values, scenario.Inflation);
    bool r = scenario.PrepareExchangeRates("usd");

    switch (command)
    {
        case "fixed":

            //Console.WriteLine("Command \"fixed\"");
            //Fixed f = new Fixed(args);
            //f.LoadData();
            Fixed.Simulate(scenario);
            break;
        case "swr":
            //Swr swr = new Swr(args);
            //swr.LoadData();
            SafeWithdrawal.Simulate(scenario);
            break;
        case "multiple_wr":
            MultipleWr.Simulate(scenario, host.Services);
            break;
        case "frequency":
            Frequency.Simulate(scenario, 100, 1);
            break;
        case "failsafe":
            Failsafe.Simulate(scenario);
            break;
        default:
            Console.WriteLine("No command match to execute");
            return;
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





static Dictionary<string, object> ParseArguments(string[] args)
{
    var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    foreach (var arg in args)
    {
        if (arg.StartsWith("--"))
        {
            var keyValue = arg.Substring(2).Split('=', 2);

            if (keyValue.Length == 2)
            {
                string key = keyValue[0];
                string value = keyValue[1];

                // Try parsing the value into the appropriate type
                if (int.TryParse(value, out int intValue))
                {
                    result[key] = intValue;
                }
                else if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
                {
                    result[key] = floatValue;
                }
                else if (bool.TryParse(value, out bool boolValue))
                {
                    result[key] = boolValue;
                }
                else
                {
                    result[key] = value; // Default to string if no other type matches
                }
            }
        }
    }

    return result;
}
