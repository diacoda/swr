public class SimulatorConfig
{
    public SimulatorConfig()
    {

    }
    
    public int TimeHorizon {get; set;}
    public int StartYear {get; set;}
    public int EndYear {get; set;}
    public float InitialInvestment { get; set; } = 10000.0f;
    public float InitialCash { get; set; } = 0.0f;
    public string Portfolio { get; set; } = String.Empty;
    public string Inflation { get; set; } = "no-inflation";
    public float Fees { get; set; } = 0.003f; // TER 0.3% = 0.003

}