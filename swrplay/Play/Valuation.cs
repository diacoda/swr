using Microsoft.Extensions.Logging;
namespace Swr.Play;

public class Valuation
{
    private ILogger<Valuation> _logger;
    public Valuation(ILogger<Valuation> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Name { get; set; } = string.Empty;
    public float InitialValue { get; set; } = 10000.0f;
    private string GetFilePath(string assetName)
    {
        string fileName = assetName;
        return Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, $"stock-data/{fileName}.csv"));
    }

    public void Do(string name)
    {
        Name = name;
        string filePath = GetFilePath(Name);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"{filePath} not found");
        }
        using StreamReader reader = new StreamReader(filePath);
        string? line;
        int index = 0;
        float initialNormalized = 1.0f;
        float prevNormalized = initialNormalized;
        float initialRateReturn = 1.0f;
        float prevRateReturn = initialRateReturn;
        float totalReturn = InitialValue;
        float prevTotalReturn = InitialValue;
        float prevValue = 0.0f;

        bool first = true;

        while ((line = reader.ReadLine()) != null)
        {
            var values = line.Split(',');
            if (values.Length == 3)
            {
                Record r = new Record();
                index++;
                r.Index = index;
                int month = int.Parse(values[0]);
                r.Month = month;
                int year = int.Parse(values[1]);
                r.Year = year;
                float value = float.Parse(values[2]);
                r.Value = value;
                prevValue = r.Value;
                if (first)
                {
                    r.Normalized = initialNormalized;
                    r.RateReturn = initialRateReturn;
                    r.TotalReturn = totalReturn;
                    first = false;
                    continue;
                }
                r.Normalized = value / prevValue * prevNormalized;
                prevNormalized = r.Normalized;
                r.RateReturn = (value / prevValue) - 1;
                prevRateReturn = r.RateReturn;
                r.TotalReturn = totalReturn * (1 + r.RateReturn);
            }
        }
    }

}