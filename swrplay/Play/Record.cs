namespace Swr.Play;

public class Record
{
    public Record()
    { }
    public Record(int index, int month, int year, float value, float normalized, float rateReturn, float totalReturn)
    {
        Index = index;
        Month = month;
        Year = year;
        Value = value;
        Normalized = normalized;
        RateReturn = rateReturn;
        TotalReturn = totalReturn;

    }

    public int Month { get; set; }
    public int Year { get; set; }
    public float Value { get; set; }
    public int Index { get; set; }
    public float Normalized { get; set; }
    public float RateReturn { get; set; }
    public float TotalReturn { get; set; }
}