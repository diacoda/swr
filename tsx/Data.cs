using OoplesFinance.YahooFinanceAPI;
using OoplesFinance.YahooFinanceAPI.Enums;
using OoplesFinance.YahooFinanceAPI.Helpers;
using OoplesFinance.YahooFinanceAPI.Models;
public class Data
{
    public static async Task GetData()
    {

        // Define the ticker symbol

        string tickerSymbol = "^GSPC";

        DateTime startDate = new DateTime(1871, 1, 1);
        DateTime endDate = DateTime.Now;

        var yahooFinanceApi = new YahooClient();
        var historicalData = await yahooFinanceApi.GetHistoricalDataAsync(tickerSymbol, DataFrequency.Daily, startDate, endDate);

        // Define the file path
        string filePath = "../../../../stock-data/us_monthly3.csv";
        if (historicalData != null && historicalData.Count() > 0)
        {
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
            Console.WriteLine($"Data has been saved to {filePath}");
        }
        else
        {
            Console.WriteLine("No historical data found for the specified date range.");
        }
    }
}