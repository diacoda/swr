using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Net.Http.Headers;

namespace swrapi.Model;

public class SimulationRequest
{
    public SimulationRequest()
    {
    }

    public int Years { get; set; } = 30;
    public int StartYear { get; set; } = 1871;
    public int EndYear { get; set; } = 2024;
    public string Portfolio { get; set; } = String.Empty;
    public string Inflation { get; set; } = String.Empty;
    public float? WithdrawalRate { get; set; } = 0.0f;
    public float Fees { get; set; } = 0.0f;
}