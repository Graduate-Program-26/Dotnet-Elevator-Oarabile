using ElevatorSim.Console.Display;
using ElevatorSim.Console.Input;
using Microsoft.Extensions.DependencyInjection;

namespace ElevatorSim.Console.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddElevatorConsole(this IServiceCollection services)
    {
        services.AddSingleton<BuildingConfigPrompt>();
        services.AddSingleton<ElevatorTypePrompt>();
        services.AddSingleton<SimulationStatus>();
        services.AddSingleton<StatusBoard>();
        services.AddSingleton<DashboardRunner>();
        services.AddSingleton<CommandParser>();
        services.AddSingleton<AutopilotRunner>();
        services.AddSingleton<AdaptiveStrategyRunner>();
        services.AddSingleton<ConsoleInputRunner>();
        services.AddSingleton<ElevatorBuilder>();
        services.AddSingleton<ElevatorSimulationRunner>();

        return services;
    }
}
