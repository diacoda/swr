// See https://aka.ms/new-console-template for more information

await Data.GetData("^GSPTSE", OoplesFinance.YahooFinanceAPI.Enums.DataFrequency.Daily);
await Data.GetData("^GSPC", OoplesFinance.YahooFinanceAPI.Enums.DataFrequency.Daily);

/*string inputFilePath = "../stock-data/us_stocks.txt"; // Path to your positional file
string outputFilePath = "../stock-data/us_stocks.csv"; // Path for the output CSV file

// Read the file and process each line
var lines = File.ReadAllLines(inputFilePath);
var csvData = new List<string> { "Month,Year,Value" }; // CSV header

foreach (var line in lines)
{
    // Split by space or tab
    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length != 2)
    {
        Console.WriteLine("part not 2");
        continue;
    }

    var yearMonth = parts[0].Split('.');
    if (yearMonth.Length != 2)
    {
        Console.WriteLine("year month not 2");
        continue;
    }

    // Parse year, month, and value
    if (int.TryParse(yearMonth[0], out int year) &&
        int.TryParse(yearMonth[1], out int month) &&
        double.TryParse(parts[1], out double value))
    {
        csvData.Add($"{month:D2},{year},{value:F2}");
    }
}

// Write to CSV file
File.WriteAllLines(outputFilePath, csvData);

Console.WriteLine($"Data successfully transformed and saved to {outputFilePath}");
*/