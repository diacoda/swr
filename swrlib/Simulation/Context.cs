namespace Swr.Simulation;
public class Context
{
    public float FinalRemainingTarget { get; set; } = 0.0f;

    public float VanguardWithdrawal { get; set; } = 0.0f;
    public float LastYearWithdrawal { get; set; } = 0.0f;

    public float Cash { get; set; } = 0.0f;
    public float MinimumWithdrawal { get; set; } = 0.0f;

    public float YearStartValue { get; set; } = 0.0f;
    public float YearWithdrawn { get; set; } = 0.0f;
    public float LastWithdrawalAmount { get; set; } = 0.0f;

    public float Withdrawal { get; set; } = 0.0f;

    public int MonthIndex { get; set; } = 0;
    public int TotalMonths { get; set; } = 0;

    public bool End
    {
        get
        {
            return MonthIndex == TotalMonths;
        }
    }
}