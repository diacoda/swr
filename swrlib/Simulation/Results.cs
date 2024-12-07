namespace Swr.Simulation;

using System.Collections.Generic;

public class Results
{
    // Fields
    public ulong Successes { get; set; } = 0;
    public ulong Failures { get; set; } = 0;
    public float SuccessRate { get; set; } = 0.0f;

    public double TvAverage { get; set; } = 0.0;
    public double TvMinimum { get; set; } = 0.0;
    public double TvMaximum { get; set; } = 0.0;
    public double TvMedian { get; set; } = 0.0;

    public double SpendingAverage { get; set; } = 0.0;
    public double SpendingMinimum { get; set; } = 0.0;
    public double SpendingMaximum { get; set; } = 0.0;
    public double SpendingMedian { get; set; } = 0.0;

    public int YearsLargeSpending { get; set; } = 0;
    public int YearsSmallSpending { get; set; } = 0;
    public int YearsVolatileUpSpending { get; set; } = 0;
    public int YearsVolatileDownSpending { get; set; } = 0;

    public int WorstDuration { get; set; } = 0;
    public int WorstStartingMonth { get; set; } = 0;
    public int WorstStartingYear { get; set; } = 0;

    public int LowestEffWrYear { get; set; } = 0;
    public int LowestEffWrStartYear { get; set; } = 0;
    public int LowestEffWrStartMonth { get; set; } = 0;
    public double LowestEffWr { get; set; } = 0.0;

    public int HighestEffWrYear { get; set; } = 0;
    public int HighestEffWrStartYear { get; set; } = 0;
    public int HighestEffWrStartMonth { get; set; } = 0;
    public double HighestEffWr { get; set; } = 0.0;

    public double WorstTv { get; set; } = 0;
    public int WorstTvMonth { get; set; } = 0;
    public int WorstTvYear { get; set; } = 0;
    public double BestTv { get; set; } = 0;
    public int BestTvMonth { get; set; } = 0;
    public int BestTvYear { get; set; } = 0;

    public double WithdrawnPerYear { get; set; } = 0.0;

    public string Message { get; set; } = string.Empty;
    public bool Error { get; set; } = false;

    public void Print(string frequency)
    {
        Console.WriteLine($"Success rate: {frequency}: ({this.Successes}/{this.Failures + this.Successes}) {this.SuccessRate} [{this.TvAverage}, {this.TvMedian}, {this.TvMinimum}, {this.TvMaximum}]");
        if (Failures > 0)
        {
            Console.WriteLine($"         Worst duration: {WorstDuration} months ({WorstStartingMonth}/{WorstStartingYear})");
        }
        else
        {
            Console.WriteLine($"         Worst duration: years of the scenario");
        }

        Console.WriteLine($"         Worst result: {WorstTv} ({WorstTvMonth}/{WorstTvYear})");
        Console.WriteLine($"          Best result: {BestTv} ({BestTvMonth}/{BestTvYear})");

        Console.WriteLine($"         Highest Eff. WR: {HighestEffWr}% ({HighestEffWrStartMonth}/{HighestEffWrStartYear} -> {HighestEffWrYear})");
        Console.WriteLine($"          Lowest Eff. WR: {LowestEffWr}% ({LowestEffWrStartMonth}/{LowestEffWrStartYear} -> {LowestEffWrYear})");
    }

    // Methods
    public void ComputeTerminalValues(List<double> terminalValues)
    {
        // Sort terminal values
        terminalValues.Sort();

        // Compute metrics
        TvMedian = terminalValues[terminalValues.Count / 2];
        TvMinimum = terminalValues.First();
        TvMaximum = terminalValues.Last();
        TvAverage = terminalValues.Sum() / terminalValues.Count;
    }

    public void ComputeSpending(List<List<double>> yearlySpending, int years)
    {
        var spending = new List<double>();

        // Process yearly spending
        foreach (var yearly in yearlySpending)
        {
            // Sum yearly spending
            spending.Add(yearly.Sum());

            for (int y = 1; y < yearly.Count; y++)
            {
                // Large spending
                if (yearly[y] >= 1.5f * yearly[0])
                {
                    YearsLargeSpending++;
                }

                // Small spending
                if (yearly[y] <= 0.5f * yearly[0])
                {
                    YearsSmallSpending++;
                }

                // Volatile spending up
                if (yearly[y] >= 1.1f * yearly[y - 1])
                {
                    YearsVolatileUpSpending++;
                }

                // Volatile spending down
                if (yearly[y] <= 0.9f * yearly[y - 1])
                {
                    YearsVolatileDownSpending++;
                }
            }
        }

        // Sort the spending values
        spending.Sort();

        // Compute metrics
        SpendingMedian = spending[spending.Count / 2] / years;
        SpendingMinimum = spending.First() / years;
        SpendingMaximum = spending.Last() / years;
        SpendingAverage = spending.Sum() / spending.Count / years;
    }

    public void RecordFailure(int months, int currentMonth, int currentYear)
    {
        if (WorstDuration == 0 || months < WorstDuration)
        {
            WorstDuration = months;
            WorstStartingMonth = currentMonth;
            WorstStartingYear = currentYear;
        }
    }
}
