using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Swr;
using Swr.Data;
using Swr.Investment;
using Swr.Model;
namespace Swr.Simulation;

public class Scenario
{
    private readonly ILogger<Scenario> _logger;

    public Scenario(ILogger<Scenario> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Portfolio = new Portfolio("");
    }

    public void CopyFrom(SimulationRequest request)
    {
        TimeHorizon = request.TimeHorizon;
        StartYear = request.StartYear;
        EndYear = request.EndYear;
        Portfolio portfolio = new Portfolio(request.Portfolio);
        Portfolio = portfolio;
        Inflation = request.Inflation;
        Fees = request.Fees;
        WithdrawalMethod = request.WithdrawalMethod;
        WithdrawalFrequency = request.WithdrawalFrequency;
    }
    public void CopyFrom(Scenario s)
    {
        Portfolio = s.Portfolio;
        InflationData = s.InflationData;
        Values = s.Values;
        ExchangeSet = s.ExchangeSet;
        ExchangeRates = s.ExchangeRates;

        TimeHorizon = s.TimeHorizon;
        WithdrawalRate = s.WithdrawalRate;
        StartYear = s.StartYear;
        EndYear = s.EndYear;

        SuccessRateLimit = s.SuccessRateLimit;
        InitialInvestment = s.InitialInvestment;
        WithdrawFrequency = s.WithdrawFrequency;
        Rebalance = s.Rebalance;
        RebalancingThreshold = s.RebalancingThreshold;
        Fees = s.Fees;

        WithdrawalMethod = s.WithdrawalMethod;

        MinimumWithdrawalRate = s.MinimumWithdrawalRate;
        VanguardMaxIncreaseRate = s.VanguardMaxIncreaseRate;
        VanguardMaxDecreaseRate = s.VanguardMaxDecreaseRate;

        InitialCash = s.InitialCash;
        UseCashWithdrawal = s.UseCashWithdrawal;

        Inflation = s.Inflation;
        FinalTargetPercentage = s.FinalTargetPercentage;
        InflationAdjustedFinalTarget = s.InflationAdjustedFinalTarget;

        UseGlidepath = s.UseGlidepath;
        GlidepathAllocationChangeRate = s.GlidepathAllocationChangeRate;
        GlidepathAllocationTarget = s.GlidepathAllocationTarget;

        UseSocialSecurity = s.UseSocialSecurity;
        SocialDelay = s.SocialDelay;
        SocialCoverage = s.SocialCoverage;
    }

    private const float _monthlyRebalancingCost = 0.005f;
    private const float _yearlyRebalancingCost = 0.01f;
    private const float _thresholdRebalancingCost = 0.01f;

    public Investment.Portfolio Portfolio { get; set; }
    public DataVector InflationData { get; set; } = new DataVector("Inflation");
    public List<DataVector> Values { get; set; } = new List<DataVector>();
    public List<bool> ExchangeSet { get; set; } = new List<bool>();
    public List<DataVector> ExchangeRates { get; set; } = new List<DataVector>();

    public string Inflation { get; set; } = "no-inflation";
    public int TimeHorizon { get; set; }
    public float WithdrawalRate { get; set; } = 4.0f;
    public int StartYear { get; set; }
    public int EndYear { get; set; }
    // success rate limit for finding safw withdrawal rate, the success rate must be bigger than this rate
    public float SuccessRateLimit { get; set; } = 95.0f;
    public float InitialInvestment { get; set; } = 10000.0f;
    public float Fees { get; set; } = 0.003f; // TER 0.3% = 0.003
    public WithdrawalFrequency WithdrawalFrequency { get; set; } = WithdrawalFrequency.MONTHLY;
    public int WithdrawFrequency { get; set; } = 1;
    public Rebalancing Rebalance { get; set; } = Rebalancing.NONE;
    public float RebalancingThreshold { get; set; } = 0.01f;

    public WithdrawalMethod WithdrawalMethod { get; set; } = WithdrawalMethod.STANDARD;
    public float MinimumWithdrawalRate { get; set; } = 3.0f; // Minimum of 3% * initial
    public float VanguardMaxIncreaseRate { get; set; } = 5.0f;
    public float VanguardMaxDecreaseRate { get; set; } = 2.0f;
    public float InitialCash { get; set; } = 0.0f;
    public bool UseCashWithdrawal { get; set; } = false;

