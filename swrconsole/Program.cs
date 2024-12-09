using System.Globalization;
using Swr.Finance;
using Swr.Investment;
using Swr.Processing;
using Swr.Simulation;

Scenario scenario = new Scenario();

if (args.Length == 0)
{
    return;
}

scenario.Years = 30;
scenario.StartYear = 1871;
scenario.EndYear = 2023;
scenario.Portfolio = new Portfolio("us_stocks_orig:100;");
scenario.Inflation = "us_inflation";
scenario.WithdrawFrequency = 1;
scenario.WithdrawalMethod = WithdrawalMethod.STANDARD;
scenario.WithdrawalRate = 4.0f;
scenario.Fees = 0.003f;
scenario.MinimumWithdrawalPercent = 0.03f;

scenario.Values = DataLoader.LoadValues(scenario.Portfolio.Allocations);
scenario.InflationData = DataLoader.LoadInflation(scenario.Values, scenario.Inflation);
bool r3 = scenario.PrepareExchangeRates("usd");
Fixed.Simulate(scenario);

Vanguard.Simulate(scenario);

scenario.WithdrawalMethod = WithdrawalMethod.CURRENT;
Vanguard.Simulate(scenario);

scenario.WithdrawalMethod = WithdrawalMethod.VANGUARD;
Vanguard.Simulate(scenario);


Dictionary<string, object> arguments = ParseArguments(args);

string? command = arguments["command"].ToString();
if (String.IsNullOrEmpty(command))
{

}

scenario.Years = (int)arguments["years"];
scenario.StartYear = (int)arguments["start"];
scenario.EndYear = (int)arguments["end"];
scenario.Portfolio = new Portfolio(arguments["portfolio"].ToString());
if (arguments.TryGetValue("inflation", out var inflationValue) && !string.IsNullOrEmpty(inflationValue?.ToString()))
{
    scenario.Inflation = inflationValue.ToString();
}
if (arguments.TryGetValue("withdrawalRate", out var wr))
{
    scenario.WithdrawalRate = (float)wr;
}
if (arguments["fees"] != null)
{
    // there is a default, 0.01
    scenario.Fees = (float)arguments["fees"] / 100.0f;
}
if (arguments.TryGetValue("minimumWithdrawalPercent", out var minimumWithdrawalPercent))
{
    scenario.MinimumWithdrawalPercent = (float)minimumWithdrawalPercent;
}
if (arguments.TryGetValue("limit", out var limit))
{
    // there is a default, 95
    scenario.SuccessRateLimit = (float)limit;
}
if (arguments.TryGetValue("rebalancing", out object rebalancingString))
{
    if (Enum.TryParse(rebalancingString?.ToString(), true, out Rebalancing rebalancing))
    {
        scenario.Rebalance = rebalancing;
    }
}

scenario.Values = DataLoader.LoadValues(scenario.Portfolio.Allocations);
scenario.InflationData = DataLoader.LoadInflation(scenario.Values, scenario.Inflation);
bool r = scenario.PrepareExchangeRates("usd");

switch (command)
{
    case "fixed":

        //Console.WriteLine("Command \"fixed\"");
        //Fixed f = new Fixed(args);
        //f.LoadData();
        Fixed.Simulate(scenario);
        break;
    case "swr":
        //Swr swr = new Swr(args);
        //swr.LoadData();
        SafeWithdrawal.Simulate(scenario);
        break;
    case "multiple_wr":
        MultipleWr.Simulate(scenario);
        break;
    case "frequency":
        Frequency.Simulate(scenario, 100, 1);
        break;
    case "failsafe":
        Failsafe.Simulate(scenario);
        break;
    default:
        Console.WriteLine("No command match to execute");
        return;
}




static Dictionary<string, object> ParseArguments(string[] args)
{
    var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    foreach (var arg in args)
    {
        if (arg.StartsWith("--"))
        {
            var keyValue = arg.Substring(2).Split('=', 2);

            if (keyValue.Length == 2)
            {
                string key = keyValue[0];
                string value = keyValue[1];

                // Try parsing the value into the appropriate type
                if (int.TryParse(value, out int intValue))
                {
                    result[key] = intValue;
                }
                else if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
                {
                    result[key] = floatValue;
                }
                else if (bool.TryParse(value, out bool boolValue))
                {
                    result[key] = boolValue;
                }
                else
                {
                    result[key] = value; // Default to string if no other type matches
                }
            }
        }
    }

    return result;
}
/* List<float> v = Enumerable.Range(1, 10).Select(i => (float)i).ToList();
IEnumerable<float> result = v.Skip(4);
List<float> resultList = v.SkipWhile(value => value != 4.0f).ToList();
 */
