using ElevatorSim.Application.Enums;
using ElevatorSim.Application.Factories;
using ElevatorSim.Application.Strategies;
using ElevatorSim.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ElevatorSim.Application.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddElevatorApplication(this IServiceCollection services)
    {
        services.AddSingleton<IElevatorFactory, ElevatorFactory>();

        services.AddKeyedSingleton<IDispatchStrategy, NearestElevatorStrategy>(DispatchStrategyType.Normal);
        services.AddKeyedSingleton<IDispatchStrategy, Emergencystrategy>(DispatchStrategyType.Emergency);
        services.AddKeyedSingleton<IDispatchStrategy, MorningPeakStrategy>(DispatchStrategyType.MorningPeak);

        return services;
    }
}