    // the percentage from the initial value that must remain after withdrawals
    // if the current value is below, then the simulation fails as it is not able to finish abive the percentage remaining threshold
    public float FinalTargetPercentage { get; set; } = 0.01f;
    // adjust the initial value with inflation, such that at the end of the simulation, the value is more realistic 
    public bool InflationAdjustedFinalTarget { get; set; } = true;

    public bool UseGlidepath { get; set; } = false;
    public float GlidepathAllocationChangeRate { get; set; } = 0.0f;
    public float GlidepathAllocationTarget { get; set; } = 0.0f;

    public bool UseSocialSecurity { get; set; } = false;
    public int SocialDelay { get; set; } = 0;
    public float SocialCoverage { get; set; } = 0.0f;

    public bool IsFailure(Context context, double currentValue)
    {
        // If it's not the end, we simply need to not run out of money
        if (!context.End)
        {
            return currentValue <= 0.0f;
        }

        // If it's the end, we need to respect the percentage that must remain
        if (InflationAdjustedFinalTarget)
        {
            // target value is adjusted with inflation
            return currentValue <= FinalTargetPercentage * context.FinalRemainingTarget;
        }
        else
        {
            return currentValue <= FinalTargetPercentage * InitialInvestment;
        }
    }

    private float Sum(List<float> currentValues)
    {
        float value = 0.0f;
        foreach (float currentValue in currentValues)
        {
            value += currentValue;
        }
        return value;
    }

    private bool PayFees(Context context, List<float> currentValues, int N)
    {
        if (currentValues.Count != N)
        {
            return false;
        }

        // Simulate TER
        if (Fees > 0.0f)
        {
            for (int i = 0; i < currentValues.Count; i++)
            {
                currentValues[i] *= 1.0f - (Fees / 12.0f);
            }

            // TER can cause failure
            if (IsFailure(context, Sum(currentValues)))
            {
                return false;
            }
        }
        return true;
    }

    private bool Glidepath(Context context, List<float> currentValues, int N)
    {
        // must be done for two only, yes for now
        if (UseGlidepath && Portfolio.Allocations.Count >= 2)
        {
            // Check if we have already reached the target: GlidepathAllocationTarget
            if (Portfolio.Allocations[0].AllocationCurrent == GlidepathAllocationTarget)
            {
                return true;
            }
            // GlidepathAllocationChangeRate
            Portfolio.Allocations[0].AllocationCurrent += GlidepathAllocationChangeRate;
            Portfolio.Allocations[1].AllocationCurrent -= GlidepathAllocationChangeRate;

            // Acount for float inaccuracies
            if (GlidepathAllocationChangeRate > 0.0f && Portfolio.Allocations[0].AllocationCurrent > GlidepathAllocationTarget)
            {
                Portfolio.Allocations[0].AllocationCurrent = GlidepathAllocationTarget;
                Portfolio.Allocations[1].AllocationCurrent = 100.0f - GlidepathAllocationTarget;
            }
            else if (GlidepathAllocationChangeRate < 0.0f && Portfolio.Allocations[0].AllocationCurrent < GlidepathAllocationTarget)
            {
                Portfolio.Allocations[0].AllocationCurrent = GlidepathAllocationTarget;
                Portfolio.Allocations[1].AllocationCurrent = 100.0f - GlidepathAllocationTarget;
            }

            // If rebalancing is not monthly, we do a rebalancing ourselves
            // Otherwise, it will be done as the next step
            if (Rebalance == Rebalancing.NONE)
            {
                // Pay the fees
                for (int i = 0; i < N; i++)
                {
                    currentValues[i] *= 1.0f - _monthlyRebalancingCost / 100.0f;
                }

                float totalValue = Sum(currentValues);

                // Fees can cause failure
                if (IsFailure(context, totalValue))
                {
                    return false;
                }

                for (int i = 0; i < N; i++)
                {
                    currentValues[i] = totalValue * (Portfolio.Allocations[i].AllocationCurrent / 100.0f);
                }
            }
        }
        return true;
    }

