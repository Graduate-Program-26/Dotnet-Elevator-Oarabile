using ElevatorSim.Application.Controllers;
using ElevatorSim.Console.Display;

namespace ElevatorSim.Console.Input;

public sealed class AutopilotRunner
{
    private static readonly Random s_random = new();
    private readonly object _lock = new();
    private bool _enabled;
    private bool _emergencyModeEnabled;
    private int _requestsPerMinute = 6;

    public bool Enabled
    {
        get
        {
            lock (_lock)
            {
                return _enabled;
            }
        }
    }

    public int RequestsPerMinute
    {
        get
        {
            lock (_lock)
            {
                return _requestsPerMinute;
            }
        }
    }

    public void SetEnabled(bool enabled)
    {
        lock (_lock)
        {
            _enabled = enabled;
        }
    }

    public void SetRequestsPerMinute(int requestsPerMinute)
    {
        lock (_lock)
        {
            _requestsPerMinute = Math.Clamp(requestsPerMinute, 1, 120);
        }
    }

    public void SetEmergencyMode(bool enabled)
    {
        lock (_lock)
        {
            _emergencyModeEnabled = enabled;
        }
    }
    
    /// <summary>
    /// Runs the autopilot loop, periodically generating elevator requests based
    /// on the current simulation settings until the operation is cancelled.
    /// </summary>
    /// <param name="controller">The elevator controller used to dispatch generated requests.</param>
    /// <param name="statusBoard">Displays autopilot activity and request failures.</param>
    /// <param name="simulationStatus">Tracks and updates the current autopilot status.</param>
    /// <param name="maxFloor">The highest floor available for generated requests.</param>
    /// <param name="cancellationToken">A token used to stop the autopilot loop.</param>
    /// <returns>
    /// A task that completes when the autopilot loop is cancelled.
    /// </returns>
    public async Task RunAsync(
        ElevatorController controller,
        StatusBoard statusBoard,
        SimulationStatus simulationStatus,
        int maxFloor,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var enabled = Enabled;
            var requestsPerMinute = RequestsPerMinute;
            var emergencyModeEnabled = EmergencyModeEnabled;
            simulationStatus.UpdateAutopilot(enabled, requestsPerMinute);

            if (!enabled)
            {
                await DelayAsync(TimeSpan.FromMilliseconds(500), cancellationToken);
                continue;
            }

            var request = CreateRequest(maxFloor, controller.Elevators.Max(e => e.MaxCapacity), emergencyModeEnabled);
            var requestTask = controller.RequestElevatorAsync(
                request.PickupFloor,
                request.DestinationFloor,
                request.PassengerCount,
                cancellationToken);
            _ = requestTask.ContinueWith(
                task =>
                {
                    if (task.IsFaulted && task.Exception?.GetBaseException() is Exception ex)
                    {
                        statusBoard.LogActivity($"[red]Autopilot request failed: {ex.Message}[/]");
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            statusBoard.LogActivity(
                $"[grey]Autopilot request: {request.PickupFloor}->{request.DestinationFloor}, {request.PassengerCount} passengers[/]");

            var delaySeconds = Math.Max(0.25, 60d / requestsPerMinute);
            await DelayAsync(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
        }
    }

    private bool EmergencyModeEnabled
    {
        get
        {
            lock (_lock)
            {
                return _emergencyModeEnabled;
            }
        }
    }

    private static ElevatorRequestCommand CreateRequest(int maxFloor, int maxElevatorCapacity, bool emergencyModeEnabled)
    {
        if (emergencyModeEnabled && maxFloor > 1)
        {
            var pickupFloors = s_random.Next(2, maxFloor + 1);
            var generatedPassengerCounts = s_random.Next(5, 18);
            return new ElevatorRequestCommand(pickupFloors, 1, Math.Min(generatedPassengerCounts, maxElevatorCapacity));
        }

        var lobbyRush = s_random.NextDouble() < 0.55;
        var pickupFloor = lobbyRush ? 1 : s_random.Next(2, maxFloor + 1);
        var destinationFloor = lobbyRush ? s_random.Next(2, maxFloor + 1) : 1;
        var generatedPassengerCount = s_random.NextDouble() switch
        {
            < 0.65 => s_random.Next(1, 4),
            < 0.90 => s_random.Next(4, 8),
            _ => s_random.Next(8, 16),
        };

        //Never surpass the max
        var passengerCount = Math.Min(generatedPassengerCount, maxElevatorCapacity);

        return new ElevatorRequestCommand(pickupFloor, destinationFloor, passengerCount);
    }

    private static async Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }
}
