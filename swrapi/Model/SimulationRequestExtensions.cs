using Swr.Simulation;
using Swr.Investment;

namespace swrapi.Model;

public static class SimulationRequestExtensions
{
    public static Scenario ToScenario(this SimulationRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        Scenario scenario = new Scenario();
        scenario.Years = request.Years;
        scenario.StartYear = request.StartYear;
        scenario.EndYear = request.EndYear;
        Portfolio portfolio = new Portfolio(request.Portfolio);
        scenario.Portfolio = portfolio;
        scenario.Inflation = request.Inflation;
        scenario.WithdrawalRate = request.WithdrawalRate;
        scenario.Fees = request.Fees;
        scenario.WithdrawalMethod = request.WithdrawalMethod;
        scenario.WithdrawFrequency = request.WithdrawalFrequency == WithdrawalFrequency.Monthly ? 12 : 1;
        return scenario;
    }
}
