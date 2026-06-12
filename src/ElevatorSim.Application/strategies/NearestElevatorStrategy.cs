using ElevatorSim.Domain.Interfaces;

namespace ElevatorSim.Application.Strategies;

public sealed class NearestElevatorStrategy : IDispatchStrategy
{
    public IElevator? SelectElevator(IReadOnlyList<IElevator> elevators, int requestedFloor, int passengerCount)
    {
        return elevators
            .Where(e => !e.IsAtCapacity)
            .OrderBy(e => Math.Abs(e.CurrentFloor - requestedFloor))
            .FirstOrDefault();
    }
}
