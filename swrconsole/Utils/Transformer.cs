using System.Globalization;

public class Transformer
{
    public static void Ts()
    {
        string inputFilePath = "../stock-data/usd_cad_unformatted.csv";
        string outputFilePath = "../stock-data/usd_cad.csv";
        try
        {
            if (!File.Exists(inputFilePath))
            {
                throw new FileNotFoundException($"{inputFilePath} not found");
            }

            using (var reader = new StreamReader(inputFilePath))
            using (var writer = new StreamWriter(outputFilePath))
            {
                string? line;

                // Write the header to the output file
                //writer.WriteLine("Month,Year,Value");

                while ((line = reader.ReadLine()) != null)
                {
                    // Split each line into date and value
                    var parts = line.Split('\t');
                    if (parts.Length != 2) continue;

                    // Parse date and value
                    if (DateTime.TryParseExact(parts[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    {
                        var value = parts[1];
                        writer.WriteLine($"{date.Month},{date.Year},{value}");
                    }
                }
            }

            Console.WriteLine($"Transformation complete. Output saved to {outputFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }


    }
}