    private bool MonthlyRebalance(Context context, List<float> currentValues, int N)
    {
        // Nothing to rebalance if we have a single asset
        if (N == 1)
        {
            return true;
        }

        float totalValue = 0f;
        // Monthly Rebalance if necessary
        if (Rebalance == Rebalancing.MONTHLY)
        {
            // Pay the fees
            for (int i = 0; i < N; i++)
            {
                currentValues[i] *= 1.0f - _monthlyRebalancingCost / 100.0f;
            }

            totalValue = Sum(currentValues);

            // Fees can cause failure
            if (IsFailure(context, totalValue))
            {
                return false;
            }

            for (int i = 0; i < N; i++)
            {
                currentValues[i] = totalValue * (Portfolio.Allocations[i].AllocationCurrent / 100.0f);
            }
        }

        // Threshold Rebalance if necessary
        if (Rebalance == Rebalancing.THRESHOLD)
        {
            bool rebalance = false;
            totalValue = Sum(currentValues);
            for (int i = 0; i < N; i++)
            {
                if (Math.Abs((Portfolio.Allocations[i].AllocationCurrent / 100.0f) - currentValues[i] / totalValue) >= RebalancingThreshold)
                {
                    rebalance = true;
                    break;
                }
            }

            if (rebalance)
            {
                // Pay the fees
                for (int i = 0; i < N; i++)
                {
                    currentValues[i] *= 1.0f - _thresholdRebalancingCost / 100.0f;
                }

                // We need to recompute the total value after the fees
                totalValue = Sum(currentValues);

                // Fees can cause failure
                if (IsFailure(context, totalValue))
                {
                    return false;
                }

                for (int i = 0; i < N; i++)
                {
                    currentValues[i] = totalValue * (Portfolio.Allocations[i].AllocationCurrent / 100.0f);
                }
            }
        }
        return true;
    }

    private bool YearlyRebalance(Context context, List<float> currentValues, int N)
    {
        // Nothing to rebalance if we have a single asset
        if (N == 1)
        {
            return true;
        }

        // Yearly Rebalance if necessary
        if (Rebalance == Rebalancing.YEARLY)
        {
            // Pay the fees
            for (int i = 0; i < N; i++)
            {
                currentValues[i] *= 1.0f - _yearlyRebalancingCost / 100.0f;
            }

            float totalValue = Sum(currentValues);

            // Fees can cause failure
            if (IsFailure(context, totalValue))
            {
                return false;
            }

            for (int i = 0; i < N; i++)
            {
                currentValues[i] = totalValue * (Portfolio.Allocations[i].AllocationCurrent / 100.0f);
            }
        }
        return true;
    }

