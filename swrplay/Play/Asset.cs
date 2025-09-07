using Swr.Data;

namespace Swr.Play;
public class Asset
{
    public Asset(string name)
    {
        Name = name;
    }
    public string Name { get; set; } = string.Empty;
    public float Percent { get; set; } = 0.0f;
    public List<Item> Nominal { get; set; } = new List<Item>();
    public List<Item> Normalized { get; set; } = new List<Item>();
    public List<Item> Returns { get; set; } = new List<Item>();

    public void Configure()
    {
        LoadNominal();
        Normalize();
        TransformToReturn();
    }

    private string GetFilePath(string assetName)
    {
        string fileName = assetName;
        return Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, $"stock-data/{fileName}.csv"));
    }

    private void LoadNominal()
    {
        //List<Item> data = new List<Item>();
        string filePath = GetFilePath(Name);
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
                Nominal.Add(dataItem);
            }
        }

        // Method to ensure the data ends with a full year
        while (Nominal.Count > 0 && Nominal[Nominal.Count - 1].Month != 12)
        {
            Nominal.RemoveAt(Nominal.Count - 1);
        }

        // Method to ensure the data starts with a full year
        while (Nominal.Count > 0 && Nominal[0].Month != 1)
        {
            Nominal.RemoveAt(0);
        }
    }

    private void Normalize()
    {
        if (Nominal.Count == 0)
        {
            return;
        }

        if (Math.Abs(Nominal[0].Value - 1.0) < 0.0001)
        {
            return;
        }

        Item item = new();
        item.Index = Nominal[0].Index;
        item.Value = 1.0f;
        item.Month = Nominal[0].Month;
        item.Year = Nominal[0].Year;
        Normalized.Add(item);

        for (int i = 1; i < Nominal.Count; i++)
        {
            item = new Item();
            item.Index = Nominal[i].Index;
            item.Value = Normalized[i - 1].Value * Nominal[i].Value / Nominal[i - 1].Value;
            item.Month = Nominal[i].Month;
            item.Year = Nominal[i].Year;
            Normalized.Add(item);
        }
    }

    private void TransformToReturn()
    {
        if (Normalized.Count == 0)
        {
            return;
        }
        Item item = new();
        item.Value = 1.0f;
        item.Index = Normalized[0].Index;
        item.Month = Normalized[0].Month;
        item.Year = Normalized[0].Year;
        Returns.Add(item);

        for (int i = 1; i < Normalized.Count; i++)
        {
            item = new Item();
            item.Index = Normalized[i].Index;
            item.Value = Normalized[i].Value / Normalized[i - 1].Value;
            item.Month = Normalized[i].Month;
            item.Year = Normalized[i].Year;
            Returns.Add(item);
        }
    }
}