using Microsoft.Extensions.Logging;
using Swr.Data;
using Swr.Investment;
using Swr.Simulation;

namespace Swr.Play;

public class Simulator
{
    private ILogger<Simulator> _logger;
    public Simulator(ILogger<Simulator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private string GetFilePath(string assetName)
    {
        string fileName = assetName;
        return Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, $"stock-data/{fileName}.csv"));
    }

    private List<Item> LoadFromCSV(string assetName)
    {
        List<Item> data = new List<Item>();
        string filePath = GetFilePath(assetName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"{filePath} not found");
        }
        using StreamReader reader = new StreamReader(filePath);
        string? line;
        int index = 0;
        while ((line = reader.ReadLine()) != null)
        {
            var values = line.Split(',');
            if (values.Length == 3)
            {
                int month = int.Parse(values[0]);
                int year = int.Parse(values[1]);
                float value = float.Parse(values[2]);

                var dataItem = new Item(month, year, value);
                dataItem.Index = ++index;
                data.Add(dataItem);
            }
        }
        
        // Method to ensure the data ends with a full year
        while (data.Count > 0 && data[data.Count - 1].Month != 12)
        {
            data.RemoveAt(data.Count - 1);
        }
    
        // Method to ensure the data starts with a full year
        while (data.Count > 0 && data[0].Month != 1)
        {
            data.RemoveAt(0);
        }
        return data;
    }

    private List<Item> CalculateSCR(List<Item> c1s, List<Item> sumCs, int totalMonths)
    {
        List<Item> scr = new List<Item>();
        int nc1s = c1s.Count();
        int nsumCs = sumCs.Count();
        if(nc1s + totalMonths != nsumCs)
        {
            throw new Exception("s");
        }

        c1s = c1s.OrderBy(x => x.Index).ToList();
        sumCs = sumCs.OrderBy(x => x.Index).ToList();
        for (int i = 0; i < nc1s; i++)
        {
            Item item = new Item();
            item.Index = c1s[i].Index;
            item.Month = c1s[i].Month;
            item.Year = c1s[i].Year;
            item.Value = c1s[i].Value / sumCs[i].Value * 12;
            scr.Add(item);
        }
        return scr;
    }

    private List<Item> SumOfC(List<Item> cumulativeBackwardRealReturn, int totalMonths)
    {
        List<Item> sum = new List<Item>();
        cumulativeBackwardRealReturn = cumulativeBackwardRealReturn.OrderBy(x => x.Index).ToList();
        int count = cumulativeBackwardRealReturn.Count();
        for(int i = 0; i < count; i++)
        {
            Item item = new Item();
            float sumC = 0.0f;
            float divisor = 0.0f;
            for(int j = i; j < i + totalMonths; j++)
            {
                if(j<count)
                {
                    sumC += cumulativeBackwardRealReturn[j].Value;
                    if(j == i + totalMonths - 1)
                    {
                        divisor = cumulativeBackwardRealReturn[j].Value;
                    }
                }
            }
            item.Index = cumulativeBackwardRealReturn[i].Index;
            item.Year = cumulativeBackwardRealReturn[i].Year;
            item.Month = cumulativeBackwardRealReturn[i].Month;
            if(divisor != 0)
            {
                item.Value = sumC / divisor;
            }
            sum.Add(item);
        } 
        return sum;
    }

    private List<Item> CalculateC1(List<Item> cumulativeBackwardRealReturn, int totalMonths)
    {
        List<Item> c1s = new List<Item>();
        cumulativeBackwardRealReturn = cumulativeBackwardRealReturn.OrderBy(x => x.Index).ToList();
        for(int i = 0; i < cumulativeBackwardRealReturn.Count; i++)
        {
            Item item = new Item();
            item.Index = cumulativeBackwardRealReturn[i].Index;
            item.Month = cumulativeBackwardRealReturn[i].Month;
            item.Year = cumulativeBackwardRealReturn[i].Year;
            if(i + totalMonths < cumulativeBackwardRealReturn.Count)
            {
                item.Value = cumulativeBackwardRealReturn[i].Value / cumulativeBackwardRealReturn[i+totalMonths].Value;
                c1s.Add(item);
            }
        }
        return c1s;
    }

    private List<Item> CumulativeBackwardRealReturn(List<Item> assetRealReturns)
    {
        // Given the total month, calculate the ending simulation date = today + total month
        // that month is where cumulative backward real return is 1
        // use this: C(t)= C(t+1)(1*RR(t))
        // =mmult(Q1836:Y1836,'Parameters & Main Results'!$B$8:$B$16)-'Parameters & Main Results'!$B$21/12+mmult(Z1836:AA1836,'Parameters & Main Results'!$B$18:$B$19)
        // asset real return - expense ratio/12 + FAMA-French style
        // AC1838=AC1839*(1+AB1838)
        // C(t)= C(t+1)(1*RR(t))
        List<Item> cumulativeBackwardRealReturns = new List<Item>();
        // if I have data from 1871 to the last month, I need to add totalMonths to the count
        // That is C1 = 1
        assetRealReturns = assetRealReturns
                            .OrderByDescending(x => x.Index)
                            .ToList();

        Item item = new Item();
        item.Index = assetRealReturns[0].Index;
        item.Year = assetRealReturns[0].Year;
        item.Month = assetRealReturns[0].Month;
        item.Value = 1.0f;
        cumulativeBackwardRealReturns.Add(item);

        for(int i = 1; i < assetRealReturns.Count; i++)
        {
            item = new Item();
            item.Index = assetRealReturns[i].Index;
            item.Value = cumulativeBackwardRealReturns[i-1].Value * (1 + assetRealReturns[i].Value);
            item.Year = assetRealReturns[i].Year;
            item.Month = assetRealReturns[i].Month;
            cumulativeBackwardRealReturns.Add(item);
        }
        return cumulativeBackwardRealReturns;
    }

    /// <summary>
    /// First, let’s define variable C(t) as the total cumulative return of one dollar invested 
    /// between the beginning of month t and the final period T, which is the end of the retirement 
    /// horizon, e.g., 720 months in our case, or 360 months in the Trinity Study. 
    /// Think of this as an opportunity cost factors, the loss of each dollar of a withdrawal in period t 
    /// measured in period T dollars. If the (real, inflation-adjusted) capital market returns are r,
    /// then we can calculate this as:
    /// C(t) = Product (x from t to T) (1 + r(x))
    /// So, the C(t) are simply the cumulative capital market returns, but moving backward rather than
    /// forward. Also, note that C1 is the cumulative return of the initial principal if held over 
    /// the entire retirement horizon. In other words,
    /// C(T) = 1 + r(T)
    /// C(T-1) = C(T) * (1 + r(T-1)) = (1 + r(T)) * (1 + r(T-1))
    /// C(1) = Product (x from 1 to T) (1 + r(x))
    /// We can now calculate the final asset value of a portfolio with an initial value of one 
    /// and withdrawals w every month as:
    /// FV = C(1) - w * Sum (x from 1 to T) C(x)
    /// The first term on the right side is how much the portfolio would have grown in the absence of
    /// any withdrawals, and the second term is the total opportunity cost of all withdrawals 
    /// translated into date T dollars. 
    /// Notice that even the final month’s withdrawal is subjected to a return rT because the 
    /// withdrawal comes out at the beginning of the month while the final asset value is marked 
    /// at the end of the final month.
    /// Now we can easily solve for the withdrawal rate w that generates the final value target as
    /// w = C(1) - FV / Sum (x from 1 to T) C(x)
    /// </summary>
    /// <param name="assetReturns"></param>
    /// <param name="inflationReturns"></param>
    /// <returns></returns>
    /// <exception cref="ApplicationException"></exception>
    private List<Item> AssetRealReturns(List<Item> assetReturns, List<Item> inflationReturns, float expenseRatio)
    {
        List<Item> assetRealReturns = new List<Item>();
        if (!(assetReturns[0].Month == inflationReturns[0].Month))
        {
            throw new ApplicationException("no month match");
        }
        if (!(assetReturns[0].Year == inflationReturns[0].Year))
        {
            throw new ApplicationException("no month match");
        }

        Item item = new Item();
        item.Index = assetReturns[0].Index;
        item.Month = assetReturns[0].Month;
        item.Year = assetReturns[0].Year;
        item.Value = ((assetReturns[0].Value - expenseRatio) / inflationReturns[0].Value) - 1;
        assetRealReturns.Add(item);

        for (int i = 1; i < assetReturns.Count; i++)
        {
            if (!(assetReturns[i].Month == inflationReturns[i].Month))
            {
                throw new ApplicationException("no month match");
            }
            if (!(assetReturns[i].Year == inflationReturns[i].Year))
            {
                throw new ApplicationException("no year match");
            }
            item = new Item();
            item.Index = assetReturns[i].Index;
            item.Month = assetReturns[i].Month;
            item.Year = assetReturns[i].Year;
            item.Value = ((assetReturns[i].Value - expenseRatio) / inflationReturns[i].Value) - 1;
            assetRealReturns.Add(item);
        }
        return assetRealReturns;
    }
    
    private List<Item> TransformToReturn(List<Item> normalized)
    {
        List<Item> returns = new List<Item>();
        if (normalized.Count == 0)
        {
            return returns;
        }
        Item item = new();
        item.Value = 1.0f;
        item.Index = normalized[0].Index;
        item.Month = normalized[0].Month;
        item.Year = normalized[0].Year;
        returns.Add(item);

        for (int i = 1; i < normalized.Count; i++)
        {
            item = new Item();
            item.Index = normalized[i].Index;
            item.Value = normalized[i].Value / normalized[i - 1].Value;
            item.Month = normalized[i].Month;
            item.Year = normalized[i].Year;
            returns.Add(item);
        }
        return returns;
    }

    private List<Item> Normalize(List<Item> nominal)
    {
        List<Item> normalized = new List<Item>();
        if (nominal.Count == 0)
        {
            return normalized;
        }

        if (Math.Abs(nominal[0].Value - 1.0) < 0.0001)
        {
            return normalized;
        }

        Item item = new();
        item.Index = nominal[0].Index;
        item.Value = 1.0f;
        item.Month = nominal[0].Month;
        item.Year = nominal[0].Year;
        normalized.Add(item);

        for (int i = 1; i < nominal.Count; i++)
        {
            item = new Item();
            item.Index = nominal[i].Index;
            item.Value = normalized[i - 1].Value * nominal[i].Value / nominal[i - 1].Value;
            item.Month = nominal[i].Month;
            item.Year = nominal[i].Year;
            normalized.Add(item);
        }
        return normalized;
    }

    float[] ConvertListToFloat(List<Item> items)
    {
        float[] values = items.Select(item => item.Value).ToArray();
        return values;
    }
    private float CalculatePercentile(float[] data, float percentile)
    {
        if (data == null || data.Length == 0)
        {
            throw new ArgumentException("Data array must not be null or empty.");
        }

        if (percentile < 0 || percentile > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(percentile), "Percentile (percentile) must be between 0 and 1.");
        }

        // Sort the data
        var sortedData = data.OrderBy(x => x).ToArray();

        // Calculate the position
        float position = (sortedData.Length - 1) * percentile;
        int lowerIndex = (int)Math.Floor(position);
        int upperIndex = (int)Math.Ceiling(position);

        // Interpolate if needed
        if (lowerIndex == upperIndex)
        {
            return sortedData[lowerIndex];
        }
        else
        {
            float weight = position - lowerIndex;
            return sortedData[lowerIndex] * (1 - weight) + sortedData[upperIndex] * weight;
        }
    }

    private List<Item> TotalReturns(List<Asset> assets)
    {
        
        int assetCount = assets.Count();
        if(assetCount <= 0)
        {
            throw new ApplicationException("");
        }

        int returnsCount = assets[0].Returns.Count;
        for(int c=1; c<assetCount; c++)
        {
            if(returnsCount!=assets[c].Returns.Count)
            {
                throw new ApplicationException("");
            }
        }
        List<Item> totalReturns = new List<Item>();
        for (int i =0; i<returnsCount; i++)
        {
            float totalReturn = 0.0f;
            for(int j =0; j<assetCount; j++)
            {
                totalReturn += assets[j].Returns[i].Value * assets[j].Percent;
            }
            Item item = new Item();
            item.Value = totalReturn;
            item.Index = assets[0].Returns[i].Index;
            item.Month = assets[0].Returns[i].Month;
            item.Year = assets[0].Returns[i].Year;
            totalReturns.Add(item);
        }

        return totalReturns;
    }
    public WithdrawalRates CalculateWithdrawalRates(Scenario scenario)
    {
        int assetsCount = scenario.Portfolio.Allocations.Count;

        List<Asset> assets = new List<Asset>(assetsCount);
        foreach(Allocation allocation in scenario.Portfolio.Allocations)
        {
            Asset asset = new Asset(allocation.Asset);
            asset.Percent = allocation.AllocationPercentage / 100.0f;
            List<Item> nominal = LoadFromCSV(allocation.Asset);
            asset.Nominal = nominal;
            List<Item> normalized = Normalize(nominal);
            asset.Normalized = normalized;
            List<Item> returns = TransformToReturn(normalized);
            asset.Returns = returns;
            assets.Add(asset);
        }

        List<Item> nominalInflation = LoadFromCSV(scenario.Inflation);
        List<Item> normalizedInflation = Normalize(nominalInflation);
        List<Item> returnsInflation = TransformToReturn(normalizedInflation);

        List<Item> totalReturns = TotalReturns(assets);
        
        List<Item> realReturns = AssetRealReturns(totalReturns, returnsInflation, scenario.ExpenseRatio);
        List<Item> cumulativeBackwardRealReturn = CumulativeBackwardRealReturn(realReturns);
        int endYear = scenario.EndYear;
        int startYear = scenario.StartYear;
        int totalMonths = scenario.TimeHorizon * 12;
        List<Item> c1s = CalculateC1(cumulativeBackwardRealReturn, totalMonths);
        List<Item> sumCs = SumOfC(cumulativeBackwardRealReturn, totalMonths);

        List<Item> scr = CalculateSCR(c1s, sumCs, totalMonths);
        float[] values = ConvertListToFloat(scr);
        WithdrawalRates withdrawalRates= new WithdrawalRates();
        var sortedData = values.OrderBy(x => x).ToArray();
        float scr0 = CalculatePercentile(values,0.0f);
        withdrawalRates.Fail0Percent = scr0 * 100;
        Console.WriteLine($"0% Failure rate: {scr0}");
        float scr1 = CalculatePercentile(values,0.01f);
        withdrawalRates.Fail1Percent = scr1 * 100;
        Console.WriteLine($"1% Failure rate: {scr1}");
        float scr2 = CalculatePercentile(values,0.02f);
        withdrawalRates.Fail2Percent = scr2 * 100;
        Console.WriteLine($"2% Failure rate: {scr2}");
        float scr5 = CalculatePercentile(values,0.05f);
        withdrawalRates.Fail5Percent = scr5 * 100;
        Console.WriteLine($"5% Failure rate: {scr5}");
        return withdrawalRates;
    }
}