    /// <summary>
    /// Executes the withdrawal process for a given context and portfolio values. 
    /// Adjusts portfolio values based on the withdrawal strategy and returns a 
    /// boolean indicating success or failure of the withdrawal process.
    /// </summary>
    /// <param name="context">The context containing withdrawal parameters and state.</param>
    /// <param name="currentValues">A list of current portfolio values for each asset class.</param>
    /// <param name="N">The number of asset classes in the portfolio.</param>
    /// <returns>
    /// True if the withdrawal was successful, or the portfolio remains viable; 
    /// False if the withdrawal leads to failure (e.g., depleting resources).
    /// </returns>
    private bool Withdraw(Context context, List<float> currentValues, int N)
    {
        // Perform withdrawals only at specified intervals based on WithdrawFrequency.
        if ((context.MonthIndex - 1) % WithdrawFrequency == 0)
        {
            // Calculate the total portfolio value.
            float totalValue = Sum(currentValues);

            // Determine the number of withdrawal periods remaining.
            int periods = WithdrawFrequency;
            if ((context.MonthIndex - 1) + WithdrawFrequency > context.TotalMonths)
            {
                periods = context.TotalMonths - (context.MonthIndex - 1);
            }

            float withdrawalAmount = 0f;

            // Compute the withdrawal amount based on the withdrawal strategy
            if (WithdrawalMethod == WithdrawalMethod.STANDARD)
            {
                // Fixed annual withdrawal rate divided into periodic withdrawals.
                withdrawalAmount = context.Withdrawal / (12.0f / periods);
            }
            else if (WithdrawalMethod == WithdrawalMethod.CURRENT)
            {
                // Percentage-based withdrawal tied to the current portfolio value.
                withdrawalAmount = (totalValue * (WithdrawalRate / 100.0f)) / (12.0f / periods);
                // Ensure the withdrawal does not fall below the specified minimum.
                float minimumWithdrawal = context.MinimumWithdrawal / (12.0f / periods);

                if (withdrawalAmount < minimumWithdrawal)
                {
                    withdrawalAmount = minimumWithdrawal;
                }
            }
            else if (WithdrawalMethod == WithdrawalMethod.VANGUARD)
            {
                // Vanguard's dynamic withdrawal strategy, adjusting year-over-year.
                if (context.MonthIndex == 1)
                {
                    // Fisrt year: initialize Vanguard withdrawal
                    context.VanguardWithdrawal = totalValue * (WithdrawalRate / 100.0f);
                    context.LastYearWithdrawal = context.VanguardWithdrawal;
                }
                else if ((context.MonthIndex - 1) % 12 == 0)
                {
                    // Update withdrawals annually.
                    context.LastYearWithdrawal = context.VanguardWithdrawal;
                    context.VanguardWithdrawal = totalValue * (WithdrawalRate / 100.0f);

                    // Cap increases and decreases based on specified limits.
                    if (context.VanguardWithdrawal > (1.0f + VanguardMaxIncreaseRate / 100.0f) * context.LastYearWithdrawal)
                    {
                        context.VanguardWithdrawal = (1.0f + VanguardMaxIncreaseRate / 100.0f) * context.LastYearWithdrawal;
                    }
                    else if (context.VanguardWithdrawal < (1.0f - VanguardMaxDecreaseRate / 100.0f) * context.LastYearWithdrawal)
                    {
                        context.VanguardWithdrawal = (1.0f - VanguardMaxDecreaseRate / 100.0f) * context.LastYearWithdrawal;
                    }
                }
                // Adjust withdrawal to a periodic base
                withdrawalAmount = context.VanguardWithdrawal / (12.0f / periods);

                // Ensure a minimum withdrawal amount is maintained.
                float minimumWithdrawal = context.MinimumWithdrawal / (12.0f / periods);
                if (withdrawalAmount < minimumWithdrawal)
                {
                    withdrawalAmount = minimumWithdrawal;
                }
            }

            // Adjust withdrawal based on social security coverage if applicable.
            if (UseSocialSecurity)
            {
                if ((context.MonthIndex / 12.0f) >= SocialDelay)
                {
                    withdrawalAmount -= (SocialCoverage * withdrawalAmount);
                }
            }
            context.LastWithdrawalAmount = withdrawalAmount;

            // If no withdrawal amount is required, exit early as successful.
            if (withdrawalAmount <= 0.0f)
            {
                return true;
            }
            // Calculate the effective withdrawal rate.
            float effectiveWithdrawalRate = withdrawalAmount / context.YearStartValue;

            // Strategies with cash or effective withdrawal rate is greater than the monthly WithdrawalRate
            // withdrawing from cash if the effective rate exceeds the target monthly rate.
            if (UseCashWithdrawal || ((effectiveWithdrawalRate * 100.0f) >= (WithdrawalRate / 12.0f)))
            {
                // First, withdraw from cash if possible
                if (context.Cash > 0.0f)
                {
                    if (withdrawalAmount <= context.Cash)
                    {
                        context.YearWithdrawn += withdrawalAmount;
                        context.Cash -= withdrawalAmount;
                        withdrawalAmount = 0.0f;
                    }
                    else
                    {
                        context.YearWithdrawn += context.Cash;
                        withdrawalAmount -= context.Cash;
                        context.Cash = 0.0f;
                    }
                }
            }
            // Adjust each portfolio value proportionally to account for the withdrawal.
            for (int i = 0; i < currentValues.Count; i++)
            {
                float proportion = currentValues[i] / totalValue;
                float withdrawal = proportion * withdrawalAmount;
                currentValues[i] = Math.Max(0.0f, currentValues[i] - withdrawal);
            }
            // Check for portfolio failure after the withdrawal
            if (IsFailure(context, Sum(currentValues)))
            {
                context.YearWithdrawn += totalValue;
                return false;
            }
            // Update the total withdrawn for the year
            context.YearWithdrawn += withdrawalAmount;

        }
        return true;
    }

    public bool ValidYear(DataVector dataVector, int year)
    {
        // Check if any item in the Data list of the DataVector has the specified year
        return dataVector.Data.Any(item => item.Year == year);
    }

