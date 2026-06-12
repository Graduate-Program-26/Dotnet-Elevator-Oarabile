using ElevatorSim.Domain.Entities;
using ElevatorSim.Domain.Enums;

namespace ElevatorSim.Tests.Domain;

public class ElevatorTypesTests
{
    [Fact]
    public void PassengerElevator_HasCorrectDefaults()
    {
        PassengerElevator elevator = new PassengerElevator("P1", startingFloor: 1);

        Assert.Equal(ElevatorType.Passanger, elevator.Type);
        Assert.Equal(PassengerElevator.DefaultMaxCapacity, elevator.MaxCapacity);
        Assert.Equal(PassengerElevator.DefaultMaxCapacity, elevator.MaxCapacity);
    }

    [Fact]
    public void HighSpeedElevator_HasCorrectDefaults()
    {
        HighSpeedElevator elevator = new HighSpeedElevator("H1", startingFloor: 1);

        Assert.Equal(ElevatorType.HighSpeed, elevator.Type);
        Assert.Equal(HighSpeedElevator.DefaultMaxCapacity, elevator.MaxCapacity);
        Assert.Equal(HighSpeedElevator.DefaultSecondsPerFloor, elevator.SecondsPerFloor);
    }

    [Fact]
    public void FreightElevator_HasCorrectDefaults()
    {
        var elevator = new FreightElevator("F1", startingFloor: 1);

        Assert.Equal(ElevatorType.Freight, elevator.Type);
        Assert.Equal(FreightElevator.DefaultMaxCapacity, elevator.MaxCapacity);
        Assert.Equal(FreightElevator.DefaultSecondsPerFloor, elevator.SecondsPerFloor);
    }
}