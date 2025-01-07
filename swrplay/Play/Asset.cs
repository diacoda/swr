using Swr.Data;

namespace Swr.Play;
public class Asset
{
    public Asset(string name)
    {
        Name = name;
    }
    public string Name { get; set;} = string.Empty;
    public float Percent { get; set;} = 0.0f;
    public List<Item> Nominal {get; set;} = new List<Item>();
    public List<Item> Normalized {get; set; } = new List<Item>();
    public List<Item> Returns {get; set; } = new List<Item>();   
}