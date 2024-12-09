using Microsoft.Extensions.DependencyInjection;
using Swr.Simulation;

namespace Swr.Processing;

public class Vanguard
{
    public static void Simulate(Scenario scenario, IServiceProvider serviceProvider)
    {
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
            // Resolve a new Scenario from DI
            using var scope = serviceProvider.CreateScope();
            Scenario s = scope.ServiceProvider.GetRequiredService<Scenario>();

            s.CopyFrom(scenario);
            s.WithdrawalRate = wr;
            // Update scenario for monthly simulation
            s.WithdrawFrequency = 1;

            allMonthlyResults[i] = s.Simulate();
            allMonthlyResults[i].WithdrawalRate = s.WithdrawalRate;
            allMonthlyResults[i].WithdrawalMethod = s.WithdrawalMethod;
        });

        int i = 0;
        var sortedMonthly = allMonthlyResults.OrderBy(r => r.SuccessRate);
        foreach (Results r in sortedMonthly)
        {
            r.Print($"M{i}: Method: {r.WithdrawalMethod}, Rate: {r.WithdrawalRate}");
            i++;
        }
    }
}