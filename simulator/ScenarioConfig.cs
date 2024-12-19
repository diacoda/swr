public class ScenarioConfig
{
    private const float _monthlyRebalancingCost = 0.005f;
    private const float _yearlyRebalancingCost = 0.01f;
    private const float _thresholdRebalancingCost = 0.01f;

    public float InitialValue { get; set; } = 10000.0f;
    public string Portfolio { get; set; } = String.Empty;
    public string Inflation { get; set; } = "no-inflation";
    public float Fees { get; set; } = 0.003f; // TER 0.3% = 0.003
    public int StartYear { get; set; }
    public int EndYear { get; set; }
    public int Years { get; set; }
    public float WithdrawalRate { get; set; } = 4.0f;
    public WithdrawalMethod WithdrawalMethod { get; set; } = WithdrawalMethod.STANDARD;
    public WithdrawalFrequency WithdrawalFrequency { get; set; } = WithdrawalFrequency.MONTHLY;
    public float MinimumWithdrawalRate { get; set; } = 3.0f; // Minimum of 3% * initial
    // success rate limit for finding safw withdrawal rate, the success rate must be bigger than this rate
    public float SuccessRateLimit { get; set; } = 95.0f;
    public Rebalancing Rebalance { get; set; } = Rebalancing.NONE;
    public float RebalancingThreshold { get; set; } = 0.01f;


    public float VanguardMaxIncrease { get; set; } = 0.05f;
    public float VanguardMaxDecrease { get; set; } = 0.02f;
    public float InitialCash { get; set; } = 0.0f;
    public bool CashSimple { get; set; } = false;

    // the percentage from the initial value that must remain after withdrawals
    // if the current value is below, then the simulation fails as it is not able to finish abive the percentage remaining threshold
    public float PercentageRemainingTarget { get; set; } = 0.01f;
    // adjust the initial value with inflation, such that at the end of the simulation, the value is more realistic 
    public bool AdjustRemainingWithInflation { get; set; } = true;

    public bool UseGlidepath { get; set; } = false;
    public float GlidepathAllocationChangeRate { get; set; } = 0.0f;
    public float GlidepathAllocationTarget { get; set; } = 0.0f;

    public bool UseSocialSecurity { get; set; } = false;
    public int SocialDelay { get; set; } = 0;
    public float SocialCoverage { get; set; } = 0.0f;

    public bool StrictValidation { get; set; } = true;
}