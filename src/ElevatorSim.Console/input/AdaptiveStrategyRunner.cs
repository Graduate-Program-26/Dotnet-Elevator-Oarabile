using ElevatorSim.Application.Controllers;
using ElevatorSim.Application.Enums;
using ElevatorSim.Console.Display;
using ElevatorSim.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ElevatorSim.Console.Input;

public sealed class AdaptiveStrategyRunner
{
    private readonly IServiceProvider _serviceProvider;
    private readonly object _lock = new();
    private bool _emergencyModeEnabled;

    public AdaptiveStrategyRunner(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetEmergencyMode(bool enabled)
    {
        lock (_lock)
        {
            _emergencyModeEnabled = enabled;
        }
    }

    public bool EmergencyModeEnabled
    {
        get
        {
            lock (_lock)
            {
                return _emergencyModeEnabled;
            }
        }
    }

    public async Task RunAsync(
        ElevatorController controller,
        AutopilotRunner autopilotRunner,
        StatusBoard statusBoard,
        SimulationStatus simulationStatus,
        CancellationToken cancellationToken)
    {
        var activeStrategyType = DispatchStrategyType.Normal;
        simulationStatus.UpdateStrategy(controller.CurrentStrategyName);

        while (!cancellationToken.IsCancellationRequested)
        {
            simulationStatus.UpdateLoad(controller.PendingRequestCount, controller.BusyElevatorCount);

            simulationStatus.UpdateEmergencyMode(EmergencyModeEnabled);

            var nextStrategyType = SelectStrategy(controller, autopilotRunner, EmergencyModeEnabled);
            if (nextStrategyType != activeStrategyType)
            {
                var strategy = _serviceProvider.GetRequiredKeyedService<IDispatchStrategy>(nextStrategyType);
                controller.SetDispatchStrategy(strategy);
                activeStrategyType = nextStrategyType;
                simulationStatus.UpdateStrategy(controller.CurrentStrategyName);
                statusBoard.LogActivity($"[yellow]Strategy changed to {FormatStrategy(nextStrategyType)}[/]");
            }
            else
            {
                simulationStatus.UpdateStrategy(controller.CurrentStrategyName);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private static DispatchStrategyType SelectStrategy(
        ElevatorController controller,
        AutopilotRunner autopilotRunner,
        bool emergencyModeEnabled)
    {
        if (emergencyModeEnabled)
        {
            return DispatchStrategyType.Emergency;
        }

        var elevatorCount = Math.Max(1, controller.Elevators.Count);
        var busyRatio = controller.BusyElevatorCount / (double)elevatorCount;

        if (controller.PendingRequestCount >= elevatorCount || busyRatio >= 0.75)
        {
            return DispatchStrategyType.Emergency;
        }

        if (autopilotRunner.Enabled && autopilotRunner.RequestsPerMinute >= 8)
        {
            return DispatchStrategyType.MorningPeak;
        }

        return DispatchStrategyType.Normal;
    }

    private static string FormatStrategy(DispatchStrategyType strategyType)
    {
        return strategyType switch
        {
            DispatchStrategyType.Normal => "Normal",
            DispatchStrategyType.Emergency => "Emergency",
            DispatchStrategyType.MorningPeak => "Morning Peak",
            _ => strategyType.ToString(),
        };
    }
}
