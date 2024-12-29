using Swr.Model;
using Swr.Simulation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<Scenario>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/simulations", (SimulationRequest request, ILogger<Scenario> logger, Scenario scenario) =>
{
    scenario.CopyFrom(request);
    return Microsoft.AspNetCore.Http.Results.Ok(new SimulationResponse());
})
.WithName("Simulations")
.WithOpenApi();

app.MapPost("/retirement", (SimulationRequest request) =>
{
})
.WithName("Retirement")
.WithOpenApi();

app.Run();
