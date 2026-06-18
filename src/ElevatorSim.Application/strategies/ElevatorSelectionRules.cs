using ElevatorSim.Domain.Interfaces;
using ElevatorSim.Domain.Enums;

namespace ElevatorSim.Application.Strategies;

internal static class ElevatorSelectionRules
{
    public static bool CanCarry(IElevator elevator, int passengerCount)
    {
        return !elevator.IsAtCapacity
            && elevator.State != ElevatorState.OutOfService
            && passengerCount >= 0
            && elevator.PassengerCount + passengerCount <= elevator.MaxCapacity;
    }
}
