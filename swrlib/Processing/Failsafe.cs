using Swr.Simulation;

namespace Swr.Processing;

public class Failsafe
{
    public static void Simulate(Scenario scenario)
    {
        float startWr = 6.0f;
        float endWr = 2.0f;
        float step = 0.01f;


        for (float wr = startWr; wr >= endWr; wr -= step)
        {
            scenario.WithdrawalRate = wr;
            // default is scenario.WithdrawFrequency = 1; therefore monthly
            Results monthlyResults = scenario.Simulate();

            if (monthlyResults.SuccessRate >= scenario.SuccessRateLimit)
            {
                Console.WriteLine(wr);
                return;
            }
        }
        Console.WriteLine("0");
    }
}