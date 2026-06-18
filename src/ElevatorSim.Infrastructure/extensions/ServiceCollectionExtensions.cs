using ElevatorSim.Application.Models;
using ElevatorSim.Infrastructure.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ElevatorSim.Infrastructure.Extensions;

public static class ServiceCollectionExtension
{
    public const string LogFilePath = "logs/elevator-sim-.log";

    public static IServiceCollection AddElevatorLogging(this IServiceCollection services)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(LogFilePath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        services.AddLogging(builder => builder.AddSerilog(dispose: true));

        return services;
    }

    public static IServiceCollection AddElevatorValidation(this IServiceCollection services)
    {
        services.AddSingleton<IValidator<BuildingConfigRequest>, BuildingConfigRequestValidator>();

        return services;
    }
}