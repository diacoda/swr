namespace Swr.Processing;
using Swr.Data;
using Swr.Investment;
using Swr.Simulation;

public class SafeWithdrawal
{
    public static void Simulate(Scenario scenario)
    {
        scenario.WithdrawFrequency = 1;

        float best_wr = 0.0f;
        Results best_results;

        for (float wr = 6.0f; wr >= 2.0f; wr -= 0.01f)
        {
            scenario.WithdrawalRate = wr;

            Results results = scenario.Simulate();
            //results.Print(wr.ToString());

            if (results.Message.Length > 0)
            {
                Console.WriteLine(results.Message);
            }

            if (results.Error)
            {
                Console.WriteLine("Error");
                break;
            }

            if (results.SuccessRate > scenario.SuccessRateLimit)
            {
                best_results = results;
                best_wr = wr;
                results.Print(best_wr.ToString());
                break;
            }
        }
    }
}