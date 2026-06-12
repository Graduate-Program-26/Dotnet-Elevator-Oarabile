using ElevatorSim.Domain.Enums;

namespace ElevatorSim.Domain.Entities;

public sealed class FreightElevator : ElevatorBase
{
    public const int DefaultMaxCapacity = 20;
    public const double DefaultSecondsPerFloor = 0.8;
    public FreightElevator(string id, int startingFloor)
        : base(id, ElevatorType.Freight, DefaultMaxCapacity, DefaultSecondsPerFloor, startingFloor) { }
}