using ElevatorSim.Domain.Interfaces;
using ElevatorSim.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ElevatorSim.Application.Controllers;

public sealed class ElevatorController : IElevatorController
{
    private readonly List<IElevator> _elevators;
    private IDispatchStrategy _strategy;
    private readonly int _minFloor;
    private readonly int _maxFloor;
    private readonly Queue<PendingRequest> _pendingRequests = new();
    private readonly HashSet<string> _busyElevatorIds = new();
    private readonly object _lock = new();
    private readonly ILogger<ElevatorController> _logger;

    public ElevatorController(IEnumerable<IElevator> elevators, IDispatchStrategy strategy, int minFloor, int maxFloor, ILogger<ElevatorController> logger)
    {
        _elevators = elevators.ToList();
        _strategy = strategy;
        _minFloor = minFloor;
        _maxFloor = maxFloor;
        _logger = logger;
    }

    public void SetDispatchStrategy(IDispatchStrategy strategy)
    {
        lock (_lock)
        {
            _strategy = strategy;
        }

        _logger.LogInformation("Dispatch strategy changed to {StrategyType}.", strategy.GetType().Name);
    }

    private sealed record PendingRequest(
        int RequestedFloor,
        int PassengerCount,
        TaskCompletionSource CompletionSource,
        CancellationToken CancellationToken
    );

    private IElevator? TryDispatch(int requestedFloor, int passengerCount, CancellationToken cancellationToken)
    {
        var available = _elevators.Where(e => !_busyElevatorIds.Contains(e.Id)).ToList();
        var selected = _strategy.SelectElevator(available, requestedFloor, passengerCount);

        if (selected is null)
        {
            return null;
        }

        selected.AddPassengers(passengerCount);
        _busyElevatorIds.Add(selected.Id);

        _logger.LogInformation(
            "Elevator {ElevatorId} dispatched to floor {RequestedFloor} with {PassengerCount} passengers.",
            selected.Id,
            requestedFloor,
            passengerCount
            );

        _ = RunDispatchAsync(selected, requestedFloor, cancellationToken);

        return selected;
    }

    private async Task RunDispatchAsync(IElevator elevator, int destinationFloor, CancellationToken cancellationToken)
    {
        try
        {
            await elevator.MoveToFloorAsync(destinationFloor, cancellationToken);

            _logger.LogInformation(
                "Elevator {ElevatorId} arrived at floor {DestinationFloor}.",
                elevator.Id,
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

            if (TryDispatch(request.RequestedFloor, request.PassengerCount, request.CancellationToken) is not null)
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

    public IReadOnlyList<IElevator> Elevators => _elevators;

    public Task RequestElevatorAsync(int requestedFloor, int passengerCount, CancellationToken cancellationToken)
    {
        _ = new FloorNumber(requestedFloor, _minFloor, _maxFloor);

        lock (_lock)
        {
            if (TryDispatch(requestedFloor, passengerCount, cancellationToken) is not null)
            {
                return Task.CompletedTask;
            }

            TaskCompletionSource completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            PendingRequest request = new PendingRequest(requestedFloor, passengerCount, completionSource, cancellationToken);

            cancellationToken.Register(() => completionSource.TrySetCanceled());

            _pendingRequests.Enqueue(request);
            return completionSource.Task;
        }
    }
}