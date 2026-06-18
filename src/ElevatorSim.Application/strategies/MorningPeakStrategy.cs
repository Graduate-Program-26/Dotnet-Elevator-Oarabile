using ElevatorSim.Domain.Enums;
using ElevatorSim.Domain.Interfaces;

namespace ElevatorSim.Application.Strategies; 

public sealed class MorningPeakStrategy : IDispatchStrategy
{
    public const int GroundFloor = 1;

    public IElevator? SelectElevator(IReadOnlyList<IElevator> elevators, int requestedFloor, int passengerCount)
    {
        var availableElevators = elevators
            .Where(e => ElevatorSelectionRules.CanCarry(e, passengerCount))
            .ToList();

        return availableElevators
            .OrderBy(e => GetPriorityScore(e, requestedFloor))
            .ThenBy(e => Math.Abs(e.CurrentFloor - requestedFloor))
            .FirstOrDefault();
    }

    private static int GetPriorityScore(IElevator elevator, int requestedFloor)
    {
        if (elevator.Direction == ElevatorDirection.Up && elevator.CurrentFloor <= requestedFloor)
        {
            return 0;
        }

        if (elevator.Direction == ElevatorDirection.Idle && elevator.CurrentFloor <= requestedFloor)
        {
            return 1;
        }

        if (elevator.Direction == ElevatorDirection.Idle)
        {
            return 2;
        }

        return 3;
    }
}
