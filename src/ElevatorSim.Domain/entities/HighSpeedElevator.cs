using ElevatorSim.Domain.Enums;

namespace ElevatorSim.Domain.Entities;
public sealed class HighSpeedElevator : ElevatorBase
{
    public const int DefaultMaxCapacity = 6;
    public const double DefaultSecondsPerFloor = 0.2;
    public HighSpeedElevator(string id, int startingFloor)
        : base(id, ElevatorType.HighSpeed, DefaultMaxCapacity, DefaultSecondsPerFloor, startingFloor) { }
}