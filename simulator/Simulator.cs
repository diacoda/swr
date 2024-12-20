using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Swr.Config;

public class Simulator
{
    public Simulator(ILogger<Simulator> logger, IConfiguration configuration)
    {
        Dictionary<string, ScenarioConfig> scenarios = configuration.GetSection("Simulations").Get<Dictionary<string, ScenarioConfig>>();
        int i = 0;
    }
}