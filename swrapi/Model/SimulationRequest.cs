using System.ComponentModel.DataAnnotations;
using Swr.Simulation;

namespace swrapi.Model;

public class SimulationRequest
{
    public SimulationRequest()
    {
    }
    [Required]
    public int Years { get; set; } = 30;
    [Required]
    public int StartYear { get; set; } = 1871;
    public int EndYear { get; set; } = 2024;
    public string Portfolio { get; set; } = String.Empty;
    public string Inflation { get; set; } = String.Empty;
    public float WithdrawalRate { get; set; } = 0.04f;
    public float Fees { get; set; } = 0.003f;
    public WithdrawalMethod WithdrawalMethod { get; set; } = WithdrawalMethod.STANDARD;
    public WithdrawalFrequency WithdrawalFrequency { get; set; } = WithdrawalFrequency.Monthly;


}