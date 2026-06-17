using ElevatorSim.Application.Extensions;
using ElevatorSim.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;

ServiceCollection services = new ServiceCollection();

services.AddElevatorApplication();
services.AddElevatorLogging();
services.AddElevatorValidation();

await using var serviceProvider = services.BuildServiceProvider();

Console.WriteLine("=== ElevatorSim ===");
Console.WriteLine("Welcome! Let's configure your building.");