/*
var portfolioString = "us_stocks:100;us_bonds:40;";
var allowZero = false;

Portfolio p = new Portfolio(portfolioString);
p.NormalizePortfolio();


var exchangeData = DataLoader.LoadExchange("usd_cad");
Console.WriteLine("Exchange Data:");
foreach (var data in exchangeData)
{
    Console.WriteLine($"Month: {data.Month}, Year: {data.Year}, Value: {data.Value}");
}

var exchangeInvData = DataLoader.LoadExchangeInv("usd_cad");
Console.WriteLine("Exchange Data Inv:");
foreach (var data in exchangeInvData)
{
    Console.WriteLine($"Month: {data.Month}, Year: {data.Year}, Value: {data.Value}");
}

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
var portfolio = new List<Allocation>
{
    new Allocation { Asset = "cash2", AllocationValue = 0.5f },
    new Allocation { Asset = "cash2_x2", AllocationValue = 0.5f }
};

var values = DataLoader.LoadValues(portfolio);
foreach (var vector in values)
{
    Console.WriteLine($"Data for {vector.Name}:");
    foreach (var data in vector)
    {
        Console.WriteLine(data);
    }
}

//var inflationData = PortfolioLoader.LoadInflation(values, "no_inflation");
var inflationData = DataLoader.LoadInflation(values, "us_inflation");
Console.WriteLine("Inflation Data:");
foreach (var data in inflationData)
{
    Console.WriteLine($"Month: {data.Month}, Year: {data.Year}, Value: {data.Value}");
}

// Create a DataVector object
var dataVector = new DataVector("cash");
dataVector.LoadDataFromCsv("../../../../stock-data/cash2.csv");
foreach (var data in dataVector.Data)
{
    Console.WriteLine($"Month: {data.Month}, Year: {data.Year}, Value: {data.Value}");
}
dataVector.NormalizeData();
foreach (var data in dataVector.Data)
{
    Console.WriteLine($"Month: {data.Month}, Year: {data.Year}, Value: {data.Value}");
}
float? v = dataVector.GetValue(1871, 4);
Console.WriteLine(v);

int index = dataVector.GetIndex(1871, 4);
Console.WriteLine(index);

DataVector dv2 = dataVector.GetDataVector(1871, 4);
foreach (var data in dv2.Data)
{
    Console.WriteLine($"Month: {data.Month}, Year: {data.Year}, Value: {data.Value}");
}

// Add some data
//dataVector.AddData(new Data(1, 2023, 99.9f));
//dataVector.AddData(new Data(2, 2023, 101.5f));
//dataVector.AddData(new Data(3, 2023, 120.9f));
//dataVector.AddData(new Data(4, 2023, 115.5f));

// Access the name
Console.WriteLine($"DataVector Name: {dataVector.Name}");

// Iterate through the data
foreach (var data in dataVector.Data)
{
    Console.WriteLine($"Month: {data.Month}, Year: {data.Year}, Value: {data.Value}");
}

// Using AsEnumerable method
IEnumerable<Data> enumerableData = dataVector.AsEnumerable();

// Using LINQ to filter and print data
var filteredData = enumerableData.Where(d => d.Value > 120);
foreach (var dataItem in filteredData)
{
    Console.WriteLine(dataItem);
}

// Using GetEnumerator method
IEnumerator<Data> enumerator = dataVector.GetEnumerator();
while (enumerator.MoveNext())
{
    Data dataItem = enumerator.Current;
    Console.WriteLine(dataItem);
}

// Using GetReadOnlyEnumerator method
IEnumerator<Data> readOnlyEnumerator = dataVector.GetReadOnlyEnumerator();
while (readOnlyEnumerator.MoveNext())
{
    Data dataItem = readOnlyEnumerator.Current;
    Console.WriteLine(dataItem);
}
*/