    private Results Validate()
    {
        var res = new Results();

        int N = Portfolio.Allocations.Count();

        if (ExchangeSet.Count() == 0 || ExchangeRates.Count() == 0)
        {
            res.Message = "Invalid scenario (no exchange rates)";
            res.Error = true;
            return res;
        }

        if (StartYear >= EndYear)
        {
            res.Message = "The end year must be higher than the start year";
            res.Error = true;
            return res;
        }

        if (TimeHorizon <= 0)
        {
            res.Message = "The number of years must be at least 1";
            res.Error = true;
            return res;
        }

        // Validation and adjustment logic (adapt start and end years)...
        res = ValidateAndAdaptYears(N);
        if (res.Error) return res;

        // 2. Make sure the simulation makes sense
        if (N == 0)
        {
            res.Message = "Cannot work with an empty portfolio";
            res.Error = true;
            return res;
        }

        if (UseSocialSecurity)
        {
            if (InitialCash > 0.0f)
            {
                res.Message = "Social security and cash is not implemented";
                res.Error = true;
                return res;
            }

            if (WithdrawalMethod != WithdrawalMethod.STANDARD)
            {
                res.Message = "Social security is only implemented for standard withdrawal method";
                res.Error = true;
                return res;
            }
        }

        if (WithdrawalMethod == WithdrawalMethod.VANGUARD && WithdrawFrequency != 1)
        {
            res.Message = "Vanguard dynamic withdrawals is only implemented with monthly withdrawals";
            res.Error = true;
            return res;
        }

        if (EndYear - StartYear < TimeHorizon)
        {
            res.Message = $"The period is too short for a {TimeHorizon} TimeHorizon simulation. The number of TimeHorizon has been reduced to {EndYear - StartYear}";
            TimeHorizon = EndYear - StartYear;
        }

        if (UseGlidepath)
        {
            if (Portfolio.Allocations[0].Asset != "us_stocks")
            {
                res.Message = "The first assert must be us_stocks for glidepath";
                res.Error = true;
                return res;
            }

            if (Rebalance != Rebalancing.NONE && Rebalance != Rebalancing.MONTHLY)
            {
                res.Message = "Invalid rebalancing method for glidepath";
                res.Error = true;
                return res;
            }

            if (GlidepathAllocationChangeRate == 0.0f)
            {
                res.Message = $"Invalid pass ({GlidepathAllocationChangeRate}) for glidepath";
                res.Error = true;
                return res;
            }

            if (GlidepathAllocationChangeRate > 0.0f && GlidepathAllocationTarget <= Portfolio.Allocations[0].AllocationValue)
            {
                //std::cout << scenario.GlidepathAllocationChangeRate << std::endl;
                //std::cout << scenario.GlidepathAllocationTarget << std::endl;
                //std::cout << portfolio[0].allocation << std::endl;
                res.Message = "Invalid goal/pass (1) for glidepath";
                res.Error = true;
                return res;
            }

            if (GlidepathAllocationChangeRate < 0.0f && GlidepathAllocationTarget >= Portfolio.Allocations[0].AllocationValue)
            {
                //std::cout << scenario.GlidepathAllocationChangeRate << std::endl;
                //std::cout << scenario.GlidepathAllocationTarget << std::endl;
                //std::cout << portfolio[0].allocation << std::endl;
                res.Message = "Invalid goal/pass (2) for glidepath";
                res.Error = true;
                return res;
            }
        }
        // More validation of data (should not happen but would fail silently otherwise)
        bool valid = true;

        for (int i = 0; i < N; i++)
        {
            valid &= Values[i].IsStartValid(StartYear, 1);
            valid &= ExchangeRates[i].IsStartValid(StartYear, 1);
        }

        valid &= InflationData.IsStartValid(StartYear, 1);

        if (!valid)
        {
            res.Message = "Invalid data points (internal bug, contact the developer)";
            res.Error = true;
            return res;
        }

        return res;
    }

