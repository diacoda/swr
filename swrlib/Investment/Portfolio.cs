namespace Swr.Investment;
public class Portfolio
{
    List<Allocation> portfolio = new List<Allocation>();

    public Portfolio(string portfolioView)
    {
        this.portfolio = ParseAllocation(portfolioView, false);
        NormalizePortfolio();
    }

    private List<Allocation> ParseAllocation(string portfolioView, bool allowZero)
    {
        List<Allocation> portfolio = new List<Allocation>();
        var positions = portfolioView.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var position in positions)
        {
            var parts = position.Split(':');
            if (parts.Length == 2)
            {
                var asset = parts[0];
                if (float.TryParse(parts[1], out var allocation))
                {
                    if (allowZero || allocation > 0.0f)
                    {
                        portfolio.Add(new Allocation { Asset = asset, AllocationPercentage = allocation });
                    }
                }
                else
                {
                    throw new ApplicationException($"Invalid allocation value for asset {asset}");
                }
            }
            else
            {
                throw new ApplicationException($"Invalid portfolio position: {position}");
            }
        }
        return portfolio;
    }

    public List<Allocation> Allocations { get { return portfolio; } }

    public float TotalAllocation()
    {
        float total = 0;

        foreach (var position in portfolio)
        {
            total += position.AllocationPercentage;
        }

        return total;
    }

    public void NormalizePortfolio()
    {
        float total = TotalAllocation();

        if (total != 100.0f)
        {
            for (int i = 0; i < portfolio.Count; i++)
            {
                portfolio[i].AllocationPercentage *= 100.0f / total;
            }
        }
    }
}