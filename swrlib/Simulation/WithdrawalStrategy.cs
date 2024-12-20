using System.ComponentModel;

namespace Swr.Simulation;

public class WithdrawalStrategy
{
    public WithdrawalStrategy(float InitialInvestment)
    {
        this.InitialInvestment = InitialInvestment;
    }

    // initial investment Value
    private float InitialInvestment;
    // default as monthly(1)
    private int frequency = 1;
    public WithdrawalFrequency WithdrawalFrequency
    {
        get
        {
            return frequency switch
            {
                12 => WithdrawalFrequency.MONTHLY,
                1 => WithdrawalFrequency.YEARLY,
                _ => throw new ApplicationException("Invalid frequency conversion")
            };
        }
        set
        {
            frequency = value switch
            {
                WithdrawalFrequency.MONTHLY => 12,
                WithdrawalFrequency.YEARLY => 1,
                _ => throw new ApplicationException("Invalid frequency conversion")
            };
        }
    }
    private WithdrawalMethod method = WithdrawalMethod.STANDARD;
    public WithdrawalMethod WithdrawalMethod { get { return method; } set { method = value; } }
    // percent: withdrawal rate
    public float WithdrawalRate { get; set; } = 4.0f;
    // minimum can't go lower 
    public float MinimumWithdrawalRate { get; set; } = 3.0f;

    public float VanguardMaxIncreaseRate { get; set; } = 5.0f;
    public float VanguardMaxDecreaseRate { get; set; } = 2.0f;
    public bool UseCashWithdrawal { get; set; } = false;

    private float lastYearWithdrawal = 0.0f;
    private float lastWithdrawalAmount = 0.0f;
    private float yearWithdrawn = 0.0f;

    private bool Validate()
    {
        bool valid = false;
        // must have frequency
        if (!(frequency == 1 || frequency == 12))
        {
            throw new InvalidEnumArgumentException(nameof(frequency));
        }
        // WithdrawalRate must be positive
        if (WithdrawalRate <= 0)
        {
            throw new ArgumentException(nameof(WithdrawalRate));
        }

        switch (method)
        {
            case WithdrawalMethod.STANDARD:
                // must have frequency
                break;
            case WithdrawalMethod.CURRENT:
                break;
            case WithdrawalMethod.VANGUARD:
                break;
        }
        valid = true;
        return valid;
    }

    private float WithrawalAmount(float currentValue, float rate, int periods)
    {
        return (currentValue * (rate / 100.0f)) / (12.0f / periods);
    }

    public float CalculateWithdrawalAmount(int monthIndex, int totalMonths, float currentValue, float inflation)
    {
        if (!Validate())
        {
            throw new ApplicationException("invalid withdrawal use case");
        }
        // index with inflation
        InitialInvestment *= inflation;

        float withdrawalAmount = 0f;

        // Perform withdrawals only at specified intervals based on WithdrawFrequency.
        if ((monthIndex - 1) % frequency == 0)
        {
            // Determine the number of withdrawal periods remaining.
            int periods = frequency;
            if ((monthIndex - 1) + frequency > totalMonths)
            {
                periods = totalMonths - (monthIndex - 1);
            }

            // Compute the withdrawal amount based on the withdrawal strategy
            if (method == WithdrawalMethod.STANDARD)
            {
                // Fixed annual withdrawal rate divided into periodic withdrawals.
                withdrawalAmount = WithrawalAmount(InitialInvestment, WithdrawalRate, periods);
            }
            else if (method == WithdrawalMethod.CURRENT)
            {
                // Percentage-based withdrawal tied to the current portfolio value.
                withdrawalAmount = WithrawalAmount(currentValue, WithdrawalRate, periods);
                // Ensure the withdrawal does not fall below the specified minimum.
                float minimumWithdrawalAmount = WithrawalAmount(InitialInvestment, MinimumWithdrawalRate, periods);
                if (withdrawalAmount < minimumWithdrawalAmount)
                {
                    withdrawalAmount = minimumWithdrawalAmount;
                }
            }
            else if (method == WithdrawalMethod.VANGUARD)
            {
                // Vanguard's dynamic withdrawal strategy, adjusting year-over-year.
                if (monthIndex == 1)
                {
                    // First year: initialize Vanguard withdrawal
                    withdrawalAmount = currentValue * (WithdrawalRate / 100.0f);
                    lastYearWithdrawal = withdrawalAmount;
                }
                else if ((monthIndex - 1) % 12 == 0)
                {
                    // Update withdrawals annually.
                    withdrawalAmount = currentValue * (WithdrawalRate / 100.0f);
                    // Cap increases and decreases based on specified limits.
                    if (withdrawalAmount > (1.0f + VanguardMaxIncreaseRate / 100.0f) * lastYearWithdrawal)
                    {
                        withdrawalAmount = (1.0f + VanguardMaxIncreaseRate / 100.0f) * lastYearWithdrawal;
                    }
                    else if (withdrawalAmount < (1.0f - VanguardMaxDecreaseRate / 100.0f) * lastYearWithdrawal)
                    {
                        withdrawalAmount = (1.0f - VanguardMaxDecreaseRate / 100.0f) * lastYearWithdrawal;
                    }
                    lastYearWithdrawal = withdrawalAmount;
                }
                // Adjust withdrawal to a periodic base
                withdrawalAmount = WithrawalAmount(currentValue, WithdrawalRate, periods);
                // Ensure the withdrawal does not fall below the specified minimum.
                float minimumWithdrawalAmount = WithrawalAmount(InitialInvestment, MinimumWithdrawalRate, periods);
                if (withdrawalAmount < minimumWithdrawalAmount)
                {
                    withdrawalAmount = minimumWithdrawalAmount;
                }
            }

            /*
            // Adjust withdrawal based on social security coverage if applicable.
            if (UseSocialSecurity)
            {
                if ((context.MonthIndex / 12.0f) >= SocialDelay)
                {
                    withdrawalAmount -= (SocialCoverage * withdrawalAmount);
                }
            }
            */
            this.lastWithdrawalAmount = withdrawalAmount;
            this.yearWithdrawn += withdrawalAmount;
        }
        return withdrawalAmount;
    }
}
// If no withdrawal amount is required, exit early as successful.
//if (withdrawalAmount <= 0.0f)
//{
//    return true;
//}

// Calculate the effective withdrawal rate.
//float effectiveWithdrawalRate = withdrawalAmount / context.YearStartValue;

// Strategies with cash or effective withdrawal rate is greater than the monthly WithdrawalRate
// withdrawing from cash if the effective rate exceeds the target monthly rate.
/*
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
*/

// Adjust each portfolio value proportionally to account for the withdrawal.
//for (int i = 0; i < currentValues.Count; i++)
//{
//    float proportion = currentValues[i] / totalValue;
//    float withdrawal = proportion * withdrawalAmount;
//    currentValues[i] = Math.Max(0.0f, currentValues[i] - withdrawal);
//}
// Check for portfolio failure after the withdrawal
//if (IsFailure(context, Sum(currentValues)))
//{
//    context.YearWithdrawn += totalValue;
//    return false;
//}
// Update the total withdrawn for the year
//context.YearWithdrawn += withdrawalAmount;

//}
//return true;
//}