    public Results Simulate()
    {
        int N = Portfolio.Allocations.Count();

        var res = new Results();
        Stopwatch stopwatch = Stopwatch.StartNew();

        res = Validate();
        if (res.Error)
        {
            Console.WriteLine($"Invalid: {res.Message} ");
            return res;
        }

        // the final value after the simulation per year
        List<float> terminalValues = new List<float>();
        // total withdrawals per year, but not for failed ones
        List<List<float>> yearlyWithdrawals = new List<List<float>>();

        // 3. Do the actual simulation
        for (int currentYear = StartYear; currentYear <= EndYear - TimeHorizon; currentYear++)
        {
            for (int currentMonth = 1; currentMonth <= 12; currentMonth++)
            {
                Info info = new();
                info.SimulationYear = currentYear;
                info.SimulationMonth = currentMonth;

                float totalWithdrawnPerYear = 0.0f;
                bool failure = false;

                // Reset the allocation for the context
                foreach (var asset in Portfolio.Allocations)
                {
                    asset.AllocationCurrent = asset.AllocationValue;
                }

                // Get the data vector for the current year and month.
                DataVector dv = InflationData.GetDataVector(currentYear, currentMonth);
                IEnumerator<Item> inflationVector = dv.GetEnumerator();
                inflationVector.MoveNext();

                List<float> currentValues = new List<float>(new float[N]);
                List<IEnumerator<Item>> returns = new List<IEnumerator<Item>>(N);
                List<IEnumerator<Item>> exchangeRates = new List<IEnumerator<Item>>(N);
                for (int i = 0; i < N; i++)
                {
                    returns.Add(new List<Item>().GetEnumerator());
                    returns[i] = Values[i].GetDataVector(currentYear, currentMonth).GetEnumerator();
                    returns[i].MoveNext();

                    exchangeRates.Add(new List<Item>().GetEnumerator());
                    exchangeRates[i] = ExchangeRates[i].GetDataVector(currentYear, currentMonth).GetEnumerator();
                    exchangeRates[i].MoveNext();

                    currentValues[i] = InitialInvestment * (Portfolio.Allocations[i].AllocationCurrent / 100.0f);
                }

                // Add an empty list to the list of lists
                yearlyWithdrawals.Add(new List<float>());

                Context context = new Context();
                context.MonthIndex = 1;
                context.TotalMonths = TimeHorizon * 12;
                // The amount of money withdrawn per year (STANDARD method)
                context.Withdrawal = InitialInvestment * (WithdrawalRate / 100.0f);
                // The minimum amount of money withdraw (CURRENT method)
                context.MinimumWithdrawal = InitialInvestment * (MinimumWithdrawalRate / 100.0f);
                // The amount of cash available
                context.Cash = InitialCash;
                // Used for the target threshold
                context.FinalRemainingTarget = InitialInvestment;

                int endYear = currentYear + (currentMonth - 1 + context.TotalMonths - 1) / 12;
                int endMonth = 1 + (currentMonth - 1 + (context.TotalMonths - 1) % 12) % 12;

                WithdrawalStrategy withdrawalStrategy = new WithdrawalStrategy(InitialInvestment);
                withdrawalStrategy.MinimumWithdrawalRate = MinimumWithdrawalRate;
                withdrawalStrategy.WithdrawalRate = WithdrawalRate;
                withdrawalStrategy.WithdrawalFrequency = WithdrawalFrequency;
                withdrawalStrategy.UseCashWithdrawal = false;
                withdrawalStrategy.VanguardMaxDecreaseRate = VanguardMaxDecreaseRate;
                withdrawalStrategy.VanguardMaxIncreaseRate = VanguardMaxIncreaseRate;

                for (int y = currentYear; y <= endYear; y++)
                {
                    context.YearStartValue = currentValues.Sum();
                    context.YearWithdrawn = 0.0f;

                    for (int m = y == currentYear ? currentMonth : 1; !failure && m <= (y == endYear ? endMonth : 12); m++, context.MonthIndex++)
                    {
                        info.ContextMonth = m;
                        info.ContextYear = y;

                        // Adjust the portfolio with returns and exchanges
                        for (int i = 0; i < N; i++)
                        {
                            Debug.Assert(y == returns[i].Current.Year && m == returns[i].Current.Month, $"Returns no match year:{y} month:{m} within current year: {currentYear} and current month: {currentMonth}");
                            currentValues[i] *= returns[i].Current.Value;
                            returns[i].MoveNext();

                            Debug.Assert(y == exchangeRates[i].Current.Year && m == exchangeRates[i].Current.Month, $"Exchange rates no match year:{y} month:{m} within current year: {currentYear} and current month: {currentMonth}");
                            currentValues[i] *= exchangeRates[i].Current.Value;
                            exchangeRates[i].MoveNext();
                        }

                        info.ValueWithInflationAndExchangeRate = Sum(currentValues);

                        // Handle failure scenarios
                        step(() => !IsFailure(context, currentValues.Sum()), res, currentYear, currentMonth, ref failure, context);
                        step(() => Glidepath(context, currentValues, N), res, currentYear, currentMonth, ref failure, context);
                        step(() => MonthlyRebalance(context, currentValues, N), res, currentYear, currentMonth, ref failure, context);
                        info.ValueAfterMonthlyRebalance = Sum(currentValues);

                        step(() => PayFees(context, currentValues, N), res, currentYear, currentMonth, ref failure, context);
                        info.ValuesAfterFees = Sum(currentValues);

                        Debug.Assert(y == inflationVector.Current.Year && m == inflationVector.Current.Month, $"Inflation no match year:{y} month:{m} within current year: {currentYear} and current month: {currentMonth}");
                        float inflation = inflationVector.Current.Value;
                        inflationVector.MoveNext();

                        // Adjust withdrawals for inflation
                        context.Withdrawal *= inflation;
                        context.MinimumWithdrawal *= inflation;
                        context.FinalRemainingTarget *= inflation;

                        float w = withdrawalStrategy.CalculateWithdrawalAmount(m, TimeHorizon * 12, currentValues.Sum(), inflation);
                        //Console.WriteLine($"Withdrawal {w} year:{y} month:{m} within current year: {currentYear} and current month: {currentMonth}");
                        // Perform withdrawals
                        step(() => Withdraw(context, currentValues, N), res, currentYear, currentMonth, ref failure, context);
                        //Console.WriteLine($"Out {context.LastWithdrawalAmount} year:{y} month:{m} within current year: {currentYear} and current month: {currentMonth}");
                        info.ValuesAfterWithdrawal = Sum(currentValues);
                        // Record withdrawal
                        if ((context.MonthIndex - 1) % 12 == 0)
                        {
                            yearlyWithdrawals.Last().Add(context.LastWithdrawalAmount);
                        }
                        else
                        {
                            yearlyWithdrawals.Last()[yearlyWithdrawals.Last().Count - 1] += context.LastWithdrawalAmount;
                        }
                        //_logger.LogInformation("{@Info}", info);
                    }

                    // logic for currentYear
                    totalWithdrawnPerYear += context.YearWithdrawn;
                    step(() => YearlyRebalance(context, currentValues, N), res, currentYear, currentMonth, ref failure, context);
                    info.ValueAfterYearlylyRebalance = Sum(currentValues);

                    if (failure)
                    {
                        float effectiveWithdrawalRate = context.YearWithdrawn / context.YearStartValue;

                        if (res.LowestEffWrYear == 0 || effectiveWithdrawalRate < res.LowestEffWr)
                        {
                            res.LowestEffWrStartYear = currentYear;
                            res.LowestEffWrStartMonth = currentMonth;
                            res.LowestEffWrYear = y;
                            res.LowestEffWr = effectiveWithdrawalRate;
                        }

                        if (res.HighestEffWrYear == 0 || effectiveWithdrawalRate > res.HighestEffWr)
                        {
                            res.HighestEffWrStartYear = currentYear;
                            res.HighestEffWrStartMonth = currentMonth;
                            res.HighestEffWrYear = y;
                            res.HighestEffWr = effectiveWithdrawalRate;
                        }
                        break;
                    }
                }

                Info2 info2 = new();
                info2.Year = currentYear;
                info2.Month = currentMonth;
                info2.LastYear = info.ContextYear;
                info2.LastMonth = info.ContextMonth;
                info2.Value = Sum(currentValues);
                info2.IsSuccess = !failure;

                //_logger.LogInformation("{@Info2}", info2);

                float finalValue = failure ? 0.0f : Sum(currentValues);
                terminalValues.Add(finalValue);

                if (!failure)
                {
                    res.Successes++;
                    res.AverageWithdrawnPerYear += totalWithdrawnPerYear;
                }
                else
                {
                    res.Failures++;
                }

                if (failure)
                {
                    yearlyWithdrawals.RemoveAt(yearlyWithdrawals.Count - 1);
                }

                // Record periods
                if (res.BestTvYear == 0)
                {
                    res.BestTvYear = currentYear;
                    res.BestTvMonth = currentMonth;
                    res.BestTv = finalValue;
                }
                if (res.WorstTvYear == 0)
                {
                    res.WorstTvYear = currentYear;
                    res.WorstTvMonth = currentMonth;
                    res.WorstTv = finalValue;
                }
                if (finalValue < res.WorstTv)
                {
                    res.WorstTvYear = currentYear;
                    res.WorstTvMonth = currentMonth;
                    res.WorstTv = finalValue;
                }
                if (finalValue > res.BestTv)
                {
                    res.BestTvYear = currentYear;
                    res.BestTvMonth = currentMonth;
                    res.BestTv = finalValue;
                }
            }
        }
        // Final metrics
        res.AverageWithdrawnPerYear = (res.AverageWithdrawnPerYear / TimeHorizon) / res.Successes;
        res.SuccessRate = 100.0f * (res.Successes / (float)(res.Successes + res.Failures));
        res.ComputeTerminalValues(terminalValues);
        res.ComputeWithdrawals(yearlyWithdrawals, TimeHorizon);
        _logger.LogInformation("{@Results}", res);

        stopwatch.Stop();
        TimeSpan elapsed = stopwatch.Elapsed;

        return res;
    }

