using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Swr.Simulation;
using Swr.Investment;
using Swr.Data;
public class Testing
{
    Scenario _scenario;
    ILogger<Testing> _logger;
    public Testing(Scenario scenario, ILogger<Testing> logger)
    {
        _scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Do()
    {
        // Resolve Scenario using DI
        Portfolio portfolio = new Portfolio("us_stocks:50;us_stocks_orig:50");
        _scenario.Portfolio = portfolio;
        _scenario.Values = DataLoader.LoadValues(_scenario.Portfolio.Allocations);
        foreach (DataVector dv in _scenario.Values)
        {
            string fileName = $"{dv.Name}.txt"; // Create a file name based on the DataVector's Name
            using (StreamWriter writer = new StreamWriter(fileName))
            {
                foreach (Item item in dv.Data)
                {
                    // Assuming `Item` has properties like `Month`, `Year`, and `Value`
                    writer.WriteLine($"{item.Month},{item.Year},{item.Value}");
                }
            }
        }
    }

}