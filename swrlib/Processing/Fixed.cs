using Swr.Data;
using Swr.Investment;
using Swr.Simulation;

namespace Swr.Processing;

public class Fixed
{
    public Fixed(string[] args)
    {
    }

    public static void Simulate(Scenario scenario)
    {
        scenario.WithdrawalFrequency = WithdrawalFrequency.YEARLY;
        Results yearlyResults = scenario.Simulate();
        yearlyResults.Print("Yearly");

        scenario.WithdrawFrequency = 1;
        Results monthlyResults = scenario.Simulate();
        monthlyResults.Print("Monthly");
    }
}