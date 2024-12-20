using Swr.Simulation;
using Swr.Investment;

namespace swrapi.Model;

public static class SimulationRequestExtensions
{
    public static Scenario ToScenario(this SimulationRequest request, ILogger<Scenario> logger)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        Scenario scenario = new Scenario(logger);
        scenario.TimeHorizon = request.TimeHorizon;
        scenario.StartYear = request.StartYear;
        scenario.EndYear = request.EndYear;
        Portfolio portfolio = new Portfolio(request.Portfolio);
        scenario.Portfolio = portfolio;
        scenario.Inflation = request.Inflation;
        scenario.Fees = request.Fees;
        scenario.WithdrawalMethod = request.WithdrawalMethod;
        scenario.WithdrawalFrequency = request.WithdrawalFrequency;
        return scenario;
    }
}
