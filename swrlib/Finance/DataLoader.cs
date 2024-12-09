
using Swr.Investment;
using System.Runtime.Caching;

namespace Swr.Finance;

public static class DataLoader
{
    public static string GetFilePath(string fileName)
    {
        return Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, $"stock-data/{fileName}.csv"));
    }

    public static List<DataVector> LoadValues(List<Allocation> portfolio)
    {
        var values = new List<DataVector>();
        for (int index = 0; index < portfolio.Count(); index++)
        {
            string assetName = portfolio[index].Asset;

            bool x2 = assetName.EndsWith("_x2");
            string filename = x2 ? assetName.Substring(0, assetName.Length - 3) : assetName;
            var data = LoadData(filename, GetFilePath(filename));

            if (data.Empty())
            {
                Console.WriteLine($"Impossible to load data for asset {assetName}");
                return new List<DataVector>();
            }

            data.NormalizeData();
            data.TransformToReturns();

            if (x2)
            {
                var copy = new DataVector(data.Name);
                int startYear = data[0].Year;
                int yearsInData = data.Size() / 12;

                for (int i = yearsInData; i > 0; i--)
                {
                    foreach (var item in data.Data.GetRange(0, 12))
                    {
                        copy.AddData(new Item(item.Month, startYear - i, item.Value));
                    }
                }
                data.Data.InsertRange(0, copy.Data);
            }

            values.Add(data);
        }

        return values;
    }

    public static DataVector LoadInflation(List<DataVector> values, string inflation)
    {
        DataVector inflationData;

        if (inflation == "no-inflation")
        {
            inflationData = new DataVector(values[0].Name);
            foreach (var value in values[0])
            {
                inflationData.AddData(new Item(value.Month, value.Year, 1.0f));
            }
        }
        else
        {
            inflationData = LoadData(inflation, GetFilePath(inflation));

            if (inflationData.Empty())
            {
                Console.WriteLine($"Impossible to load inflation data for asset {inflation}");
                return new DataVector("empty");
            }

            inflationData.NormalizeData();
            inflationData.TransformToReturns();
        }

        return inflationData;
    }

    public static DataVector LoadExchange(string exchange)
    {
        DataVector data;

        data = LoadData(exchange, GetFilePath(exchange));

        if (data.Empty())
        {
            Console.WriteLine($"Impossible to load exchange data for {exchange}");
            return new DataVector("empty");
        }

        data.NormalizeData();
        data.TransformToReturns();

        return data;
    }

    public static DataVector LoadExchangeInv(string exchange)
    {
        DataVector originalData;

        originalData = LoadData(exchange, GetFilePath(exchange));

        if (originalData.Empty())
        {
            Console.WriteLine($"Impossible to load exchange data for {exchange}");
            return new DataVector("empty");
        }

        var invertedData = new DataVector($"{exchange}_inv");

        foreach (var item in originalData.Data)
        {
            invertedData.AddData(new Item(item.Month, item.Year, 1.0f / item.Value));
        }

        invertedData.NormalizeData();
        invertedData.TransformToReturns();

        return invertedData;
    }

    private static readonly MemoryCache _cache = MemoryCache.Default;
    private static DataVector LoadData(string name, string filePath)
    {
        //check in cache
        if (_cache.Contains(filePath))
        {
            return (DataVector)_cache.Get(filePath);
        }
        else
        {
            var dataVector = new DataVector(name);
            dataVector.LoadDataFromCsv(filePath);
            _cache.Add(filePath, dataVector, DateTimeOffset.Now.AddMinutes(5));
            return dataVector;
        }
    }
}