using ElevatorSim.Domain.Interfaces;

namespace ElevatorSim.Application.Controllers;

public sealed class ElevatorController : IElevatorController
{
    private readonly List<IElevator> _elevators;
    private readonly IDispatchStrategy _strategy;
    private readonly int _minFloor;
    private readonly int _maxFloor;
    private readonly Queue<PendingRequest> _pendingRequests = new();
    private readonly HashSet<string> _busyElevatorIds = new();
    private readonly object _lock = new();

    private sealed record PendingRequest(
        int RequestedFloor,
        int PassengerCount,
        TaskCompletionSource CompletionSource,
        CancellationToken CancellationToken
    );

    public ElevatorController(IEnumerable<IElevator> elevators, IDispatchStrategy strategy, int minFloor, int maxFloor)
    {
        _elevators = elevators.ToList();
        _strategy = strategy;
        _minFloor = minFloor;
        _maxFloor = maxFloor;
    }
}