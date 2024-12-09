namespace Swr.Data;

public class Item
{
    public int Month { get; set; }
    public int Year { get; set; }
    public double Value { get; set; }

    public Item(int month, int year, double value)
    {
        Month = month;
        Year = year;
        Value = value;
    }

    public override string ToString()
    {
        return $"Month: {Month}, Year: {Year}, Value: {Value}";
    }
}