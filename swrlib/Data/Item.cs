namespace Swr.Data;

public class Item
{
    public int Month { get; set; }
    public int Year { get; set; }
    public float Value { get; set; }
    public int Index {get;set;}

    public Item(int month, int year, float value)
    {
        Month = month;
        Year = year;
        Value = value;
    }

    public Item()
    {
    }

    public override string ToString()
    {
        return $"Month: {Month}, Year: {Year}, Value: {Value}, Index: {Index}";
    }
}