using ElevatorSim.Domain.Entities;
using ElevatorSim.Domain.Enums;

namespace ElevatorSim.Tests.Helpers;

public sealed class TestElevator : ElevatorBase
{
    public TestElevator(string id, int maxCapacity, double secondsPerFloor, int startingFloor)
        : base(id, ElevatorType.Passanger, maxCapacity, secondsPerFloor, startingFloor) { }

    public int FloorChangedCount { get; private set; }

    public int ArrivedCount { get; private set; }

    protected override void OnFloorChanged()
    {
        FloorChangedCount++;
    }

    protected override void OnArrived()
    {
        ArrivedCount++;
    }
}