    private void step(Func<bool> result, Results? res, int currentYear, int currentMonth, ref bool failure, Context context)
    {
        if (!failure && !result())
        {
            failure = true;
            if (res == null)
            {
                throw new NullReferenceException("res can't be null");
            }
            res.RecordFailure(context.MonthIndex, currentMonth, currentYear);
        }
    }

    private Results ValidateAndAdaptYears(int N)
    {
        Results results = new Results();

        bool changed = false;

        if (!ValidYear(InflationData, StartYear) && !ValidYear(InflationData, EndYear))
        {
            results.Message = "The given period is out of the historical data, it's either too far in the future or too far in the past";
            results.Error = true;
            return results;
        }

        foreach (var v in Values)
        {
            if (!ValidYear(v, StartYear) && !ValidYear(v, EndYear))
            {
                results.Message = "The given period is out of the historical data, it's either too far in the future or too far in the past";
                results.Error = true;
                return results;
            }
        }

        int inflationFirstYear = InflationData.First().Year;
        if (InflationData.First().Year > StartYear)
        {
            StartYear = InflationData.First().Year;
            changed = true;
        }

        if (InflationData.Last().Year < EndYear)
        {
            EndYear = InflationData.Last().Year;
            changed = true;
        }

        foreach (var value in Values)
        {
            if (value.First().Year > StartYear)
            {
                StartYear = value.First().Year;
                changed = true;
            }

            if (value.Last().Year < EndYear)
            {
                EndYear = value.Last().Year;
                changed = true;
            }
        }

        for (int i = 0; i < N; i++)
        {
            if (ExchangeSet[i])
            {
                DataVector v = ExchangeRates[i];

                if (v.Front().Year > StartYear)
                {
                    StartYear = v.Front().Year;
                    changed = true;
                }

                if (v.Back().Year < EndYear)
                {
                    EndYear = v.Back().Year;
                    changed = true;
                }
            }
        }

        if (changed)
        {
            if (EndYear <= StartYear)
            {

                results.Message = "The period is invalid with this duration.";
                results.Error = true;
            }
            else
            {
                results.Message = $"The period has been changed to {StartYear}:{EndYear} based on the available data.";
            }
        }
        return results;
    }

