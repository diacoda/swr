using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swr.Config;
using Swr.Simulation;
public class App
{
    private IHostEnvironment _env;
    private ILogger<App> _logger;
    private IConfiguration _configuration;
    private Simulator _simulator;

    public App(IHostEnvironment env,
                ILogger<App> logger,
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
        IConfigurationSection simulationsSection = _configuration.GetSection("Simulations");
        Dictionary<Command, ScenarioConfig>? scenarios = simulationsSection.Get<Dictionary<Command, ScenarioConfig>>();
        if (scenarios == null || scenarios.Count == 0)
        {
            throw new ApplicationException("No config");
        }
        foreach (Command command in scenarios.Keys)
        {
            Validate(command, scenarios[command]);
            switch (command)
            {
                case Command.allocation:
                    break;
                case Command.withdrawalRate:

                    break;
                default:
                    throw new ApplicationException("Invalid command");
            }
        }
        return Task.CompletedTask;
    }

    public bool Validate(Command command, ScenarioConfig config)
    {
        switch (command)
        {
            case Command.withdrawalRate:

                break;
            case Command.allocation:
                break;
        }
        return false;
    }
}