namespace Swr.Simulation;
public class Context
{
    public double TargetValue { get; set; } = 0.0f;

    public double VanguardWithdrawal { get; set; } = 0.0;
    public double LastYearWithdrawal { get; set; } = 0.0;

    public double Cash { get; set; } = 0.0;
    public double MinimumWithdrawal { get; set; } = 0.0;

    public double YearStartValue { get; set; } = 0.0;
    public double YearWithdrawn { get; set; } = 0.0;
    public double LastWithdrawalAmount { get; set; } = 0.0;

    public double Withdrawal { get; set; } = 0.0f;

    public int CurrentMonth { get; set; } = 0;
    public int TotalMonths { get; set; } = 0;

    public bool End()
    {
        return CurrentMonth == TotalMonths;
    }
}