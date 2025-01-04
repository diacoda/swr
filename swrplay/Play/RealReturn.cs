using Microsoft.Extensions.Logging;
using Swr.Data;

namespace Swr.Play;

public class RealReturn
{

    private ILogger<RealReturn> _logger;
    public RealReturn(ILogger<RealReturn> logger)
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
        while ((line = reader.ReadLine()) != null)
        {
            var values = line.Split(',');
            if (values.Length == 3)
            {
                int month = int.Parse(values[0]);
                int year = int.Parse(values[1]);
                float value = float.Parse(values[2]);

                var dataItem = new Item(month, year, value);
                data.Add(dataItem);
            }
        }
        return data;
    }

    public void Calculate()
    {
        List<Item> nominalAsset = LoadFromCSV("us_stocks");
        List<Item> normalizedAsset = Normalize(nominalAsset);
        List<Item> returnsAsset = TransformToReturn(normalizedAsset);

        List<Item> nominalInflation = LoadFromCSV("us_inflation");
        List<Item> normalizedInflation = Normalize(nominalInflation);
        List<Item> returnsInflation = TransformToReturn(normalizedInflation);

        List<Item> realReturns = AssetRealReturns(returnsAsset, returnsInflation);
    }

    public void CumulativeBackwardRealReturn(List<Item> assetRealReturns, int totalMonths)
    {
        // =mmult(Q1836:Y1836,'Parameters & Main Results'!$B$8:$B$16)-'Parameters & Main Results'!$B$21/12+mmult(Z1836:AA1836,'Parameters & Main Results'!$B$18:$B$19)
        // asset real return - expense ratio/12 + FAMA-French style
        // AC1838=AC1839*(1+AB1838)
        // C(t)= C(t+1)(1*RR(t))
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
    private List<Item> AssetRealReturns(List<Item> assetReturns, List<Item> inflationReturns)
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
        item.Month = assetReturns[0].Month;
        item.Year = assetReturns[0].Year;
        item.Value = (assetReturns[0].Value / inflationReturns[0].Value) - 1;
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
            item.Month = assetReturns[i].Month;
            item.Year = assetReturns[i].Year;
            item.Value = (assetReturns[i].Value / inflationReturns[i].Value) - 1;
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
        item.Month = normalized[0].Month;
        item.Year = normalized[0].Year;
        returns.Add(item);

        for (int i = 1; i < normalized.Count; i++)
        {
            item = new Item();
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
        item.Value = 1.0f;
        item.Month = nominal[0].Month;
        item.Year = nominal[0].Year;
        normalized.Add(item);

        for (int i = 1; i < nominal.Count; i++)
        {
            item = new Item();
            item.Value = normalized[i - 1].Value * nominal[i].Value / nominal[i - 1].Value;
            item.Month = nominal[i].Month;
            item.Year = nominal[i].Year;
            normalized.Add(item);
        }
        return normalized;
    }
}