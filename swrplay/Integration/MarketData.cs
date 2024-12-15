using System.Runtime.CompilerServices;
using Integration;
using Microsoft.Extensions.Logging;
using OoplesFinance.YahooFinanceAPI;
using OoplesFinance.YahooFinanceAPI.Enums;
using OoplesFinance.YahooFinanceAPI.Models;

namespace Swr.Integration;
public class MarketData
{
    private ILogger<MarketData> _logger;
    public MarketData(ILogger<MarketData> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string?> GetData(string ticker, TickerFrequency frequency)
    {
        DataFrequency dataFrequency = frequency switch
        {
            TickerFrequency.Daily => DataFrequency.Daily,
            TickerFrequency.Monthly => DataFrequency.Monthly,
            _ => throw new ApplicationException("Invalid frequency conversion")
        };

        DateTime startDate = new DateTime(1871, 1, 1);
        DateTime endDate = DateTime.Now;
        var yahooFinanceApi = new YahooClient();
        IEnumerable<HistoricalChartInfo> historicalData = await yahooFinanceApi.GetHistoricalDataAsync(ticker, dataFrequency, startDate, endDate);

        string? filePath = null;
        if (historicalData != null && historicalData.Count() > 0)
        {
            filePath = $"stock-data/{ticker}_{dataFrequency.ToString()}.csv";
            using (var writer = new System.IO.StreamWriter(filePath))
            {
                writer.WriteLine("Year,Month,Day,Close,AdjustedClose");
                foreach (var result in historicalData)
                {
                    writer.WriteLine($"{result.Date.Year},{result.Date.Month},{result.Date.Day},{result.Close:F2},{result.AdjustedClose:F2}");
                }
            }
        }
        else
        {
            _logger.LogInformation($"No historical data found for the specified date range. {ticker}, {dataFrequency}, {startDate}, {endDate} ");
        }
        return filePath;
    }

    public void TransformCanadianStocks()
    {
        String? line;
        try
        {
            using StreamReader reader = new StreamReader(Path.Combine(Environment.CurrentDirectory, "stock-data/ca-stokcs.csv"));
            using StreamWriter writer = new StreamWriter(Path.Combine(Environment.CurrentDirectory, "stock-data/ca-stocks.csv"));
            line = reader.ReadLine();
            while (line != null)
            {

                //write the line to console window
                Console.WriteLine(line);
                string[] data = line.Split(',', StringSplitOptions.RemoveEmptyEntries);
                int month = data[0] switch
                {
                    "January" => 1,
                    "February" => 2,
                    "March" => 3,
                    "April" => 4,
                    "May" => 5,
                    "June" => 6,
                    "July" => 7,
                    "August" => 8,
                    "September" => 9,
                    "October" => 10,
                    "November" => 11,
                    "December" => 12,
                    _ => throw new ApplicationException("Invalid frequency conversion")
                };
                int year = int.Parse(data[1]);
                string v = data[2];
                if (data[2].StartsWith('"'))
                {
                    v = data[2].Substring(1);
                    v += data[3].Substring(0, data[3].Length - 1);
                }
                double value = double.Parse(v);
                writer.WriteLine($"{month},{year},{value}");
                //Read the next line
                line = reader.ReadLine();
            }
            reader.Close();
            writer.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception: " + e.Message);
        }
        finally
        {

        }
    }

    public void TransformCanadaCPI()
    {
        String? line;
        try
        {
            using StreamReader reader = new StreamReader(Path.Combine(Environment.CurrentDirectory, "stock-data/18100004.csv"));
            using StreamWriter writer = new StreamWriter(Path.Combine(Environment.CurrentDirectory, "stock-data/ca_inflation.csv"));
            line = reader.ReadLine();
            while (line != null)
            {

                //write the line to console window
                //Console.WriteLine(line);
                string[] data = line.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (data[1] == "\"Canada\"" && data[3] == "\"All-items\"")
                {
                    string[] dates = data[0].Split('-', StringSplitOptions.RemoveEmptyEntries);
                    int year = int.Parse(dates[0].Substring(1));
                    int month = int.Parse(dates[1].Substring(0, dates[1].Length - 1));
                    double value = double.Parse(data[10].Substring(1, data[10].Length - 2));
                    writer.WriteLine($"{month},{year},{value}");
                }
                line = reader.ReadLine();
            }
            reader.Close();
            writer.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception: " + e.Message);
        }
        finally
        {

        }
    }
    public string TransformDaily(string inputFilePath)
    {
        string outputFilePath = inputFilePath + "_m";
        string[] lines = File.ReadAllLines(inputFilePath);

        // Create the output file and write the header
        using (StreamWriter writer = new StreamWriter(outputFilePath))
        {

        }
        return outputFilePath;
    }

    public string TransformNominal(string inputFilePath)
    {
        return "";
        /*

            // data comes from a file
            string inputFilePath = "us_nominal_stocks.txt";
            string outputFilePath = "us_stocks_n.csv";

            // Read all lines from the input file
            string[] lines = File.ReadAllLines(inputFilePath);

            // Create the output file and write the header
            using (StreamWriter writer = new StreamWriter(outputFilePath))
            {
                double initialPrice = 0.0;
                double nominalCumulativeReturn = 0.0;
                double totalReturn = 100.0;
                bool first = true;

                foreach (string line in lines)
                {
                    // Skip empty lines
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    // Split the line into date fraction and value
                    string[] parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length != 3)
                    {
                        continue; // Skip malformed lines
                    }

                    // Parse date fraction and value
                    string[] dates = parts[0].Split('.', StringSplitOptions.RemoveEmptyEntries);
                    int year = int.Parse(dates[0]);
                    int month = int.Parse(dates[1]);
                    double price = double.Parse(parts[1], CultureInfo.InvariantCulture);
                    double dividend = double.Parse(parts[2], CultureInfo.InvariantCulture);

                    if (first)
                    {
                        initialPrice = price;
                        first = false;
                        writer.WriteLine($"{month},{year},{totalReturn:F2}");
                        continue;
                    }
                    // Calculate dividend yield and update total return index
                    double dividendYield = dividend / 12 / price;
                    totalReturn *= (1 + dividendYield);

                    // Calculate cumulative return
                    nominalCumulativeReturn = totalReturn * (price / initialPrice);

                    // Write the transformed data to the output file
                    writer.WriteLine($"{month},{year},{nominalCumulativeReturn:F2}");
                }
            }

            Console.WriteLine($"Transformed data saved to {outputFilePath}");

        */
    }

    public void MonthlyAverages()
    {
        /*
                            List<Item> itemizedData = new List<Item>();

                    foreach (var data in historicalData)
                    {
                        int day = int.Parse(data.Date.ToString("dd"));
                        int month = int.Parse(data.Date.ToString("MM"));
                        int year = int.Parse(data.Date.ToString("yyyy"));
                        float close = float.Parse(data.Close.ToString("F2"));
                        Item item = new Item()
                        {
                            Day = day,
                            Month = month,
                            Year = year,
                            Value = close
                        };
                        itemizedData.Add(item);
                        //writer.WriteLine($"{day},{month},{year},{close}");
                    }
                    // Calculate average monthly values
                    var monthlyAverages = itemizedData
                        .GroupBy(item => new { item.Year, item.Month })
                        .Select(group => new
                        {
                            Year = group.Key.Year,
                            Month = group.Key.Month,
                            AverageValue = group.Average(item => item.Value)
                        })
                        .OrderBy(result => result.Year)
                        .ThenBy(result => result.Month);


                    using (var writer = new System.IO.StreamWriter(filePath))
                    {
                        writer.WriteLine("Month,Year,AverageValue");
                        foreach (var result in monthlyAverages)
                        {
                            writer.WriteLine($"{result.Month},{result.Year},{result.AverageValue:F2}");
                        }
                    }
                    /*
                    // Open a StreamWriter to write the data to the file
                    using (StreamWriter writer = new StreamWriter(filePath))
                    {
                        // Write the header
                        writer.WriteLine("year,month,day,close");

                        // Write the data
                        foreach (var data in historicalData)
                        {
                            string day = data.Date.ToString("dd");
                            string month = data.Date.ToString("MM");
                            string year = data.Date.ToString("yyyy");
                            string close = data.Close.ToString("F2");
                            writer.WriteLine($"{day},{month},{year},{close}");
                        }
                    }

        */
    }
}