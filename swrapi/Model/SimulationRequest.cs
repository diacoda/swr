using System.ComponentModel.DataAnnotations;
using Swr.Simulation;

namespace swrapi.Model;

public class SimulationRequest
{
    public SimulationRequest()
    {
    }

    [Required]
    public float InitialInvestment { get; set; } = 10000.0f;
    [Required]
    public float InitialCash { get; set; } = 0.0f;
    [Required]
    public int TimeHorizon { get; set; } = 30;
    [Required]
    public int StartYear { get; set; } = 1871;
    [Required]
    public int EndYear { get; set; } = 2024;
    [Required]
    public string Portfolio { get; set; } = String.Empty;
    public string Inflation { get; set; } = "no-inflation";
    public float Fees { get; set; } = 0.003f;
    public WithdrawalMethod WithdrawalMethod { get; set; } = WithdrawalMethod.STANDARD;
    public WithdrawalFrequency WithdrawalFrequency { get; set; } = WithdrawalFrequency.MONTHLY;
    public float SuccessRateLimit { get; set; } = 95.0f;
}