    public bool PrepareExchangeRates(string currency)
    {
        DataVector exchangeData = DataLoader.LoadExchange("usd_cad");
        DataVector exchangeDataInv = DataLoader.LoadExchangeInv("usd_cad");

        if (exchangeData.Count() == 0 || exchangeDataInv.Count() == 0)
        {
            return false;
        }

        int N = Portfolio.Allocations.Count();
        ExchangeRates = new List<DataVector>(N);
        ExchangeSet = new List<bool>(new bool[N]);

        for (int i = 0; i < N; i++)
        {
            var asset = Portfolio.Allocations[i].Asset;

            if (currency == "usd")
            {
                if (asset == "ca_stocks" || asset == "ca_bonds")
                {
                    ExchangeSet[i] = true;
                    ExchangeRates.Add(exchangeDataInv);
                }
                else
                {
                    ExchangeSet[i] = false;
                    DataVector valuesCopy = new DataVector("exchange rates");
                    foreach (var rate in Values[i])
                    {
                        valuesCopy.Data.Add(new Item(rate.Month, rate.Year, 1));
                    }
                    ExchangeRates.Add(valuesCopy);
                }
            }
            else if (currency == "cad")
            {
                if (asset == "ca_stocks" || asset == "ca_bonds")
                {
                    ExchangeSet[i] = false;
                    DataVector valuesCopy = new DataVector("exchange rates");
                    foreach (var rate in Values[i])
                    {
                        valuesCopy.Data.Add(new Item(rate.Month, rate.Year, 1));
                    }
                    ExchangeRates.Add(valuesCopy);
                }
                else
                {
                    ExchangeSet[i] = true;
                    ExchangeRates.Add(exchangeData);
                }
            }
        }
        return true;
    }
}