using Swr.Simulation;

namespace Swr.Processing;

public class Vanguard
{
    /*
    else if (args[n] == "current") {
            scenario.wmethod = swr::WithdrawalMethod::CURRENT;
            scenario.minimum = 0.04f;
        } else if (args[n] == "vanguard") {
            scenario.wmethod = swr::WithdrawalMethod::VANGUARD;
            scenario.minimum = 0.04f;
        } else if (args[n] == "current3") {
            scenario.wmethod = swr::WithdrawalMethod::CURRENT;
            scenario.minimum = 0.03f;
        } else if (args[n] == "vanguard3") {
            scenario.wmethod = swr::WithdrawalMethod::VANGUARD;
            scenario.minimum = 0.03f;*/
    public static void Simulate(Scenario scenario)
    {
        Results results = scenario.Simulate();

        var wrValues = Enumerable.Range(0, 29).Select(i => 3.0f + i * 0.1f).ToList();

        List<Results> allMonthlyResults = new List<Results>(wrValues.Count());

        foreach (var _ in wrValues)
        {
            allMonthlyResults.Add(new Results());
        }

        // Parallel processing
        Parallel.ForEach(wrValues.Select((wr, i) => (wr, i)), tuple =>
        {
            var (wr, i) = tuple;

            // Copy the scenario for thread safety
            Scenario s = new Scenario(scenario);
            s.WR = wr;
            // Update scenario for monthly simulation
            s.WithdrawFrequency = 1;

            allMonthlyResults[i] = s.Simulate();
        });

        int i = 0;
        var sortedMonthly = allMonthlyResults.OrderBy(r => r.SuccessRate);
        foreach (Results r in sortedMonthly)
        {
            r.Print("M" + i.ToString());
            i++;
        }
    }
}