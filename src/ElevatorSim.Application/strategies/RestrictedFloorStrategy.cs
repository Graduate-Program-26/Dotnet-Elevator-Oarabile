using ElevatorSim.Domain.Interfaces;

namespace ElevatorSim.Application.Strategies;

public sealed class RestrictedFloorStrategy : IDispatchStrategy
{
    private readonly string _restrictedElevatorId;
    private readonly IReadOnlySet<int> _allowedFloors;
    private readonly IDispatchStrategy _fallbackStrategy;

    public RestrictedFloorStrategy (
        string restrictedElevatorId,
        IReadOnlySet<int> allowedFloors,
        IDispatchStrategy fallbackStrategy
    )
    {
        _restrictedElevatorId = restrictedElevatorId;
        _allowedFloors = allowedFloors;
        _fallbackStrategy = fallbackStrategy;
    }

    public IElevator? SelectElevator(IReadOnlyList<IElevator> elevators, int requestedFloor, int passengerCount)
    {
        if (_allowedFloors.Contains(requestedFloor))
        {
            var restrictedElevator = elevators.FirstOrDefault(e => e.Id == _restrictedElevatorId);

            if (restrictedElevator is not null && !restrictedElevator.IsAtCapacity)
            {
                return restrictedElevator;
            }
        }

        var unrestrictedElevators = elevators.Where(e => e.Id != _restrictedElevatorId).ToList();

        return _fallbackStrategy.SelectElevator(unrestrictedElevators, requestedFloor, passengerCount);
    }
}