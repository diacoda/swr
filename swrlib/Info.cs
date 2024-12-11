namespace Swr;

public class Info
{
    public int SimulationYear { get; set; }
    public int SimulationMonth { get; set; }
    public int ContextYear { get; set; }
    public int ContextMonth { get; set; }
    public double ValueWithInflationAndExchangeRate { get; set; }
    public double ValueAfterMonthlyRebalance { get; set; }
    public double ValueAfterYearlylyRebalance { get; set; }
    public double ValuesAfterFees { get; set; }
    public double ValuesAfterWithdrawal { get; set; }
}