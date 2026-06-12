using ElevatorSim.Domain.Interfaces;

namespace ElevatorSim.Application.Strategies;

public sealed class Emergencystrategy : IDispatchStrategy
{
    public IElevator? SelectElevator(IReadOnlyList<IElevator> elevators, int requestedFloor, int passengerCount)
    {
       return elevators
            .Where(e => e.State != Domain.Enums.ElevatorState.OutOfService)
            .OrderBy(e => EstimatedSecondsToReach(e, requestedFloor))
            .FirstOrDefault();
    }

    //Looking for the fastesty elevator, not really the closest
    private static double EstimatedSecondsToReach(IElevator elevator, int requestedFloor)
    {
        var floorsToTravel = Math.Abs(elevator.CurrentFloor - requestedFloor);
        return floorsToTravel * elevator.SecondsPerFloor;
    }
}