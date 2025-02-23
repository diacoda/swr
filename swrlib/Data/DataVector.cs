namespace Swr.Data;

// Define the DataVector class
public class DataVector : IEnumerable<Item>
{
    public List<Item> Data { get; set; }
    public List<Item> Nominal { get; set; }
    public List<Item> Normalized { get; set; }

    // Property for the name
    public string Name { get; set; }

    // Constructor
    public DataVector(string name)
    {
        Name = name;
        Data = new List<Item>();
        Nominal = new List<Item>();
        Normalized = new List<Item>();
    }

    public Item this[int i]
    {
        get => Data[i];
        set => Data[i] = value;
    }

    public Item Front()
    {
        if (Data.Count == 0)
            throw new InvalidOperationException("The list is empty.");
        return Data[0];
    }

    public Item Back()
    {
        if (Data.Count == 0)
            throw new InvalidOperationException("The list is empty.");
        return Data[Data.Count - 1];
    }

    public int Size()
    {
        return Data.Count;
    }

    public bool Empty()
    {
        return Data.Count == 0;
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return ((System.Collections.IEnumerable)Data).GetEnumerator();
    }

    // IEnumerable<Item> implementation to support foreach loops
    public IEnumerable<Item> AsEnumerable()
    {
        return Data;
    }

    // Enumerator to mimic const_iterator behavior
    public IEnumerator<Item> GetEnumerator()
    {
        return Data.GetEnumerator();
    }

    // Method to load data from a CSV file
    public void LoadDataFromCsv(string filePath)
    {
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

                var dataItem = new
                Item(month, year, value);
                Data.Add(dataItem);
            }
        }
    }

    // Method to ensure the data ends with a full year
    public void FixEnd()
    {
        while (Data.Count > 0 && Data[Data.Count - 1].Month != 12)
        {
            Data.RemoveAt(Data.Count - 1);
        }
    }

    // Method to ensure the data starts with a full year
    public void FixStart()
    {
        while (Data.Count > 0 && Data[0].Month != 1)
        {
            Data.RemoveAt(0);
        }
    }

    // Method to normalize the data
    public void NormalizeData()
    {
        FixEnd();
        FixStart();

        if (Data.Count == 0)
        {
            return;
        }

        if (Math.Abs(Data[0].Value - 1.0) < 0.0001)
        {
            return;
        }
        Nominal = new List<Item>(new Item[Data.Count]);

        float previousValue = Data[0].Value;
        Nominal[0] = new Item();
        Nominal[0].Value = previousValue;
        Nominal[0].Year = Data[0].Year;
        Nominal[0].Month = Data[0].Month;
        Data[0].Value = 1.0f;

        for (int i = 1; i < Data.Count; i++)
        {
            float currentValue = Data[i].Value;
            Data[i].Value = Data[i - 1].Value * (currentValue / previousValue);
            previousValue = currentValue;
            Nominal[i] = new Item();
            Nominal[i].Value = previousValue;
            Nominal[i].Year = Data[i].Year;
            Nominal[i].Month = Data[i].Month;
        }
    }

    // Method to transform values to returns
    public void TransformToReturns()
    {
        if (Data.Count == 0) return;
        Normalized = new List<Item>(new Item[Data.Count]);

        float previousValue = Data[0].Value;
        Normalized[0] = new Item();
        Normalized[0].Value = previousValue;
        Normalized[0].Year = Data[0].Year;
        Normalized[0].Month = Data[0].Month;

        for (int i = 1; i < Data.Count; i++)
        {
            float newValue = Data[i].Value / previousValue;
            previousValue = Data[i].Value;
            Data[i].Value = newValue;
            Normalized[i] = new Item();
            Normalized[i].Value = previousValue;
            Normalized[i].Year = Data[0].Year;
            Normalized[i].Month = Data[0].Month;
        }
    }

    // Method to get the value for a specific year and month
    private double? GetValue(int year, int month)
    {
        foreach (var dataItem in Data)
        {
            if (dataItem.Year == year && dataItem.Month == month)
            {
                return dataItem.Value;
            }
        }

        return null;
    }

    // Method to get the index of the first occurrence of a specific year and month
    private int GetIndex(int year, int month)
    {
        for (int i = 0; i < Data.Count; i++)
        {
            if (Data[i].Year == year && Data[i].Month == month)
            {
                return i;
            }
        }

        Console.WriteLine("This should not happen (start out of range)");
        return 0;
    }

    private DataVector GetDataVector(int startIndex)
    {
        if (startIndex < 0 || startIndex >= Data.Count)
            throw new ArgumentOutOfRangeException(nameof(startIndex), "Start index must be within the bounds of the list.");

        var portion = new DataVector($"{Name} Portion from inded {startIndex}");
        portion.Data = Data.GetRange(startIndex, Data.Count - startIndex);
        return portion;
    }

    // Method to get the portion of the data starting from the specified index
    public DataVector GetDataVector(int year, int month)
    {
        int startIndex = GetIndex(year, month);
        if (startIndex < 0 || startIndex >= Data.Count)
        {
            throw new ArgumentOutOfRangeException("startIndex", "Index is out of range.");
        }

        var portion = new DataVector($"{Name} Portion from {year}-{month}");
        for (int i = startIndex; i < Data.Count; i++)
        {
            portion.Data.Add(Data[i]);
        }
        return portion;
    }

    // Method to check if the start year and month are valid
    public bool IsStartValid(int year, int month)
    {
        return GetValue(year, month).HasValue;
    }
}
