using Swr.Model;

namespace Swr.Config;
public class SimulationsSection
{
    public Dictionary<string, SimulationRequest>? Simulations { get; set; }
}