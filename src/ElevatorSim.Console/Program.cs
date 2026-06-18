using ElevatorSim.Application.Extensions;
using ElevatorSim.Console.Extensions;
using ElevatorSim.Console.Input;
using ElevatorSim.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddElevatorApplication();
services.AddElevatorLogging();
services.AddElevatorValidation();
services.AddElevatorConsole();

await using var serviceProvider = services.BuildServiceProvider();

global::System.Console.WriteLine("========== ElevatorSim ==========");
global::System.Console.WriteLine("Configure your building.\n");

var configPrompt = serviceProvider.GetRequiredService<BuildingConfigPrompt>();
var config = configPrompt.Prompt();

global::System.Console.WriteLine($"\nBuilding configured: {config.FloorCount} floors, {config.ElevatorCount} elevators.\n");

var typePrompt = serviceProvider.GetRequiredService<ElevatorTypePrompt>();
var chosenTypes = typePrompt.PromptForTypes(config.ElevatorCount);

var elevatorBuilder = serviceProvider.GetRequiredService<ElevatorBuilder>();
var elevators = elevatorBuilder.CreateElevators(chosenTypes);

global::System.Console.WriteLine();
foreach (var elevator in elevators)
{
    global::System.Console.WriteLine(
        $"  {elevator.Id} — {elevator.Type} — Capacity: {elevator.MaxCapacity} — Speed: {elevator.SecondsPerFloor}s/floor");
}

global::System.Console.WriteLine("\nStarting simulation in 2 seconds...\n");
await Task.Delay(1500);

var simulationRunner = serviceProvider.GetRequiredService<ElevatorSimulationRunner>();
await simulationRunner.RunAsync(elevators, config.FloorCount);

global::System.Console.WriteLine("\nSimulation stopped. Goodbye!");
