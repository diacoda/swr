// See https://aka.ms/new-console-template for more information
using Swr.Data;
using Swr.Investment;
using Swr.Simulation;

Console.WriteLine("Hello, World!");
Portfolio portfolio = new Portfolio("us_stocks:50;us_stocks_orig:50");
Scenario scenario = new Scenario();
scenario.Portfolio = portfolio;
scenario.Values = DataLoader.LoadValues(scenario.Portfolio.Allocations);
foreach (DataVector dv in scenario.Values)
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

