namespace Swr.Simulation;

using System.Collections.Generic;

public class Results
{
    // Fields
    public ulong Successes { get; set; } = 0;
    public ulong Failures { get; set; } = 0;
    public float SuccessRate { get; set; } = 0.0f;

    public double TerminalValueAverage { get; set; } = 0.0;
    public double TerminalValueMinimum { get; set; } = 0.0;
    public double TerminalValueMaximum { get; set; } = 0.0;
    public double TerminalValueMedian { get; set; } = 0.0;

    public float WithdrawalRate { get; set; } = 0.0f;
    public WithdrawalMethod WithdrawalMethod { get; set; } = WithdrawalMethod.STANDARD;
    public double WithdrawalAverage { get; set; } = 0.0;
    public double WithdrawalMinimum { get; set; } = 0.0;
    public double WithdrawalMaximum { get; set; } = 0.0;
    public double WithdrawalMedian { get; set; } = 0.0;

    public int YearsLargeWithdrawal { get; set; } = 0;
    public int YearsSmallWithdrawal { get; set; } = 0;
    public int YearsVolatileUpWithdrawal { get; set; } = 0;
    public int YearsVolatileDownWithdrawal { get; set; } = 0;

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

    public double AverageWithdrawnPerYear { get; set; } = 0.0;

    public string Message { get; set; } = string.Empty;
    public bool Error { get; set; } = false;

    public void Print(string frequency)
    {
        Console.WriteLine($"Success rate: {frequency}: ({this.Successes}/{this.Failures + this.Successes}) {this.SuccessRate}");
        Console.WriteLine($"Terminal Value: [avg:{this.TerminalValueAverage}, med:{this.TerminalValueMedian}, min:{this.TerminalValueMinimum}, max:{this.TerminalValueMaximum}]");
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
        TerminalValueMedian = terminalValues[terminalValues.Count / 2];
        TerminalValueMinimum = terminalValues.First();
        TerminalValueMaximum = terminalValues.Last();
        TerminalValueAverage = terminalValues.Sum() / terminalValues.Count;
    }

    public void ComputeWithdrawals(List<List<double>> yearlyWithdrawals, int years)
    {
        var withdrawals = new List<double>();

        // Process yearly withdrawals
        foreach (var yearly in yearlyWithdrawals)
        {
            // Sum yearly withdrawals
            withdrawals.Add(yearly.Sum());

            for (int y = 1; y < yearly.Count; y++)
            {
                // Large withdrawals
                if (yearly[y] >= 1.5f * yearly[0])
                {
                    YearsLargeWithdrawal++;
                }

                // Small withdrawal
                if (yearly[y] <= 0.5f * yearly[0])
                {
                    YearsSmallWithdrawal++;
                }

                // Volatile withdrawal up
                if (yearly[y] >= 1.1f * yearly[y - 1])
                {
                    YearsVolatileUpWithdrawal++;
                }

                // Volatile withdrawal down
                if (yearly[y] <= 0.9f * yearly[y - 1])
                {
                    YearsVolatileDownWithdrawal++;
                }
            }
        }

        // Sort the spending values
        withdrawals.Sort();

        // Compute metrics
        WithdrawalMedian = withdrawals[withdrawals.Count / 2] / years;
        WithdrawalMinimum = withdrawals.First() / years;
        WithdrawalMaximum = withdrawals.Last() / years;
        WithdrawalAverage = withdrawals.Sum() / withdrawals.Count / years;
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
