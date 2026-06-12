using ElevatorSim.Domain.Enums;

namespace ElevatorSim.Domain.Entities;

public sealed class PassengerElevator : ElevatorBase
{
    public const int DefaultMaxCapacity = 10;
    public const double DefaultSecondsPerFloor = 0.4;
    public PassengerElevator(string id, int startingFloor)
        : base(id, ElevatorType.Passanger, DefaultMaxCapacity, DefaultSecondsPerFloor, startingFloor) { }
}