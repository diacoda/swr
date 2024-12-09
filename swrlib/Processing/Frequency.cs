using Swr.Data;
using Swr.Simulation;

namespace Swr.Processing;

public class Frequency
{
    /// <summary>
    /// Simulates investment scenarios over a given period, calculating various statistics including average net worth, maximum net worth, 
    /// and the best and worst investment results based on different frequencies of monthly buys.
    /// </summary>
    /// <param name="scenario">The investment scenario containing historical data and configurations.</param>
    /// <param name="monthlyBuy">The amount of money to be invested monthly.</param>
    /// <param name="frequency">The frequency at which the monthly investment is made.</param>
    public static void Simulate(Scenario scenario, int monthlyBuy, int frequency)
    {
        // Calculate the total number of months for the simulation period.
        int numberOfMonths = scenario.Years * 12;
        double total = 0;
        double max = 0;
        int simulations = 0;

        // Loop through each starting year within the scenario's range.
        for (int currentYear = scenario.StartYear; currentYear <= scenario.EndYear - scenario.Years; currentYear++)
        {
            // Loop through each month.
            for (int currentMonth = 1; currentMonth <= 12; currentMonth++)
            {
                // Calculate the end year and end month for the current simulation period.
                int endYear = currentYear + (currentMonth - 1 + numberOfMonths - 1) / 12;
                int endMonth = 1 + ((currentMonth - 1) + (numberOfMonths - 1) % 12) % 12;

                int months = 0;

                // Get the data vector for the current year and month.
                DataVector dv = scenario.Values[0].GetDataVector(currentYear, currentMonth);
                IEnumerator<Item> returns = dv.GetEnumerator();
                returns.MoveNext();

                double netWorth = 0;

                // Loop through each month in the simulation period.
                for (int y = currentYear; y <= endYear; y++)
                {
                    for (int m = (y == currentYear ? currentMonth : 1); m <= (y == endYear ? endMonth : 12); m++, months++)
                    {
                        // Check if returns data match the current year and month
                        if (y != returns.Current.Year || m != returns.Current.Month)
                        {
                            Console.WriteLine("no match");
                        }

                        // Adjust the portfolio with the returns
                        netWorth *= returns.Current.Value;
                        returns.MoveNext();

                        // Add the monthly buy amount to the net worth at the specified frequency.
                        if (months % frequency == frequency - 1)
                        {
                            netWorth += frequency * monthlyBuy;
                        }
                    }
                }

                // Accumulate the total net worth and update the maximum net worth.
                total += netWorth;
                simulations++;

                max = Math.Max(netWorth, max);
            }
        }

        /*
        Simulation Loop:
            The method performs a simulation for each starting year and month within the scenario's range.
            For each starting point, it calculates the net worth for six different frequencies (1 to 6).
        Results Storage:
            For each frequency, the method stores the net worth in the results list.
            The difference between the net worth of frequency 1 (results[0]) and each other frequency is computed.
            These differences are used to update the bestResults and worstResults.
        Difference Calculation:
            worstResults[f-1] is updated with the maximum difference between the net worth of frequency 1 and frequency f.
            bestResults[f-1] is updated with the minimum difference between the net worth of frequency 1 and frequency f.
        */
        List<double> worstResults = Enumerable.Repeat(0.0, 6).ToList();
        List<double> bestResults = Enumerable.Repeat(0.0, 6).ToList();

        for (int currentYear = scenario.StartYear; currentYear <= scenario.EndYear - scenario.Years; currentYear++)
        {
            for (int currentMonth = 1; currentMonth <= 12; currentMonth++)
            {
                int endYear = currentYear + (currentMonth - 1 + numberOfMonths - 1) / 12;
                int endMonth = 1 + ((currentMonth - 1) + (numberOfMonths - 1) % 12) % 12;

                List<double> results = Enumerable.Repeat(0.0, 6).ToList();

                for (int freq = 1; freq <= 6; freq++)
                {
                    int months = 0;

                    DataVector dv = scenario.Values[0].GetDataVector(currentYear, currentMonth);
                    IEnumerator<Item> returns = dv.GetEnumerator();
                    returns.MoveNext();

                    double netWorth = 0;

                    for (int y = currentYear; y <= endYear; y++)
                    {
                        for (int m = (y == currentYear ? currentMonth : 1); m <= (y == endYear ? endMonth : 12); m++, months++)
                        {
                            // Adjust the portfolio with the returns
                            netWorth *= returns.Current.Value;
                            returns.MoveNext();

                            if (months % freq == freq - 1)
                            {
                                netWorth += freq * monthlyBuy;
                            }
                        }
                    }
                    // Store the net worth result for the current frequency
                    results[freq - 1] = netWorth;
                }
                // Update the worst and best results based on the difference between initial and current net worth.
                for (int f = 1; f <= 6; f++)
                {
                    worstResults[f - 1] = Math.Max(worstResults[f - 1], results[0] - results[f - 1]);
                    bestResults[f - 1] = Math.Min(bestResults[f - 1], results[0] - results[f - 1]);
                }
            }
        }

        Console.WriteLine($"Average: {total / simulations}, Max: {max}, Simulations: {simulations}");
        /* 
        Positive values in worstResults indicate that some frequencies resulted in higher net worth
        than the baseline, while negative values in bestResults indicate that some frequencies 
        resulted in lower net worth than the baseline.
        */
        for (int i = 0; i < 6; i++)
        {
            Console.WriteLine($"Best Results {i + 1}: {bestResults[i]}");
            Console.WriteLine($"Worst Results {i + 1}: {worstResults[i]}");
        }
    }
}