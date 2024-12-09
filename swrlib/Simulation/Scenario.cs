using System.Diagnostics;
using Swr.Finance;
using Swr.Investment;
namespace Swr.Simulation;

public class Scenario
{
    public Scenario(Scenario s)
    {
        Portfolio = s.Portfolio;
        InflationData = s.InflationData;
        Values = s.Values;
        ExchangeSet = s.ExchangeSet;
        ExchangeRates = s.ExchangeRates;

        Years = s.Years;
        WithdrawalRate = s.WithdrawalRate;
        StartYear = s.StartYear;
        EndYear = s.EndYear;

        SuccessRateLimit = s.SuccessRateLimit;
        InitialValue = s.InitialValue;
        WithdrawFrequency = s.WithdrawFrequency;
        Rebalance = s.Rebalance;
        Threshold = s.Threshold;
        Fees = s.Fees;

        WithdrawalMethod = s.WithdrawalMethod;

        MinimumWithdrawalPercent = s.MinimumWithdrawalPercent;
        VanguardMaxIncrease = s.VanguardMaxIncrease;
        VanguardMaxDecrease = s.VanguardMaxDecrease;

        TimeoutMsecs = s.TimeoutMsecs;

        InitialCash = s.InitialCash;
        CashSimple = s.CashSimple;

        Inflation = s.Inflation;
        FinalThreshold = s.FinalThreshold;
        FinalInflation = s.FinalInflation;

        UseGlidepath = s.UseGlidepath;
        GP_Pass = s.GP_Pass;
        GP_Goal = s.GP_Goal;

        UseSocialSecurity = s.UseSocialSecurity;
        SocialDelay = s.SocialDelay;
        SocialCoverage = s.SocialCoverage;

        StrictValidation = s.StrictValidation;
    }

    public Scenario()
    {

    }

    private const float MonthlyRebalancingCost = 0.005f;
    private const float YearlyRebalancingCost = 0.01f;
    private const float ThresholdRebalancingCost = 0.01f;

    public Investment.Portfolio Portfolio { get; set; }
    public DataVector InflationData { get; set; } = new DataVector("Inflation");
    public List<DataVector> Values { get; set; } = new List<DataVector>();
    public List<bool> ExchangeSet { get; set; } = new List<bool>();
    public List<DataVector> ExchangeRates { get; set; } = new List<DataVector>();

    public int Years { get; set; }
    public float WithdrawalRate { get; set; } = 0.04f;
    public int StartYear { get; set; }
    public int EndYear { get; set; }
    // success rate limit for finding safw withdrawal rate, the success rate must be bigger than this rate
    public float SuccessRateLimit { get; set; } = 95.0f;
    public float InitialValue { get; set; } = 1000.0f;
    public int WithdrawFrequency { get; set; } = 1;
    public Rebalancing Rebalance { get; set; } = Rebalancing.NONE;
    public float Threshold { get; set; } = 0.01f;
    public float Fees { get; set; } = 0.003f; // TER 0.3% = 0.003
    public WithdrawalMethod WithdrawalMethod { get; set; } = WithdrawalMethod.STANDARD;
    public float MinimumWithdrawalPercent { get; set; } = 0.03f; // Minimum of 3% * initial

    public float VanguardMaxIncrease { get; set; } = 0.05f;
    public float VanguardMaxDecrease { get; set; } = 0.02f;

    public int TimeoutMsecs { get; set; } = 0;

    public float InitialCash { get; set; } = 0.0f;
    public bool CashSimple { get; set; } = true;

    public string Inflation { get; set; } = "no-inflation";
    public float FinalThreshold { get; set; } = 0.01f;
    public bool FinalInflation { get; set; } = true;

    public bool UseGlidepath { get; set; } = false;
    public float GP_Pass { get; set; } = 0.0f;
    public float GP_Goal { get; set; } = 0.0f;

    public bool UseSocialSecurity { get; set; } = false;
    public int SocialDelay { get; set; } = 0;
    public float SocialCoverage { get; set; } = 0.0f;

