using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Serilog;

class Program
{
    static async Task Main(string[] args)
    {
        var logFilePath = "logs/simulator.log";

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information() // Set minimum log level
            .WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}{Exception}")          // Log to the console
            .WriteTo.File(logFilePath,  // Log to a file
                          rollingInterval: RollingInterval.Day, // Roll logs daily
                          retainedFileCountLimit: 7,
                          outputTemplate: "{Message:lj}{NewLine}{Exception}")           // Retain the last 7 log files
            .CreateLogger();

        var builder = Host.CreateDefaultBuilder(args)
            .UseSerilog() // Use Serilog as the logging provider
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<MyApp>();
                services.AddScoped<Simulator>();
            });
        var app = builder.Build();
        await app.Services.GetRequiredService<MyApp>().StartAsync();
        Console.WriteLine("Done!");
    }
}

class MyApp
{
    private IHostEnvironment _env;
    private ILogger<MyApp> _logger;
    private IConfiguration _configuration;
    private Simulator _simulator;

    public MyApp(   IHostEnvironment env, 
                    ILogger<MyApp> logger, 
                    IConfiguration configuration,
                    Simulator simulator)
    {
        _env = env;
        _logger = logger;
        _configuration = configuration;
        _simulator = simulator;
    }

    public Task StartAsync()
    {
        _logger.LogInformation("logging");
        Console.WriteLine(_configuration["key"]);
        Console.WriteLine(_env.ContentRootPath);

        return Task.CompletedTask;
    }
}