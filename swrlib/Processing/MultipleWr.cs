using Swr.Simulation;

namespace Swr.Processing;
public class MultipleWr
{
    public static void Simulate(Scenario scenario)
    {
        var wrValues = Enumerable.Range(0, 9).Select(i => 3.0f + i * 0.25f).ToList();

        List<Results> allYearlyResults = new List<Results>(wrValues.Count());
        List<Results> allMonthlyResults = new List<Results>(wrValues.Count());

        foreach (var _ in wrValues)
        {
            allYearlyResults.Add(new Results());
            allMonthlyResults.Add(new Results());
        }

        // Parallel processing
        Parallel.ForEach(wrValues.Select((wr, i) => (wr, i)), tuple =>
        {
            var (wr, i) = tuple;

            // Copy the scenario for thread safety
            Scenario s = new Scenario(scenario);
            s.WithdrawalRate = wr;
            s.WithdrawFrequency = 12;
            // Run yearly simulation
            allYearlyResults[i] = s.Simulate();

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
        i = 0;
        var sortedYearly = allYearlyResults.OrderBy(r => r.SuccessRate);
        foreach (Results r in sortedYearly)
        {
            r.Print("Y" + i.ToString());
            i++;
        }
    }
}