    public bool StrictValidation { get; set; } = true;

    public bool IsFailure(Context context, double currentValue)
    {
        // If it's not the end, we simply need to not run out of money
        if (!context.End())
        {
            return currentValue <= 0.0f;
        }

        // If it's the end, we need to respect the threshold
        if (FinalInflation)
        {
            return currentValue <= FinalThreshold * context.TargetValue;
        }
        else
        {
            return currentValue <= FinalThreshold * InitialValue;
        }
    }

    private double Sum(List<double> currentValues)
    {
        double value = 0.0;
        foreach (float currentValue in currentValues)
        {
            value += currentValue;
        }
        return value;
    }

    private bool PayFees(Context context, List<double> currentValues, int N)
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

    private bool Glidepath(Context context, List<double> currentValues, int N)
    {
        if (UseGlidepath)
        {
            // Check if we have already reached the target
            if (Portfolio.Allocations[0].AllocationCurrent == GP_Goal)
            {
                return true;
            }

            Portfolio.Allocations[0].AllocationCurrent += GP_Pass;
            Portfolio.Allocations[1].AllocationCurrent -= GP_Pass;

            // Acount for float inaccuracies
            if (GP_Pass > 0.0f && Portfolio.Allocations[0].AllocationCurrent > GP_Goal)
            {
                Portfolio.Allocations[0].AllocationCurrent = GP_Goal;
                Portfolio.Allocations[1].AllocationCurrent = 100.0f - GP_Goal;
            }
            else if (GP_Pass < 0.0f && Portfolio.Allocations[0].AllocationCurrent < GP_Goal)
            {
                Portfolio.Allocations[0].AllocationCurrent = GP_Goal;
                Portfolio.Allocations[1].AllocationCurrent = 100.0f - GP_Goal;
            }

            // If rebalancing is not monthly, we do a rebalancing ourselves
            // Otherwise, it will be done as the next step
            if (Rebalance == Rebalancing.NONE)
            {
                // Pay the fees
                for (int i = 0; i < N; i++)
                {
                    currentValues[i] *= 1.0f - MonthlyRebalancingCost / 100.0f;
                }

                double totalValue = Sum(currentValues);

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

    private bool MonthlyRebalance(Context context, List<double> currentValues, int N)
    {
        // Nothing to rebalance if we have a single asset
        if (N == 1)
        {
            return true;
        }

        // Monthly Rebalance if necessary
        if (Rebalance == Rebalancing.MONTHLY)
        {
            // Pay the fees
            for (int i = 0; i < N; i++)
            {
                currentValues[i] *= 1.0f - MonthlyRebalancingCost / 100.0f;
            }

            double totalValue = Sum(currentValues);

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
            double totalValue = Sum(currentValues);
            for (int i = 0; i < N; i++)
            {
                if (Math.Abs((Portfolio.Allocations[i].AllocationCurrent / 100.0f) - currentValues[i] / totalValue) >= Threshold)
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
                    currentValues[i] *= 1.0f - ThresholdRebalancingCost / 100.0f;
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

    private bool YearlyRebalance(Context context, List<double> currentValues, int N)
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
                currentValues[i] *= 1.0f - YearlyRebalancingCost / 100.0f;
            }

            double totalValue = Sum(currentValues);

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

    private bool Withdraw(Context context, List<double> currentValues, int N)
    {
        if ((context.Months - 1) % WithdrawFrequency == 0)
        {
            double totalValue = Sum(currentValues);

            int periods = WithdrawFrequency;

            if ((context.Months - 1) + WithdrawFrequency > context.TotalMonths)
            {
                periods = context.TotalMonths - (context.Months - 1);
            }

            double withdrawalAmount = 0;

            // Compute the withdrawal amount based on the withdrawal strategy
            if (WithdrawalMethod == WithdrawalMethod.STANDARD)
            {
                withdrawalAmount = context.Withdrawal / (12.0f / periods);

            }
            else if (WithdrawalMethod == WithdrawalMethod.CURRENT)
            {

                withdrawalAmount = (totalValue * (WithdrawalRate / 100.0)) / (12.0 / periods);

                // Make sure, we don't go over the minimum
                double minimumWithdrawal = context.MinimumWithdrawalPercent / (12.0 / periods);

                if (withdrawalAmount < minimumWithdrawal)
                {
                    withdrawalAmount = minimumWithdrawal;
                }
            }
            else if (WithdrawalMethod == WithdrawalMethod.VANGUARD)
            {
                // Compute the withdrawal for the year
                if (context.Months == 1)
                {
                    context.VanguardWithdrawal = totalValue * (WithdrawalRate / 100.0);
                    context.LastYearWithdrawal = context.VanguardWithdrawal;
                }
                else if ((context.Months - 1) % 12 == 0)
                {
                    context.LastYearWithdrawal = context.VanguardWithdrawal;
                    context.VanguardWithdrawal = totalValue * (WithdrawalRate / 100.0f);

                    // Don't go over a given maximum decrease or increase
                    if (context.VanguardWithdrawal > (1.0f + VanguardMaxIncrease) * context.LastYearWithdrawal)
                    {
                        context.VanguardWithdrawal = (1.0f + VanguardMaxIncrease) * context.LastYearWithdrawal;
                    }
                    else if (context.VanguardWithdrawal < (1.0f - VanguardMaxDecrease) * context.LastYearWithdrawal)
                    {
                        context.VanguardWithdrawal = (1.0f - VanguardMaxDecrease) * context.LastYearWithdrawal;
                    }
                }

                // The base amount to withdraw
                withdrawalAmount = context.VanguardWithdrawal / (12.0 / periods);

                // Make sure, we don't go over the minimum
                double minimumWithdrawal = context.MinimumWithdrawalPercent / (12.0 / periods);
                if (withdrawalAmount < minimumWithdrawal)
                {
                    withdrawalAmount = minimumWithdrawal;
                }
            }

            if (UseSocialSecurity)
            {
                if ((context.Months / 12.0) >= SocialDelay)
                {
                    withdrawalAmount -= (SocialCoverage * withdrawalAmount);
                }
            }
            context.LastWithdrawalAmount = withdrawalAmount;

            if (withdrawalAmount <= 0.0f)
            {
                return true;
            }

            double effectiveWithdrawalRate = withdrawalAmount / context.YearStartValue;

            // Strategies with cash
            if (CashSimple || ((effectiveWithdrawalRate * 100.0f) >= (WithdrawalRate / 12.0f)))
            {
                // First, withdraw from cash if possible
                if (context.Cash > 0.0f)
                {
                    if (withdrawalAmount <= context.Cash)
                    {
                        context.YearWithdrawn += withdrawalAmount;
                        context.Cash -= withdrawalAmount;
                        withdrawalAmount = 0;
                    }
                    else
                    {
                        context.YearWithdrawn += context.Cash;
                        withdrawalAmount -= context.Cash;
                        context.Cash = 0.0f;
                    }
                }
            }

            // Adjust each value proportionally
            for (int i = 0; i < currentValues.Count; i++)
            {
                double proportion = currentValues[i] / totalValue;
                double withdrawal = proportion * withdrawalAmount;
                currentValues[i] = Math.Max(0.0f, currentValues[i] - withdrawal);
            }

            // Check for failure after the withdrawal
            if (IsFailure(context, Sum(currentValues)))
            {
                context.YearWithdrawn += totalValue;
                return false;
            }
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

        if (Years <= 0)
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

        if (EndYear - StartYear < Years)
        {
            res.Message = $"The period is too short for a {Years} years simulation. The number of years has been reduced to {EndYear - StartYear}";
            Years = EndYear - StartYear;
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

            if (GP_Pass == 0.0f)
            {
                res.Message = $"Invalid pass ({GP_Pass}) for glidepath";
                res.Error = true;
                return res;
            }

            if (GP_Pass > 0.0f && GP_Goal <= Portfolio.Allocations[0].AllocationValue)
            {
                //std::cout << scenario.gp_pass << std::endl;
                //std::cout << scenario.gp_goal << std::endl;
                //std::cout << portfolio[0].allocation << std::endl;
                res.Message = "Invalid goal/pass (1) for glidepath";
                res.Error = true;
                return res;
            }

            if (GP_Pass < 0.0f && GP_Goal >= Portfolio.Allocations[0].AllocationValue)
            {
                //std::cout << scenario.gp_pass << std::endl;
                //std::cout << scenario.gp_goal << std::endl;
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
        var stopwatch = Stopwatch.StartNew();

        res = Validate();
        if (res.Error)
        {
            Console.WriteLine($"Invalid: {res.Message} ");
            return res;
        }

        List<double> terminalValues = new List<double>();
        List<List<double>> withdrawals = new List<List<double>>();

        // 3. Do the actual simulation
        for (int currentYear = StartYear; currentYear <= EndYear - Years; currentYear++)
        {
            for (int currentMonth = 1; currentMonth <= 12; currentMonth++)
            {
                //Console.WriteLine($"Iteration, {externalIndex} year: {currentYear}, month: {currentMonth}");

                double totalWithdrawn = 0.0;
                bool failure = false;

                Context context = new Context();
                context.Months = 1;
                context.TotalMonths = Years * 12;
                // The amount of money withdrawn per year (STANDARD method)
                context.Withdrawal = InitialValue * (WithdrawalRate / 100.0f);
                // The minimum amount of money withdraw (CURRENT method)
                context.MinimumWithdrawalPercent = InitialValue * MinimumWithdrawalPercent;
                // The amount of cash available
                context.Cash = InitialCash;
                // Used for the target threshold
                context.TargetValue = InitialValue;

                Action<Func<bool>> step = result =>
                {
                    if (!failure && !result())
                    {
                        failure = true;
                        res.RecordFailure(context.Months, currentMonth, currentYear);
                    }
                };

                // Reset the allocation for the context
                foreach (var asset in Portfolio.Allocations)
                {
                    asset.AllocationCurrent = asset.AllocationValue;
                }

                // Get the data vector for the current year and month.
                DataVector dv = InflationData.GetDataVector(currentYear, currentMonth);
                IEnumerator<Item> inflationVector = dv.GetEnumerator();
                inflationVector.MoveNext();

                List<double> currentValues = new List<double>(new double[N]);
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

                    currentValues[i] = InitialValue * (Portfolio.Allocations[i].AllocationCurrent / 100.0f);
                }

                // Add an empty list to the list of lists
                withdrawals.Add(new List<double>());

                int endYear = currentYear + (currentMonth - 1 + context.TotalMonths - 1) / 12;
                int endMonth = 1 + (currentMonth - 1 + (context.TotalMonths - 1) % 12) % 12;

                for (int y = currentYear; y <= endYear; y++)
                {
                    context.YearStartValue = currentValues.Sum();
                    context.YearWithdrawn = 0.0;

                    int m;
                    for (m = y == currentYear ? currentMonth : 1; !failure && m <= (y == endYear ? endMonth : 12); m++, context.Months++)
                    {
                        //Console.WriteLine($"{currentYear}/{currentMonth} Simulation, ext index {externalIndex}, index {index}, year: {y}, month: {m}");

                        // Adjust the portfolio with returns and exchanges
                        for (int i = 0; i < N; i++)
                        {
                            currentValues[i] *= returns[i].Current.Value;
                            if (y != returns[i].Current.Year || m != returns[i].Current.Month)
                            {
                                Console.WriteLine($"returns: no match year:{y} month:{m} i:{i}");
                            }
                            returns[i].MoveNext();
                            currentValues[i] *= exchangeRates[i].Current.Value;
                            if (y != exchangeRates[i].Current.Year || m != exchangeRates[i].Current.Month)
                            {
                                Console.WriteLine($"exchange rates: no match year:{y} month:{m} i:{i}");
                            }
                            exchangeRates[i].MoveNext();
                            //Console.WriteLine($"Month: {m}, Year: {y}, Value: {currentValues[i]}, i: {i}");
                        }

                        // Handle failure scenarios
                        step(() => !IsFailure(context, currentValues.Sum()));
                        step(() => Glidepath(context, currentValues, N));
                        //Console.WriteLine($"G Month: {m}, Year: {y}, {currentValues.Sum()}");
                        step(() => MonthlyRebalance(context, currentValues, N));
                        //Console.WriteLine($"R Month: {m}, Year: {y}, {currentValues.Sum()}");
                        step(() => PayFees(context, currentValues, N));
                        //Console.WriteLine($"F Month: {m}, Year: {y}, {currentValues.Sum()}");

                        //double inflation = inflationVector[index].Value;
                        double inflation = inflationVector.Current.Value;
                        if (y != inflationVector.Current.Year || m != inflationVector.Current.Month)
                        {
                            Console.WriteLine($"Inflation: no match year:{y} month:{m}");
                        }
                        inflationVector.MoveNext();
                        // Adjust withdrawals for inflation
                        context.Withdrawal *= inflation;
                        context.MinimumWithdrawalPercent *= inflation;
                        context.TargetValue *= inflation;

                        // Perform withdrawals
                        step(() => Withdraw(context, currentValues, N));
                        //Console.WriteLine($"W Month: {m}, Year: {y}, {currentValues.Sum()}");

                        // Record withdrawal
                        if ((context.Months - 1) % 12 == 0)
                        {
                            withdrawals.Last().Add(context.LastWithdrawalAmount);
                        }
                        else
                        {
                            withdrawals.Last()[withdrawals.Last().Count - 1] += context.LastWithdrawalAmount;
                        }
                        //Console.WriteLine($"Month: {m}, Year: {y}, {currentValues.Sum()}");
                    }

                    totalWithdrawn += context.YearWithdrawn;
                    step(() => YearlyRebalance(context, currentValues, N));

                    if (failure)
                    {
                        //Console.WriteLine("Simulation failure");
                        double effectiveWithdrawalRate = context.YearWithdrawn / context.YearStartValue;

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
                    //Console.WriteLine($"Year: {y}, {currentValues.Sum()}");
                }

                double finalValue = failure ? 0.0 : currentValues.Sum();

                //Console.WriteLine($"Final value: {finalValue}, current year: {currentYear}");

                if (!failure)
                {
                    res.Successes++;
                    res.WithdrawnPerYear += totalWithdrawn;
                }
                else
                {
                    res.Failures++;
                }

                terminalValues.Add(finalValue);

                if (failure)
                {
                    withdrawals.RemoveAt(withdrawals.Count - 1);
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
        res.WithdrawnPerYear = (res.WithdrawnPerYear / Years) / res.Successes;
        res.SuccessRate = 100.0f * (res.Successes / (float)(res.Successes + res.Failures));
        res.ComputeTerminalValues(terminalValues);
        res.ComputeWithdrawals(withdrawals, Years);

        stopwatch.Stop();

        return res;
    }

    private Results ValidateAndAdaptYears(int N)
    {
        Results results = new Results();

        bool changed = false;

        if (StrictValidation)
        {
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
                if (asset == "cad_stocks" || asset == "cad_bonds")
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
                        valuesCopy.AddData(new Item(rate.Month, rate.Year, 1));
                    }
                    ExchangeRates.Add(valuesCopy);
                }
            }
            else if (currency == "cad")
            {
                if (asset == "cad_stocks" || asset == "cad_bonds")
                {
                    ExchangeSet[i] = false;
                    DataVector valuesCopy = new DataVector("exchange rates");
                    foreach (var rate in Values[i])
                    {
                        valuesCopy.AddData(new Item(rate.Month, rate.Year, 1));
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