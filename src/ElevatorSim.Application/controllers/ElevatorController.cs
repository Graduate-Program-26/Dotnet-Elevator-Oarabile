using ElevatorSim.Domain.Interfaces;
using ElevatorSim.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ElevatorSim.Application.Controllers;

public sealed class ElevatorController : IElevatorController
{
    private static readonly TimeSpan s_doorOperationDelay = TimeSpan.FromMilliseconds(600);
    private static readonly TimeSpan s_boardingDelayPerPassenger = TimeSpan.FromMilliseconds(250);
    private readonly List<IElevator> _elevators;
    private IDispatchStrategy _strategy;
    private readonly int _minFloor;
    private readonly int _maxFloor;
    private readonly Queue<PendingRequest> _pendingRequests = new();
    private readonly HashSet<string> _busyElevatorIds = new();
    private readonly object _lock = new();
    private readonly ILogger<ElevatorController> _logger;
    private string _currentStrategyName;

    public ElevatorController(IEnumerable<IElevator> elevators, IDispatchStrategy strategy, int minFloor, int maxFloor, ILogger<ElevatorController> logger)
    {
        _elevators = elevators.ToList();
        _strategy = strategy;
        _minFloor = minFloor;
        _maxFloor = maxFloor;
        _logger = logger;
        _currentStrategyName = strategy.GetType().Name;
    }

    public void SetDispatchStrategy(IDispatchStrategy strategy)
    {
        lock (_lock)
        {
            _strategy = strategy;
            _currentStrategyName = strategy.GetType().Name;
        }

        _logger.LogInformation("Dispatch strategy changed to {StrategyType}.", strategy.GetType().Name);
    }

    private sealed record PendingRequest(
        int PickupFloor,
        int DestinationFloor,
        int PassengerCount,
        TaskCompletionSource CompletionSource,
        CancellationToken CancellationToken
    );

    private IElevator? TryDispatch(int pickupFloor, int destinationFloor, int passengerCount, CancellationToken cancellationToken)
    {
        var available = _elevators.Where(e => !_busyElevatorIds.Contains(e.Id)).ToList();
        var selected = _strategy.SelectElevator(available, pickupFloor, passengerCount);

        if (selected is null)
        {
            return null;
        }

        _busyElevatorIds.Add(selected.Id);

        _logger.LogInformation(
            "Elevator {ElevatorId} dispatched to pickup floor {PickupFloor}, destination {DestinationFloor}, with {PassengerCount} passengers.",
            selected.Id,
            pickupFloor,
            destinationFloor,
            passengerCount
            );

        _ = RunDispatchAsync(selected, pickupFloor, destinationFloor, passengerCount, cancellationToken);

        return selected;
    }

    private async Task RunDispatchAsync(IElevator elevator, int pickupFloor, int destinationFloor, int passengerCount, CancellationToken cancellationToken)
    {
        try
        {
            await elevator.MoveToFloorAsync(pickupFloor, cancellationToken);
            elevator.BeginBoarding();
            await Task.Delay(GetPassengerTransferDelay(passengerCount), cancellationToken);
            elevator.AddPassengers(passengerCount);
            elevator.CompleteBoarding();

            await elevator.MoveToFloorAsync(destinationFloor, cancellationToken);
            elevator.BeginBoarding();
            await Task.Delay(GetPassengerTransferDelay(passengerCount), cancellationToken);
            elevator.RemovePassengers(elevator.PassengerCount);

            _logger.LogInformation(
                "Elevator {ElevatorId} picked up passengers at floor {PickupFloor}, arrived at floor {DestinationFloor}, and dropped them off.",
                elevator.Id,
                pickupFloor,
                destinationFloor);
        }
        finally
        {
            lock (_lock)
            {
                _busyElevatorIds.Remove(elevator.Id);
                ProcessPendingRequests();
            }
        }
    }

    private void ProcessPendingRequests()
    {
        Queue<PendingRequest> stillWaiting = new Queue<PendingRequest>();

        while (_pendingRequests.TryDequeue(out var request))
        {
            if (request.CancellationToken.IsCancellationRequested)
            {
                continue;
            }

            if (TryDispatch(request.PickupFloor, request.DestinationFloor, request.PassengerCount, request.CancellationToken) is not null)
            {
                request.CompletionSource.TrySetResult();
            }
            else
            {
                stillWaiting.Enqueue(request);
            }
        }

        while (stillWaiting.TryDequeue(out var request))
        {
            _pendingRequests.Enqueue(request);
        }
    }

    private static TimeSpan GetPassengerTransferDelay(int passengerCount)
    {
        return TimeSpan.FromMilliseconds(
            s_doorOperationDelay.TotalMilliseconds + (s_boardingDelayPerPassenger.TotalMilliseconds * passengerCount));
    }

    public IReadOnlyList<IElevator> Elevators => _elevators;
    public int PendingRequestCount
    {
        get
        {
            lock (_lock)
            {
                return _pendingRequests.Count;
            }
        }
    }

    public int BusyElevatorCount
    {
        get
        {
            lock (_lock)
            {
                return _busyElevatorIds.Count;
            }
        }
    }

    public string CurrentStrategyName
    {
        get
        {
            lock (_lock)
            {
                return _currentStrategyName;
            }
        }
    }

    public Task RequestElevatorAsync(int requestedFloor, int passengerCount, CancellationToken cancellationToken)
    {
        return RequestElevatorAsync(1, requestedFloor, passengerCount, cancellationToken);
    }

    public Task RequestElevatorAsync(int pickupFloor, int destinationFloor, int passengerCount, CancellationToken cancellationToken)
    {
        _ = new FloorNumber(pickupFloor, _minFloor, _maxFloor);
        _ = new FloorNumber(destinationFloor, _minFloor, _maxFloor);
        _ = new PassengerCount(passengerCount);
        if (pickupFloor == destinationFloor)
        {
            throw new ArgumentException("Pickup and destination floors must be different.", nameof(destinationFloor));
        }

        var largestElevatorCapacity = _elevators.Max(e => e.MaxCapacity);
        if (passengerCount > largestElevatorCapacity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(passengerCount),
                passengerCount,
                $"Passenger group cannot fit in any elevator. Largest capacity is {largestElevatorCapacity}.");
        }

        lock (_lock)
        {
            if (TryDispatch(pickupFloor, destinationFloor, passengerCount, cancellationToken) is not null)
            {
                return Task.CompletedTask;
            }

            TaskCompletionSource completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            PendingRequest request = new PendingRequest(pickupFloor, destinationFloor, passengerCount, completionSource, cancellationToken);

            cancellationToken.Register(() => completionSource.TrySetCanceled());

            _pendingRequests.Enqueue(request);
            return completionSource.Task;
        }
    }

    public void MarkElevatorOutOfService(string elevatorId)
    {
        lock (_lock)
        {
            var elevator = FindElevator(elevatorId);
            if (_busyElevatorIds.Contains(elevator.Id))
            {
                throw new InvalidOperationException($"Elevator {elevator.Id} is busy and cannot be taken out of service yet.");
            }

            elevator.MarkOutOfService();
            ProcessPendingRequests();
        }
    }

    public void ReturnElevatorToService(string elevatorId)
    {
        lock (_lock)
        {
            FindElevator(elevatorId).ReturnToService();
            ProcessPendingRequests();
        }
    }

    private IElevator FindElevator(string elevatorId)
    {
        return _elevators.FirstOrDefault(e => e.Id.Equals(elevatorId, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Elevator '{elevatorId}' was not found.", nameof(elevatorId));
    }
}
