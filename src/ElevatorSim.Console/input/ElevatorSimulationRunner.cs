using ElevatorSim.Application.Controllers;
using ElevatorSim.Application.Enums;
using ElevatorSim.Console.Display;
using ElevatorSim.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ElevatorSim.Console.Input;

public sealed class ElevatorSimulationRunner
{
    private readonly IServiceProvider _serviceProvider;
    private readonly StatusBoard _statusBoard;
    private readonly DashboardRunner _dashboardRunner;
    private readonly CommandParser _commandParser;
    private readonly AutopilotRunner _autopilotRunner;
    private readonly AdaptiveStrategyRunner _adaptiveStrategyRunner;
    private readonly SimulationStatus _simulationStatus;
    private readonly ConsoleInputRunner _inputRunner;

    public ElevatorSimulationRunner(
        IServiceProvider serviceProvider,
        StatusBoard statusBoard,
        DashboardRunner dashboardRunner,
        CommandParser commandParser,
        AutopilotRunner autopilotRunner,
        AdaptiveStrategyRunner adaptiveStrategyRunner,
        SimulationStatus simulationStatus,
        ConsoleInputRunner inputRunner)
    {
        _serviceProvider = serviceProvider;
        _statusBoard = statusBoard;
        _dashboardRunner = dashboardRunner;
        _commandParser = commandParser;
        _autopilotRunner = autopilotRunner;
        _adaptiveStrategyRunner = adaptiveStrategyRunner;
        _simulationStatus = simulationStatus;
        _inputRunner = inputRunner;
    }

    /// <summary>
    /// Initializes the elevator simulation, starts all background services
    /// (dashboard, autopilot, adaptive strategy, status updates, and input handling),
    /// and keeps the simulation running until the user exits.
    /// </summary>
    /// <param name="elevators">The collection of elevators participating in the simulation.</param>
    /// <param name="maxFloor">The highest floor available in the building.</param>
    /// <returns>A task that completes when the simulation has been stopped.</returns>

    public async Task RunAsync(IReadOnlyList<IElevator> elevators, int maxFloor)
    {
        var normalStrategy = _serviceProvider.GetRequiredKeyedService<IDispatchStrategy>(DispatchStrategyType.Normal);
        var controllerLogger = _serviceProvider.GetRequiredService<ILogger<ElevatorController>>();
        var controller = new ElevatorController(
            elevators,
            normalStrategy,
            minFloor: 1,
            maxFloor,
            controllerLogger);

        using var simulationCts = new CancellationTokenSource();

        _simulationStatus.UpdateStrategy(controller.CurrentStrategyName);
        _simulationStatus.UpdateAutopilot(_autopilotRunner.Enabled, _autopilotRunner.RequestsPerMinute);
        _simulationStatus.UpdateEmergencyMode(_adaptiveStrategyRunner.EmergencyModeEnabled);
        _statusBoard.LogActivity("Commands: 'from,to,passengers', 'service E1 out', 'service E1 in', 'emergency on', 'auto on', 'rate 20', 'exit'.");

        var dashboardTask = _dashboardRunner.RunAsync(controller.Elevators, simulationCts.Token);
        var autopilotTask = _autopilotRunner.RunAsync(controller, _statusBoard, _simulationStatus, maxFloor, simulationCts.Token);
        var strategyTask = _adaptiveStrategyRunner.RunAsync(controller, _autopilotRunner, _statusBoard, _simulationStatus, simulationCts.Token);
        var statusTask = RunStatusLoopAsync(controller, simulationCts.Token);

        var inputTask = Task.Run(
            () => _inputRunner.RunInputLoop(controller, _commandParser, _autopilotRunner, _adaptiveStrategyRunner, _simulationStatus, _statusBoard, simulationCts),
            simulationCts.Token);

        await inputTask;
        simulationCts.Cancel();

        try
        {
            await Task.WhenAll(dashboardTask, autopilotTask, strategyTask, statusTask);
        }
        catch (OperationCanceledException)
        {
            //Expected exception, do nothing
        }
    }

    private async Task RunStatusLoopAsync(ElevatorController controller, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            _simulationStatus.UpdateLoad(controller.PendingRequestCount, controller.BusyElevatorCount);

            try
            {
                await Task.Delay(250, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
